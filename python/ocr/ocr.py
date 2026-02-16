import argparse
import json
from dataclasses import dataclass

import cv2
import numpy as np
import pytesseract


@dataclass(frozen=True)
class Roi:
    x: int
    y: int
    w: int
    h: int


def parse_roi(text: str) -> Roi:
    parts = [p.strip() for p in text.split(",") if p.strip()]
    if len(parts) != 4:
        raise ValueError("ROI must be x,y,w,h")
    x, y, w, h = map(int, parts)
    return Roi(x=x, y=y, w=w, h=h)


def preprocess(img_bgr: np.ndarray) -> np.ndarray:
    gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)

    # Upscale small UI fonts (simple, fast)
    gray = cv2.resize(gray, None, fx=2.0, fy=2.0, interpolation=cv2.INTER_CUBIC)

    # Improve contrast + binarize
    gray = cv2.GaussianBlur(gray, (3, 3), 0)
    thr = cv2.adaptiveThreshold(
        gray,
        255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY,
        31,
        5,
    )
    return thr


def run_ocr(img: np.ndarray, lang: str, psm: int) -> str:
    config = f"--psm {psm}"
    return pytesseract.image_to_string(img, lang=lang, config=config)


def main() -> int:
    ap = argparse.ArgumentParser(description="Run OCR on a screenshot or captured frame.")
    ap.add_argument("--image", required=True, help="Path to input image (PNG recommended).")
    ap.add_argument("--roi", default=None, help="Optional ROI x,y,w,h in pixels (in original image coordinates).")
    ap.add_argument("--lang", default="eng", help="Tesseract language (default: eng).")
    ap.add_argument("--psm", type=int, default=7, help="Tesseract PSM mode (default 7: single text line).")
    ap.add_argument("--json", action="store_true", help="Output JSON.")
    ap.add_argument("--debug-out", default=None, help="Optional path to write the preprocessed image.")

    args = ap.parse_args()

    img = cv2.imread(args.image, cv2.IMREAD_COLOR)
    if img is None:
        raise SystemExit(f"Could not read image: {args.image}")

    if args.roi:
        roi = parse_roi(args.roi)
        img = img[roi.y : roi.y + roi.h, roi.x : roi.x + roi.w]

    pre = preprocess(img)

    if args.debug_out:
        cv2.imwrite(args.debug_out, pre)

    text = run_ocr(pre, lang=args.lang, psm=args.psm).strip()

    if args.json:
        print(json.dumps({"text": text}, ensure_ascii=False))
    else:
        print(text)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
