from __future__ import annotations

import csv
import json
import math
import re
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

import cv2
import fitz
import numpy as np
from PIL import Image, ImageDraw
from rapidocr_onnxruntime import RapidOCR


DIGIT_PATTERN = re.compile(r"\d+")
PREVIEW_OUTLINE_COLOR = (255, 0, 0)
PREVIEW_BADGE_COLOR = (0, 170, 90)
PREVIEW_BADGE_INNER_COLOR = (255, 255, 255)
PREVIEW_TEXT_OFFSET_X = 22
PREVIEW_TEXT_OFFSET_Y = -10
DEFAULT_FAST_PROBE_DPI = 96


@dataclass(slots=True)
class DetectionRecord:
    page: int
    number: str
    center_x: float
    center_y: float
    radius: float
    bbox_x0: float
    bbox_y0: float
    bbox_x1: float
    bbox_y1: float
    source: str
    confidence: float


@dataclass(slots=True)
class TemplateSpec:
    name: str
    image: np.ndarray
    width: int
    height: int
    number_hint: str


class NumberedCircleDetector:
    def __init__(
        self,
        dpi: int = 240,
        min_radius_pt: float = 6.0,
        max_radius_pt: float = 48.0,
        template_dir: Path | None = None,
        template_threshold: float = 0.72,
        fast_mode: bool = False,
        fast_probe_dpi: int = DEFAULT_FAST_PROBE_DPI,
        allowed_number_min: int | None = None,
        allowed_number_max: int | None = None,
    ) -> None:
        if dpi < 144:
            raise ValueError("dpi must be at least 144 for stable circle detection")
        if min_radius_pt <= 0 or max_radius_pt <= min_radius_pt:
            raise ValueError("min_radius_pt and max_radius_pt are invalid")
        if not 0.5 <= template_threshold <= 0.99:
            raise ValueError("template_threshold must be between 0.5 and 0.99")
        if fast_probe_dpi < 72:
            raise ValueError("fast_probe_dpi must be at least 72")
        if (allowed_number_min is None) != (allowed_number_max is None):
            raise ValueError("allowed_number_min and allowed_number_max must be provided together")
        if allowed_number_min is not None and allowed_number_max is not None and allowed_number_max < allowed_number_min:
            raise ValueError("allowed_number_max must be greater than or equal to allowed_number_min")

        self.dpi = dpi
        self.min_radius_pt = min_radius_pt
        self.max_radius_pt = max_radius_pt
        self.template_threshold = template_threshold
        self.fast_mode = fast_mode
        self.fast_probe_dpi = fast_probe_dpi
        self.allowed_number_min = allowed_number_min
        self.allowed_number_max = allowed_number_max
        self._ocr: RapidOCR | None = None
        self._templates = self._load_templates(template_dir)

    def detect_pdf(
        self,
        pdf_path: Path,
        preview_dir: Path | None = None,
    ) -> list[DetectionRecord]:
        pdf_path = pdf_path.resolve()
        if not pdf_path.exists():
            raise FileNotFoundError(f"PDF not found: {pdf_path}")

        preview_dir = preview_dir.resolve() if preview_dir else None
        if preview_dir:
            preview_dir.mkdir(parents=True, exist_ok=True)

        records: list[DetectionRecord] = []
        document = fitz.open(pdf_path)
        try:
            for page_index in range(document.page_count):
                page = document.load_page(page_index)
                if self.fast_mode and not self._page_has_circle_candidates(page):
                    continue
                page_records, preview = self._detect_page(page)
                records.extend(self._suppress_overlapping_records(page_records))

                if preview_dir is not None:
                    preview_path = preview_dir / f"page-{page_index + 1:04d}.png"
                    preview.save(preview_path)
        finally:
            document.close()

        return records

    def _page_has_circle_candidates(self, page: fitz.Page) -> bool:
        gray, _ = self._render_page_gray(page, self.fast_probe_dpi)
        probe_circles = self._find_circles(gray, self.fast_probe_dpi, param2=16)
        return probe_circles is not None and len(probe_circles[0, :]) > 0

    def _detect_page(self, page: fitz.Page) -> tuple[list[DetectionRecord], Image.Image]:
        gray, image = self._render_page_gray(page, self.dpi)
        scale = self.dpi / 72.0
        circles = self._find_circles(gray, self.dpi, param2=20)

        text_spans = list(self._extract_digit_spans(page))
        preview = image.copy()
        preview_draw = ImageDraw.Draw(preview)

        if circles is None:
            return [], preview

        candidates = self._deduplicate_circles(circles[0, :])
        records: list[DetectionRecord] = []
        seen_keys: set[tuple[int, int, str]] = set()

        for circle in candidates:
            cx_px, cy_px, radius_px = float(circle[0]), float(circle[1]), float(circle[2])
            cx_pt, cy_pt = cx_px / scale, cy_px / scale
            radius_pt = radius_px / scale

            matched_text = self._match_text_span(text_spans, cx_pt, cy_pt, radius_pt)
            if matched_text is not None:
                number_text, confidence = matched_text
                source = "vector-text"
            else:
                number_text, confidence = self._ocr_circle(gray, cx_px, cy_px, radius_px)
                source = "ocr"

            if not number_text:
                continue

            normalized_number = self._normalize_digits(number_text)
            if not normalized_number:
                continue
            if not self._is_allowed_number(normalized_number):
                continue

            key = (round(cx_pt), round(cy_pt), normalized_number)
            if key in seen_keys:
                continue
            seen_keys.add(key)

            record = DetectionRecord(
                page=page.number + 1,
                number=normalized_number,
                center_x=round(cx_pt, 2),
                center_y=round(cy_pt, 2),
                radius=round(radius_pt, 2),
                bbox_x0=round((cx_px - radius_px) / scale, 2),
                bbox_y0=round((cy_px - radius_px) / scale, 2),
                bbox_x1=round((cx_px + radius_px) / scale, 2),
                bbox_y1=round((cy_px + radius_px) / scale, 2),
                source=source,
                confidence=round(confidence, 4),
            )
            records.append(record)
        template_records = self._detect_by_templates(page, gray)
        all_records = self._merge_records(records + template_records)
        for record in all_records:
            self._draw_preview_marker(preview_draw, record, scale)

        return all_records, preview

    def _render_page_gray(self, page: fitz.Page, dpi: int) -> tuple[np.ndarray, Image.Image]:
        scale = dpi / 72.0
        pixmap = page.get_pixmap(matrix=fitz.Matrix(scale, scale), alpha=False)
        image = Image.frombytes("RGB", (pixmap.width, pixmap.height), pixmap.samples)
        rgb = np.array(image)
        gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
        return gray, image

    def _find_circles(self, gray: np.ndarray, dpi: int, param2: int) -> np.ndarray | None:
        scale = dpi / 72.0
        blurred = cv2.GaussianBlur(gray, (9, 9), 1.5)
        min_radius_px = max(8, int(self.min_radius_pt * scale))
        max_radius_px = max(min_radius_px + 4, int(self.max_radius_pt * scale))
        return cv2.HoughCircles(
            blurred,
            cv2.HOUGH_GRADIENT,
            dp=1.2,
            minDist=max(16, min_radius_px * 2),
            param1=100,
            param2=param2,
            minRadius=min_radius_px,
            maxRadius=max_radius_px,
        )

    def _extract_digit_spans(self, page: fitz.Page) -> Iterable[tuple[fitz.Rect, str]]:
        text_dict = page.get_text("dict")
        for block in text_dict.get("blocks", []):
            for line in block.get("lines", []):
                for span in line.get("spans", []):
                    text = self._normalize_digits(span.get("text", ""))
                    if not text:
                        continue
                    rect = fitz.Rect(span["bbox"])
                    yield rect, text

    def _match_text_span(
        self,
        spans: list[tuple[fitz.Rect, str]],
        cx_pt: float,
        cy_pt: float,
        radius_pt: float,
    ) -> tuple[str, float] | None:
        best_match: tuple[str, float] | None = None
        best_distance = float("inf")

        for rect, text in spans:
            text_cx = (rect.x0 + rect.x1) / 2
            text_cy = (rect.y0 + rect.y1) / 2
            distance = math.dist((text_cx, text_cy), (cx_pt, cy_pt))
            if distance > radius_pt * 0.9:
                continue

            if rect.width > radius_pt * 2.0 or rect.height > radius_pt * 2.0:
                continue

            if distance < best_distance:
                best_distance = distance
                confidence = max(0.80, 0.99 - (distance / max(radius_pt, 1.0)) * 0.2)
                best_match = (text, confidence)

        return best_match

    def _ocr_circle(self, gray: np.ndarray, cx_px: float, cy_px: float, radius_px: float) -> tuple[str, float]:
        pad = int(max(4, radius_px * 0.35))
        x0 = max(0, int(cx_px - radius_px - pad))
        y0 = max(0, int(cy_px - radius_px - pad))
        x1 = min(gray.shape[1], int(cx_px + radius_px + pad))
        y1 = min(gray.shape[0], int(cy_px + radius_px + pad))

        if x1 <= x0 or y1 <= y0:
            return "", 0.0

        crop = gray[y0:y1, x0:x1]
        mask = np.zeros_like(crop)
        local_center = (int(cx_px - x0), int(cy_px - y0))
        local_radius = max(4, int(radius_px * 0.82))
        cv2.circle(mask, local_center, local_radius, 255, thickness=-1)
        masked = cv2.bitwise_and(crop, crop, mask=mask)

        variants = []
        resized = cv2.resize(masked, None, fx=4.0, fy=4.0, interpolation=cv2.INTER_CUBIC)
        variants.append(resized)
        variants.append(cv2.threshold(resized, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1])
        variants.append(255 - variants[-1])

        best_text = ""
        best_confidence = 0.0
        ocr_engine = self._get_ocr()
        for variant in variants:
            result, _ = ocr_engine(variant, use_det=True, use_cls=False, use_rec=True)
            if not result:
                continue
            for item in result:
                text = self._normalize_digits(item[1])
                if not text:
                    continue
                confidence = float(item[2])
                if confidence > best_confidence:
                    best_text = text
                    best_confidence = confidence

        return best_text, best_confidence

    def _normalize_digits(self, text: str) -> str:
        if not text:
            return ""
        match = DIGIT_PATTERN.search(text.replace("O", "0").replace("o", "0"))
        return match.group(0) if match else ""

    def _deduplicate_circles(self, circles: np.ndarray) -> list[np.ndarray]:
        ordered = sorted(circles, key=lambda item: float(item[2]), reverse=True)
        kept: list[np.ndarray] = []
        for circle in ordered:
            cx, cy, radius = float(circle[0]), float(circle[1]), float(circle[2])
            duplicate = False
            for existing in kept:
                ex, ey, er = float(existing[0]), float(existing[1]), float(existing[2])
                center_distance = math.dist((cx, cy), (ex, ey))
                if center_distance < min(radius, er) * 0.6 and abs(radius - er) < min(radius, er) * 0.5:
                    duplicate = True
                    break
                smaller_radius = min(radius, er)
                larger_radius = max(radius, er)
                if center_distance + smaller_radius <= larger_radius * 1.05:
                    duplicate = True
                    break
                if center_distance <= larger_radius * 0.85 and smaller_radius <= larger_radius * 0.7:
                    duplicate = True
                    break
            if not duplicate:
                kept.append(circle)
        return kept

    def _load_templates(self, template_dir: Path | None) -> list[TemplateSpec]:
        if template_dir is None:
            return []
        template_dir = template_dir.resolve()
        if not template_dir.exists():
            raise FileNotFoundError(f"template directory not found: {template_dir}")

        templates: list[TemplateSpec] = []
        for path in sorted(template_dir.glob("*")):
            if path.suffix.lower() not in {".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"}:
                continue
            image = cv2.imread(str(path), cv2.IMREAD_GRAYSCALE)
            if image is None:
                continue
            processed = self._prepare_template_image(image)
            number_hint = self._normalize_digits(path.stem)
            templates.append(
                TemplateSpec(
                    name=path.name,
                    image=processed,
                    width=processed.shape[1],
                    height=processed.shape[0],
                    number_hint=number_hint,
                )
            )
        return templates

    def _prepare_template_image(self, image: np.ndarray) -> np.ndarray:
        if image.ndim != 2:
            raise ValueError("template image must be grayscale")
        normalized = cv2.normalize(image, None, 0, 255, cv2.NORM_MINMAX)
        return cv2.threshold(normalized, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1]

    def _detect_by_templates(self, page: fitz.Page, gray: np.ndarray) -> list[DetectionRecord]:
        if not self._templates:
            return []

        page_records: list[DetectionRecord] = []
        scale = self.dpi / 72.0
        page_binary = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1]

        for template in self._templates:
            if template.width >= page_binary.shape[1] or template.height >= page_binary.shape[0]:
                continue
            result = cv2.matchTemplate(page_binary, template.image, cv2.TM_CCOEFF_NORMED)
            for x, y, score in self._iter_template_matches(result, template.width, template.height):
                score = float(result[y, x])
                center_x_px = x + (template.width / 2.0)
                center_y_px = y + (template.height / 2.0)
                radius_px = max(template.width, template.height) / 2.0
                crop_text = ""
                ocr_conf = 0.0
                if template.number_hint:
                    number = template.number_hint
                else:
                    crop_text, ocr_conf = self._ocr_circle(page_binary, center_x_px, center_y_px, radius_px)
                    number = crop_text
                number = self._normalize_digits(number)
                if not number:
                    continue
                if not self._is_allowed_number(number):
                    continue
                confidence = max(score, ocr_conf if crop_text == number else 0.0)
                page_records.append(
                    DetectionRecord(
                        page=page.number + 1,
                        number=number,
                        center_x=round(center_x_px / scale, 2),
                        center_y=round(center_y_px / scale, 2),
                        radius=round(radius_px / scale, 2),
                        bbox_x0=round(x / scale, 2),
                        bbox_y0=round(y / scale, 2),
                        bbox_x1=round((x + template.width) / scale, 2),
                        bbox_y1=round((y + template.height) / scale, 2),
                        source=f"template:{template.name}",
                        confidence=round(confidence, 4),
                    )
                )
        return page_records

    def _iter_template_matches(
        self,
        result: np.ndarray,
        template_width: int,
        template_height: int,
    ) -> list[tuple[int, int, float]]:
        locations = np.where(result >= self.template_threshold)
        candidates = sorted(
            (
                (int(x), int(y), float(result[y, x]))
                for y, x in zip(locations[0], locations[1])
            ),
            key=lambda item: item[2],
            reverse=True,
        )
        kept: list[tuple[int, int, float]] = []
        for x, y, score in candidates:
            is_duplicate = False
            for kept_x, kept_y, _ in kept:
                overlap_x = max(0, min(x + template_width, kept_x + template_width) - max(x, kept_x))
                overlap_y = max(0, min(y + template_height, kept_y + template_height) - max(y, kept_y))
                overlap_area = overlap_x * overlap_y
                template_area = template_width * template_height
                if template_area > 0 and overlap_area / template_area >= 0.45:
                    is_duplicate = True
                    break
            if not is_duplicate:
                kept.append((x, y, score))
        return kept

    def _get_ocr(self) -> RapidOCR:
        if self._ocr is None:
            self._ocr = RapidOCR()
        return self._ocr

    def _is_allowed_number(self, number_text: str) -> bool:
        if self.allowed_number_min is None or self.allowed_number_max is None:
            return True
        number_value = int(number_text)
        return self.allowed_number_min <= number_value <= self.allowed_number_max

    def _draw_preview_marker(
        self,
        preview_draw: ImageDraw.ImageDraw,
        record: DetectionRecord,
        scale: float,
    ) -> None:
        cx_px = record.center_x * scale
        cy_px = record.center_y * scale
        radius_px = record.radius * scale
        badge_cx, badge_cy, badge_radius = self._preview_badge_geometry(cx_px, cy_px, radius_px)

        preview_draw.ellipse(
            (
                cx_px - radius_px,
                cy_px - radius_px,
                cx_px + radius_px,
                cy_px + radius_px,
            ),
            outline=PREVIEW_OUTLINE_COLOR,
            width=3,
        )
        preview_draw.ellipse(
            (
                badge_cx - badge_radius,
                badge_cy - badge_radius,
                badge_cx + badge_radius,
                badge_cy + badge_radius,
            ),
            fill=PREVIEW_BADGE_COLOR,
            outline=PREVIEW_BADGE_COLOR,
            width=2,
        )
        inner_radius = max(2.0, badge_radius * 0.34)
        preview_draw.ellipse(
            (
                badge_cx - inner_radius,
                badge_cy - inner_radius,
                badge_cx + inner_radius,
                badge_cy + inner_radius,
            ),
            fill=PREVIEW_BADGE_INNER_COLOR,
        )
        preview_draw.text(
            (badge_cx + badge_radius + PREVIEW_TEXT_OFFSET_X, badge_cy + PREVIEW_TEXT_OFFSET_Y),
            record.number,
            fill=PREVIEW_OUTLINE_COLOR,
        )

    def _preview_badge_geometry(self, cx_px: float, cy_px: float, radius_px: float) -> tuple[float, float, float]:
        badge_radius = max(8.0, radius_px * 0.32)
        badge_cx = cx_px + radius_px * 1.18
        badge_cy = cy_px - radius_px * 0.88
        return badge_cx, badge_cy, badge_radius

    def _merge_records(self, records: list[DetectionRecord]) -> list[DetectionRecord]:
        merged = self._suppress_overlapping_records(records)
        return sorted(merged, key=lambda item: (item.page, item.center_y, item.center_x))

    def _suppress_overlapping_records(self, records: list[DetectionRecord]) -> list[DetectionRecord]:
        ordered = sorted(
            records,
            key=lambda item: (item.confidence, 1 if item.source.startswith("template:") else 0),
            reverse=True,
        )
        kept: list[DetectionRecord] = []
        for record in ordered:
            discard = False
            for existing in kept:
                center_distance = math.dist(
                    (record.center_x, record.center_y),
                    (existing.center_x, existing.center_y),
                )
                if center_distance > max(record.radius, existing.radius):
                    continue

                smaller_radius = min(record.radius, existing.radius)
                larger_radius = max(record.radius, existing.radius)
                if center_distance + smaller_radius <= larger_radius * 1.15:
                    discard = True
                    break
            if not discard:
                kept.append(record)
        return kept


def write_json(records: list[DetectionRecord], output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps([asdict(record) for record in records], ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def write_csv(records: list[DetectionRecord], output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = list(asdict(records[0]).keys()) if records else list(DetectionRecord.__dataclass_fields__.keys())
    with output_path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for record in records:
            writer.writerow(asdict(record))
