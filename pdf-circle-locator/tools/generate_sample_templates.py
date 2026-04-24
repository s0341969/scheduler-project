from __future__ import annotations

from pathlib import Path

import fitz
from PIL import Image


def main() -> int:
    pdf_path = Path("tmp/pdfs/sample-numbered-circles.pdf").resolve()
    template_dir = Path("templates").resolve()
    template_dir.mkdir(parents=True, exist_ok=True)

    document = fitz.open(pdf_path)
    page = document.load_page(0)
    scale = 240 / 72.0
    pixmap = page.get_pixmap(matrix=fitz.Matrix(scale, scale), alpha=False)
    image = Image.frombytes("RGB", (pixmap.width, pixmap.height), pixmap.samples)

    circles = [
        ("1.png", (120, 140, 18)),
        ("12.png", (250, 320, 20)),
        ("7.png", (410, 540, 22)),
    ]
    for filename, (cx_pt, cy_pt, radius_pt) in circles:
        cx_px = int(cx_pt * scale)
        cy_px = int(cy_pt * scale)
        radius_px = int(radius_pt * scale)
        pad = int(radius_px * 0.35)
        crop = image.crop(
            (
                cx_px - radius_px - pad,
                cy_px - radius_px - pad,
                cx_px + radius_px + pad,
                cy_px + radius_px + pad,
            )
        )
        crop.save(template_dir / filename)

    document.close()
    print(template_dir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
