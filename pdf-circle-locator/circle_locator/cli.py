from __future__ import annotations

import argparse
from pathlib import Path

from circle_locator.detector import NumberedCircleDetector, write_csv, write_json


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Detect numbered circles inside a PDF and export their coordinates."
    )
    parser.add_argument("input_pdf", type=Path, help="Path to the input PDF")
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path("output") / "pdf",
        help="Directory for exported files",
    )
    parser.add_argument(
        "--dpi",
        type=int,
        default=240,
        help="Render DPI used during detection. Higher is slower but may improve accuracy.",
    )
    parser.add_argument(
        "--min-radius-pt",
        type=float,
        default=6.0,
        help="Minimum circle radius in PDF points",
    )
    parser.add_argument(
        "--max-radius-pt",
        type=float,
        default=48.0,
        help="Maximum circle radius in PDF points",
    )
    parser.add_argument(
        "--no-preview",
        action="store_true",
        help="Disable annotated preview image export",
    )
    parser.add_argument(
        "--template-dir",
        type=Path,
        default=None,
        help="Directory containing numbered-circle template images",
    )
    parser.add_argument(
        "--template-threshold",
        type=float,
        default=0.72,
        help="Template matching threshold between 0 and 1",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    output_dir: Path = args.output_dir.resolve()
    preview_dir = None if args.no_preview else output_dir / "preview"

    detector = NumberedCircleDetector(
        dpi=args.dpi,
        min_radius_pt=args.min_radius_pt,
        max_radius_pt=args.max_radius_pt,
        template_dir=args.template_dir,
        template_threshold=args.template_threshold,
    )
    records = detector.detect_pdf(args.input_pdf, preview_dir=preview_dir)

    json_path = output_dir / "detections.json"
    csv_path = output_dir / "detections.csv"
    write_json(records, json_path)
    write_csv(records, csv_path)

    print(f"input_pdf={args.input_pdf.resolve()}")
    print(f"detections={len(records)}")
    print(f"json={json_path}")
    print(f"csv={csv_path}")
    if preview_dir is not None:
        print(f"preview_dir={preview_dir}")
    if args.template_dir is not None:
        print(f"template_dir={args.template_dir.resolve()}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
