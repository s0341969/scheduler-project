import argparse
import base64
import json
import math
import os
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import cv2
import numpy as np
from rapidocr_onnxruntime import RapidOCR


DIGITS_RE = re.compile(r"\d+")
TEMPLATE_CACHE: dict[str, dict[str, dict[str, np.ndarray]]] = {}


@dataclass
class SpecItem:
    item_no: str
    inspection_method: str | None = None


@dataclass
class PatchCandidate:
    image: np.ndarray
    weight: float
    tag: str


@dataclass
class TemplateCandidate:
    value: str
    confidence: float
    source_method: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Recognize bubble numbers for scanned PDFs.")
    parser.add_argument("--request", required=True)
    parser.add_argument("--response", required=True)
    return parser.parse_args()


def decode_data_url(image_data_url: str) -> np.ndarray:
    if "," not in image_data_url:
        raise ValueError("image data url format is invalid")
    _, encoded = image_data_url.split(",", 1)
    raw = base64.b64decode(encoded)
    arr = np.frombuffer(raw, dtype=np.uint8)
    image = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if image is None:
        raise ValueError("failed to decode page image")
    return image


def clamp_rect(x: int, y: int, w: int, h: int, max_w: int, max_h: int) -> tuple[int, int, int, int]:
    x = max(0, min(x, max_w - 1))
    y = max(0, min(y, max_h - 1))
    right = max(x + 1, min(x + w, max_w))
    bottom = max(y + 1, min(y + h, max_h))
    return x, y, right - x, bottom - y


def resolve_template_directory() -> Path | None:
    script_dir = Path(__file__).resolve().parent
    candidates = [
        script_dir.parents[1] / "templates" / "numbers-001-100",
        script_dir.parents[0] / "templates" / "numbers-001-100",
        Path.cwd() / "templates" / "numbers-001-100",
    ]
    for candidate in candidates:
        if candidate.is_dir():
            return candidate
    return None


def imread_unicode(path: Path) -> np.ndarray | None:
    try:
        data = np.fromfile(str(path), dtype=np.uint8)
    except OSError:
        return None
    if data.size == 0:
        return None
    return cv2.imdecode(data, cv2.IMREAD_COLOR)


def normalize_for_similarity(image: np.ndarray, size: int = 64) -> np.ndarray:
    if image.ndim == 3:
        image = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    clahe = cv2.createCLAHE(clipLimit=2.2, tileGridSize=(8, 8)).apply(image)
    resized = cv2.resize(clahe, (size, size), interpolation=cv2.INTER_CUBIC)
    _, binary = cv2.threshold(resized, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    binary = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, np.ones((2, 2), np.uint8), iterations=1)
    return binary


def score_similarity(source: np.ndarray, template: np.ndarray) -> float:
    if source.shape != template.shape:
        source = cv2.resize(source, (template.shape[1], template.shape[0]), interpolation=cv2.INTER_AREA)
    result = cv2.matchTemplate(source, template, cv2.TM_CCOEFF_NORMED)
    return float(result[0, 0])


def load_template_bank(allowed_items: set[str]) -> dict[str, dict[str, np.ndarray]]:
    template_dir = resolve_template_directory()
    if template_dir is None:
        return {}

    cache_key = str(template_dir).lower()
    if cache_key not in TEMPLATE_CACHE:
        TEMPLATE_CACHE[cache_key] = {}

    bank = TEMPLATE_CACHE[cache_key]
    missing_items = [item for item in allowed_items if item and item not in bank]
    for item in missing_items:
        template_path = template_dir / f"{item}.png"
        if not template_path.is_file():
            continue
        image = imread_unicode(template_path)
        if image is None:
            continue

        bubble = normalize_for_similarity(image, 64)
        digit_patch_result = extract_digit_patches(
            image,
            {
                "radius": min(image.shape[0], image.shape[1]) * 0.42,
                "left": 0.0,
                "top": 0.0,
            },
            "circle_first"
        )
        digit_template = None
        if digit_patch_result is not None:
            patches, _ = digit_patch_result
            if patches:
                digit_template = normalize_for_similarity(patches[0].image, 64)

        bank[item] = {
            "bubble": bubble,
            "digit": digit_template if digit_template is not None else bubble,
        }

    return {item: bank[item] for item in allowed_items if item in bank}


