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
