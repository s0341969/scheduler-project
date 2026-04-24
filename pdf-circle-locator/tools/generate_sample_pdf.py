from __future__ import annotations

from pathlib import Path

import fitz


def main() -> int:
    output_path = Path("tmp/pdfs/sample-numbered-circles.pdf").resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

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

    document.save(output_path)
    document.close()
    print(output_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