def build_circle_roi(page_image: np.ndarray, circle: dict[str, Any]) -> tuple[np.ndarray, dict[str, float]] | None:
    height, width = page_image.shape[:2]
    center_x = float(circle.get("centerXRatio", circle.get("xRatio", 0.0))) * width
    center_y = float(circle.get("centerYRatio", circle.get("yRatio", 0.0))) * height
    radius = float(circle.get("radiusRatio", 0.0)) * min(width, height)
    if radius <= 1.0:
        return None

    padding = max(6.0, radius * 0.55)
    side = int(math.ceil((radius + padding) * 2.0))
    left = int(round(center_x - side / 2.0))
    top = int(round(center_y - side / 2.0))
    left, top, crop_w, crop_h = clamp_rect(left, top, side, side, width, height)
    roi = page_image[top:top + crop_h, left:left + crop_w].copy()
    if roi.size == 0:
        return None

    meta = {
        "page_width": float(width),
        "page_height": float(height),
        "center_x": center_x,
        "center_y": center_y,
        "radius": radius,
        "left": float(left),
        "top": float(top),
        "crop_w": float(crop_w),
        "crop_h": float(crop_h),
    }
    return roi, meta


def refine_local_circle(roi: np.ndarray, meta: dict[str, float]) -> dict[str, float]:
    gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
    blur = cv2.medianBlur(gray, 5)
    crop_h, crop_w = gray.shape[:2]
    expected_center = (crop_w / 2.0, crop_h / 2.0)
    expected_radius = max(6.0, float(meta["radius"]))
    circles = cv2.HoughCircles(
        blur,
        cv2.HOUGH_GRADIENT,
        dp=1.15,
        minDist=max(8.0, expected_radius * 0.9),
        param1=120,
        param2=18,
        minRadius=max(4, int(round(expected_radius * 0.55))),
        maxRadius=max(8, int(round(expected_radius * 1.35))),
    )
    if circles is None or len(circles) == 0:
        return {
            "local_center_x": expected_center[0],
            "local_center_y": expected_center[1],
            "local_radius": expected_radius,
        }

    best = None
    best_score = float("inf")
    for cx, cy, radius in circles[0]:
        distance = math.hypot(cx - expected_center[0], cy - expected_center[1])
        radius_penalty = abs(radius - expected_radius) * 0.7
        score = distance + radius_penalty
        if score < best_score:
            best_score = score
            best = (float(cx), float(cy), float(radius))

    if best is None:
        return {
            "local_center_x": expected_center[0],
            "local_center_y": expected_center[1],
            "local_radius": expected_radius,
        }

    return {
        "local_center_x": best[0],
        "local_center_y": best[1],
        "local_radius": max(4.0, best[2]),
    }


