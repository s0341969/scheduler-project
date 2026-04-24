from __future__ import annotations

from pathlib import Path

import fitz

from circle_locator.detector import DetectionRecord, NumberedCircleDetector


def test_normalize_digits_extracts_digits() -> None:
    detector = NumberedCircleDetector()

    assert detector._normalize_digits("A12B") == "12"
    assert detector._normalize_digits("O7") == "07"
    assert detector._normalize_digits("") == ""


def test_template_dir_missing_raises() -> None:
    missing = Path("tests/does-not-exist")

    try:
        NumberedCircleDetector(template_dir=missing)
    except FileNotFoundError:
        pass
    else:
        raise AssertionError("Expected FileNotFoundError for missing template directory")


def test_suppress_overlapping_records_keeps_highest_confidence() -> None:
    detector = NumberedCircleDetector()
    records = [
        DetectionRecord(
            page=1,
            number="1",
            center_x=100.0,
            center_y=100.0,
            radius=20.0,
            bbox_x0=80.0,
            bbox_y0=80.0,
            bbox_x1=120.0,
            bbox_y1=120.0,
            source="vector-text",
            confidence=0.99,
        ),
        DetectionRecord(
            page=1,
            number="1",
            center_x=98.0,
            center_y=100.0,
            radius=10.0,
            bbox_x0=88.0,
            bbox_y0=90.0,
            bbox_x1=108.0,
            bbox_y1=110.0,
            source="ocr",
            confidence=0.70,
        ),
    ]

    kept = detector._suppress_overlapping_records(records)

    assert len(kept) == 1
    assert kept[0].confidence == 0.99


def test_detect_pdf_raises_for_missing_file() -> None:
    detector = NumberedCircleDetector()

    try:
        detector.detect_pdf(Path("tests/missing.pdf"))
    except FileNotFoundError:
        pass
    else:
        raise AssertionError("Expected FileNotFoundError for missing PDF")


def test_detector_defers_ocr_initialization() -> None:
    detector = NumberedCircleDetector()

    assert detector._ocr is None


def test_preview_badge_geometry_places_colored_marker_next_to_circle() -> None:
    detector = NumberedCircleDetector()

    badge_cx, badge_cy, badge_radius = detector._preview_badge_geometry(100.0, 120.0, 30.0)

    assert badge_cx > 130.0
    assert badge_cy < 120.0
    assert badge_radius >= 8.0


def test_sample_pdf_generation_and_open() -> None:
    document = fitz.open()
    page = document.new_page(width=200, height=200)
    page.draw_circle((100, 100), 20, color=(0, 0, 0), width=1.2)
    page.insert_text((92, 108), "1", fontsize=20, color=(0, 0, 0))
    pdf_bytes = document.tobytes()
    document.close()

    opened = fitz.open(stream=pdf_bytes, filetype="pdf")
    try:
        assert opened.page_count == 1
    finally:
        opened.close()


def test_detect_page_sample_pdf_returns_only_three_expected_records() -> None:
    document = fitz.open()
    page = document.new_page(width=595, height=842)
    samples = [
        ((120, 140), 18, "1"),
        ((250, 320), 20, "12"),
        ((410, 540), 22, "7"),
    ]

    for center, radius, text in samples:
        page.draw_circle(center, radius, color=(0, 0, 0), width=1.2)
        page.insert_text(
            (center[0] - radius * 0.45, center[1] + radius * 0.35),
            text,
            fontsize=radius * 1.1,
            color=(0, 0, 0),
        )

    detector = NumberedCircleDetector()
    records, _ = detector._detect_page(page)
    document.close()

    assert [record.number for record in records] == ["1", "12", "7"]
