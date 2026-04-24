from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


DEFAULT_OUTPUT_DIR = Path("templates") / "numbers-001-100"
DEFAULT_FONT_CANDIDATES = [
    Path(r"C:\Windows\Fonts\timesbd.ttf"),
    Path(r"C:\Windows\Fonts\georgiab.ttf"),
    Path(r"C:\Windows\Fonts\arialbd.ttf"),
]


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Generate numbered circle template images for values 1 through 100."
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help="Directory where generated PNG templates will be written.",
    )
    parser.add_argument(
        "--start",
        type=int,
        default=1,
        help="First number to generate.",
    )
    parser.add_argument(
        "--end",
        type=int,
        default=100,
        help="Last number to generate.",
    )
    parser.add_argument(
        "--image-size",
        type=int,
        default=96,
        help="Square image size in pixels.",
    )
    return parser


def resolve_font(image_size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    font_size = max(26, int(image_size * 0.40))
    for candidate in DEFAULT_FONT_CANDIDATES:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), font_size)
    return ImageFont.load_default()


def fit_font(text: str, image_size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for size in range(max(22, int(image_size * 0.46)), 18, -2):
        for candidate in DEFAULT_FONT_CANDIDATES:
            if candidate.exists():
                font = ImageFont.truetype(str(candidate), size)
                bbox = font.getbbox(text)
                text_width = bbox[2] - bbox[0]
                text_height = bbox[3] - bbox[1]
                if text_width <= image_size * 0.58 and text_height <= image_size * 0.42:
                    return font
    return resolve_font(image_size)


def draw_number_template(number: int, image_size: int) -> Image.Image:
    image = Image.new("L", (image_size, image_size), color=255)
    draw = ImageDraw.Draw(image)

    outer_margin = max(6, int(image_size * 0.10))
    line_width = max(3, int(image_size * 0.05))
    draw.ellipse(
        (
            outer_margin,
            outer_margin,
            image_size - outer_margin,
            image_size - outer_margin,
        ),
        outline=0,
        width=line_width,
    )

    text = str(number)
    font = fit_font(text, image_size)
    bbox = draw.textbbox((0, 0), text, font=font)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    text_x = (image_size - text_width) / 2 - bbox[0]
    text_y = (image_size - text_height) / 2 - bbox[1] - (image_size * 0.03)
    draw.text((text_x, text_y), text, font=font, fill=0)

    return image


def main() -> int:
    args = build_parser().parse_args()
    if args.start < 0 or args.end < args.start:
        raise ValueError("invalid generation range")
    if args.image_size < 48:
        raise ValueError("image_size must be at least 48")

    output_dir = args.output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    for number in range(args.start, args.end + 1):
        image = draw_number_template(number, args.image_size)
        image.save(output_dir / f"{number}.png")

    print(output_dir)
    print(f"generated={args.end - args.start + 1}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