def extract_digit_patches(roi: np.ndarray, meta: dict[str, float], strategy: str) -> tuple[list[PatchCandidate], tuple[float, float, float, float]] | None:
    gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
    clahe = cv2.createCLAHE(clipLimit=2.4, tileGridSize=(8, 8)).apply(gray)

    crop_h, crop_w = gray.shape[:2]
    refined = refine_local_circle(roi, meta)
    center = (float(refined["local_center_x"]), float(refined["local_center_y"]))
    local_radius = max(6.0, float(refined["local_radius"]))
    strategy = strategy if strategy in {"circle_first", "ocr_first"} else "circle_first"
    inner_radius = max(6, int(round(local_radius * (0.72 if strategy == "ocr_first" else 0.60))))
    inner_mask = np.zeros((crop_h, crop_w), dtype=np.uint8)
    cv2.circle(inner_mask, (int(round(center[0])), int(round(center[1]))), inner_radius, 255, -1)

    masked = cv2.bitwise_and(clahe, clahe, mask=inner_mask)
    blur = cv2.GaussianBlur(masked, (3, 3), 0)
    thr = cv2.adaptiveThreshold(
        blur,
        255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY_INV,
        31,
        8,
    )
    thr = cv2.morphologyEx(thr, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8), iterations=1)
    thr = cv2.morphologyEx(thr, cv2.MORPH_CLOSE, np.ones((2, 2), np.uint8), iterations=1)

    contours, _ = cv2.findContours(thr, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    candidates: list[tuple[int, int, int, int]] = []
    min_area = max(6, int(round((crop_w * crop_h) * 0.0025)))
    for contour in contours:
        x, y, w, h = cv2.boundingRect(contour)
        area = w * h
        if area < min_area:
            continue
        if w > crop_w * 0.92 or h > crop_h * 0.92:
            continue
        if strategy == "circle_first" and (x <= 1 or y <= 1 or x + w >= crop_w - 1 or y + h >= crop_h - 1):
            continue
        candidates.append((x, y, w, h))

    x1: int
    y1: int
    x2: int
    y2: int
    patches: list[PatchCandidate] = []

    if candidates:
        x1 = min(x for x, _, _, _ in candidates)
        y1 = min(y for _, y, _, _ in candidates)
        x2 = max(x + w for x, _, w, _ in candidates)
        y2 = max(y + h for _, y, _, h in candidates)
        pad_x = max(4, int(round((x2 - x1) * 0.18)))
        pad_y = max(4, int(round((y2 - y1) * 0.22)))
        x1 = max(0, x1 - pad_x)
        y1 = max(0, y1 - pad_y)
        x2 = min(crop_w, x2 + pad_x)
        y2 = min(crop_h, y2 + pad_y)
        digit_crop = clahe[y1:y2, x1:x2]
        if digit_crop.size > 0:
            patches.append(PatchCandidate(image=digit_crop, weight=1.20 if strategy == "circle_first" else 1.32, tag="foreground-bbox"))
    else:
        x1 = max(0, int(round(center[0] - local_radius * 0.72)))
        y1 = max(0, int(round(center[1] - local_radius * 0.72)))
        x2 = min(crop_w, int(round(center[0] + local_radius * 0.72)))
        y2 = min(crop_h, int(round(center[1] + local_radius * 0.72)))

    central_sizes = [0.68, 0.84, 1.00] if strategy == "circle_first" else [0.56, 0.72, 0.90, 1.08]
    for idx, factor in enumerate(central_sizes, start=1):
        half_w = max(6, int(round(local_radius * factor)))
        half_h = max(6, int(round(local_radius * factor * 0.92)))
        cx1 = max(0, int(round(center[0] - half_w)))
        cy1 = max(0, int(round(center[1] - half_h)))
        cx2 = min(crop_w, int(round(center[0] + half_w)))
        cy2 = min(crop_h, int(round(center[1] + half_h)))
        patch = clahe[cy1:cy2, cx1:cx2]
        if patch.size > 0:
            base_weight = max(0.75, 1.05 - idx * 0.1)
            if strategy == "ocr_first" and idx <= 2:
                base_weight += 0.08
            patches.append(PatchCandidate(image=patch, weight=base_weight, tag=f"central-{idx}"))

    if strategy == "ocr_first":
        wide_half = max(8, int(round(local_radius * 1.18)))
        wx1 = max(0, int(round(center[0] - wide_half)))
        wy1 = max(0, int(round(center[1] - wide_half)))
        wx2 = min(crop_w, int(round(center[0] + wide_half)))
        wy2 = min(crop_h, int(round(center[1] + wide_half)))
        wide_patch = clahe[wy1:wy2, wx1:wx2]
        if wide_patch.size > 0:
            patches.append(PatchCandidate(image=wide_patch, weight=0.88, tag="ocr-wide"))

    if not patches:
        return None

    unique_patches: list[PatchCandidate] = []
    seen_shapes: set[tuple[int, int, str]] = set()
    for patch in patches:
        shape_key = (patch.image.shape[0], patch.image.shape[1], patch.tag)
        if shape_key in seen_shapes:
            continue
        seen_shapes.add(shape_key)
        unique_patches.append(patch)

    page_left = meta["left"] + x1
    page_top = meta["top"] + y1
    page_right = meta["left"] + x2
    page_bottom = meta["top"] + y2
    return unique_patches, (page_left, page_top, page_right, page_bottom)


def build_variants(digit_crop: np.ndarray) -> list[np.ndarray]:
    variants: list[np.ndarray] = []
    base_scales = [2.0, 3.0, 4.0]
    for scale in base_scales:
        enlarged = cv2.resize(
            digit_crop,
            None,
            fx=scale,
            fy=scale,
            interpolation=cv2.INTER_CUBIC,
        )
        clahe = cv2.createCLAHE(clipLimit=2.4, tileGridSize=(8, 8)).apply(enlarged)
        _, otsu_inv = cv2.threshold(clahe, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        adaptive_inv = cv2.adaptiveThreshold(
            clahe,
            255,
            cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
            cv2.THRESH_BINARY_INV,
            31,
            9,
        )
        dilated = cv2.dilate(otsu_inv, np.ones((2, 2), np.uint8), iterations=1)
        for mask in (otsu_inv, adaptive_inv, dilated):
            prepared = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((2, 2), np.uint8), iterations=1)
            prepared = cv2.copyMakeBorder(prepared, 12, 12, 12, 12, cv2.BORDER_CONSTANT, value=0)
            white_bg = 255 - prepared
            variants.append(white_bg)
            rotated = cv2.rotate(white_bg, cv2.ROTATE_180)
            variants.append(rotated)
    return variants


def build_bubble_similarity_variants(roi: np.ndarray) -> list[np.ndarray]:
    gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
    blur = cv2.GaussianBlur(gray, (3, 3), 0)
    _, otsu = cv2.threshold(blur, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    adaptive = cv2.adaptiveThreshold(
        blur,
        255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY_INV,
        31,
        7,
    )
    variants = []
    for candidate in (gray, blur, otsu, adaptive):
        variants.append(normalize_for_similarity(candidate, 64))
    return variants


def normalize_text(text: str) -> str:
    matches = DIGITS_RE.findall(text or "")
    return "".join(matches)


def collect_candidates(ocr_engine: RapidOCR, patch_candidates: list[PatchCandidate], allowed_items: set[str]) -> list[dict[str, Any]]:
    candidates: dict[str, dict[str, Any]] = {}
    for patch_idx, patch in enumerate(patch_candidates):
        variants = build_variants(patch.image)
        for idx, image in enumerate(variants):
            try:
                result, _ = ocr_engine(image, use_cls=True)
            except Exception:
                continue
            if not result:
                continue
            for entry in result:
                text = ""
                confidence = 0.0
                if isinstance(entry, list | tuple):
                    if len(entry) >= 3 and isinstance(entry[1], str):
                        text = entry[1]
                        confidence = float(entry[2] or 0.0)
                    elif len(entry) >= 2 and isinstance(entry[0], str):
                        text = entry[0]
                        confidence = float(entry[1] or 0.0)
                normalized = normalize_text(text)
                if not normalized:
                    continue
                if len(normalized) > 3:
                    continue
                adjustment = 0.015 * (idx % 2)
                weighted = max(0.0, min(0.99, (confidence * patch.weight) - adjustment))
                existing = candidates.get(normalized)
                if existing is None:
                    candidates[normalized] = {
                        "value": normalized,
                        "confidence": weighted,
                        "rawConfidence": confidence,
                        "voteScore": patch.weight,
                        "hitCount": 1,
                        "sourceMethod": "python-rapidocr",
                        "inItemList": normalized in allowed_items,
                        "patchTag": patch.tag,
                    }
                    continue

                existing["hitCount"] += 1
                existing["voteScore"] += patch.weight
                if weighted > existing["confidence"]:
                    existing["confidence"] = weighted
                    existing["rawConfidence"] = confidence
                    existing["patchTag"] = patch.tag

    ranked = sorted(
        candidates.values(),
        key=lambda item: (item["inItemList"], item["voteScore"], item["confidence"], item["hitCount"]),
        reverse=True,
    )
    for item in ranked:
        item.pop("patchTag", None)
    return ranked


def collect_template_candidates(
    roi: np.ndarray,
    patch_candidates: list[PatchCandidate],
    allowed_items: set[str],
    strategy: str,
) -> list[TemplateCandidate]:
    template_bank = load_template_bank(allowed_items)
    if not template_bank:
        return []

    bubble_variants = build_bubble_similarity_variants(roi)
    digit_variants = [normalize_for_similarity(patch.image, 64) for patch in patch_candidates if patch.image.size > 0]
    ranked: list[TemplateCandidate] = []
    for item, templates in template_bank.items():
        bubble_score = max((score_similarity(variant, templates["bubble"]) for variant in bubble_variants), default=0.0)
        digit_score = max((score_similarity(variant, templates["digit"]) for variant in digit_variants), default=0.0)
        if strategy == "ocr_first":
            combined = (bubble_score * 0.28) + (digit_score * 0.72)
        else:
            combined = (bubble_score * 0.42) + (digit_score * 0.58)
        ranked.append(
            TemplateCandidate(
                value=item,
                confidence=max(0.0, min(0.99, combined)),
                source_method="python-template",
            )
        )
    ranked.sort(key=lambda item: item.confidence, reverse=True)
    return ranked[:5]


def merge_candidate_sources(
    ocr_candidates: list[dict[str, Any]],
    template_candidates: list[TemplateCandidate],
    allowed_items: set[str],
    strategy: str,
) -> list[dict[str, Any]]:
    merged: dict[str, dict[str, Any]] = {}
    for candidate in ocr_candidates:
        value = candidate["value"]
        merged[value] = dict(candidate)

    for candidate in template_candidates:
        entry = merged.get(candidate.value)
        if entry is None:
            merged[candidate.value] = {
                "value": candidate.value,
                "confidence": candidate.confidence,
                "rawConfidence": candidate.confidence,
                "voteScore": 0.8,
                "hitCount": 1,
                "sourceMethod": candidate.source_method,
                "inItemList": candidate.value in allowed_items,
                "templateConfidence": candidate.confidence,
            }
            continue

        entry["templateConfidence"] = candidate.confidence
        template_blend = 0.20 if strategy == "ocr_first" else 0.28
        entry["confidence"] = min(0.99, max(float(entry["confidence"]), (float(entry["confidence"]) * (1.0 - template_blend)) + (candidate.confidence * template_blend)))
        if candidate.confidence >= 0.78:
            entry["hitCount"] = int(entry.get("hitCount", 1)) + 1
            entry["voteScore"] = float(entry.get("voteScore", 0.0)) + 0.6
        if str(entry.get("sourceMethod", "")).startswith("python-rapidocr"):
            entry["sourceMethod"] = "python-hybrid"

    ranked = sorted(
        merged.values(),
        key=lambda item: (item["inItemList"], item.get("voteScore", 0.0), item["confidence"], item.get("templateConfidence", 0.0)),
        reverse=True,
    )
    return ranked


def build_suggestion(circle: dict[str, Any], spec_items: dict[str, SpecItem], candidates: list[dict[str, Any]], threshold: float) -> dict[str, Any] | None:
    allowed = [candidate for candidate in candidates if candidate["inItemList"]]
    if not allowed:
        return None

    best = allowed[0]
    second = allowed[1] if len(allowed) > 1 else None
    best_value = best["value"]
    confidence = float(best["confidence"])
    hit_count = int(best.get("hitCount", 1))
    vote_score = float(best.get("voteScore", 0.0))
    template_confidence = float(best.get("templateConfidence", 0.0))
    if hit_count >= 2:
        confidence = min(0.99, confidence + 0.04)
    if vote_score >= 2.0:
        confidence = min(0.99, confidence + 0.03)
    if template_confidence >= 0.82:
        confidence = min(0.99, confidence + 0.04)
    need_review = confidence < threshold
    if second is not None and (confidence - float(second["confidence"])) < 0.08:
        need_review = True
    if len(best_value) == 1 and hit_count < 2:
        need_review = True

    spec = spec_items[best_value]
    return {
        "itemNo": best_value,
        "inspectionMethod": spec.inspection_method,
        "pageNo": int(circle["pageNo"]),
        "xRatio": float(circle.get("xRatio", 0.0)),
        "yRatio": float(circle.get("yRatio", 0.0)),
        "radiusRatio": float(circle.get("radiusRatio", 0.0)),
        "widthRatio": float(circle.get("widthRatio", 0.0)),
        "heightRatio": float(circle.get("heightRatio", 0.0)),
        "centerXRatio": float(circle.get("centerXRatio", circle.get("xRatio", 0.0))),
        "centerYRatio": float(circle.get("centerYRatio", circle.get("yRatio", 0.0))),
        "confidence": round(min(0.99, confidence), 4),
        "matchSource": "python-rapidocr-circle",
        "sourceMethod": "python-rapidocr-circle",
        "matchedText": best_value,
        "needReview": need_review,
        "candidates": candidates[:3],
    }


def main() -> int:
    args = parse_args()
    with open(args.request, "r", encoding="utf-8") as fp:
        payload = json.load(fp)

    spec_items = {
        str(item.get("itemNo", "")).strip(): SpecItem(
            item_no=str(item.get("itemNo", "")).strip(),
            inspection_method=item.get("inspectionMethod"),
        )
        for item in payload.get("specItems", [])
        if str(item.get("itemNo", "")).strip()
    }
    allowed_items = set(spec_items.keys())
    page_images = {
        int(page.get("pageNo", 0)): decode_data_url(page.get("imageDataUrl", ""))
        for page in payload.get("pageImages", [])
        if page.get("imageDataUrl")
    }
    circles_by_page: dict[int, list[dict[str, Any]]] = {}
    for circle in payload.get("circles", []):
        page_no = int(circle.get("pageNo", 0))
        if page_no <= 0:
            continue
        circles_by_page.setdefault(page_no, []).append(circle)
    strategy = str(payload.get("strategy", "circle_first")).strip().lower() or "circle_first"
    if strategy not in {"circle_first", "ocr_first"}:
        strategy = "circle_first"

    warnings: list[str] = []
    suggestions: list[dict[str, Any]] = []
    ocr_engine = RapidOCR()
    threshold = float(payload.get("ocrConfidenceThreshold", 0.55))
    template_bank_size = len(load_template_bank(allowed_items))
    total_circles = 0
    roi_failures = 0
    digit_patch_failures = 0
    candidate_failures = 0

    for page_no, circles in sorted(circles_by_page.items()):
        page_image = page_images.get(page_no)
        if page_image is None:
            warnings.append(f"page {page_no}: missing page image")
            continue

        for circle in circles:
            total_circles += 1
            built = build_circle_roi(page_image, circle)
            if built is None:
                roi_failures += 1
                continue
            roi, meta = built
            digit_patch_result = extract_digit_patches(roi, meta, strategy)
            if digit_patch_result is None:
                digit_patch_failures += 1
                continue
            patch_candidates, _ = digit_patch_result
            ocr_candidates = collect_candidates(ocr_engine, patch_candidates, allowed_items)
            template_candidates = collect_template_candidates(roi, patch_candidates, allowed_items, strategy)
            candidates = merge_candidate_sources(ocr_candidates, template_candidates, allowed_items, strategy)
            if not candidates:
                candidate_failures += 1
                continue
            suggestion = build_suggestion(circle, spec_items, candidates, threshold)
            if suggestion is None:
                candidate_failures += 1
                continue
            suggestions.append(suggestion)

    warnings.append(
        "Python 掃描辨識統計："
        f"策略 {strategy}、"
        f"圈候選 {total_circles}、模板庫 {template_bank_size}、命中 {len(suggestions)}、"
        f"ROI失敗 {roi_failures}、數字區失敗 {digit_patch_failures}、候選不足 {candidate_failures}"
    )

    result = {
        "suggestions": suggestions,
        "warnings": warnings,
    }
    os.makedirs(os.path.dirname(args.response), exist_ok=True)
    with open(args.response, "w", encoding="utf-8") as fp:
        json.dump(result, fp, ensure_ascii=False)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # pragma: no cover
        print(str(exc), file=sys.stderr)
        raise
