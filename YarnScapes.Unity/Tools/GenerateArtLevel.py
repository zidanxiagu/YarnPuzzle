"""Import the original pixel map without quantizing its textured preview.

finished_url is the small, authoritative pixel map. i512_url is presentation art.
Recovered analysis rules are shared with AuditLevel; runtime cache parity remains
a separate acceptance check.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from AuditLevel import analyze


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, help="original textured i512 image")
    parser.add_argument("--finished", required=True, help="original small pixel-map PNG")
    parser.add_argument("--image-output", required=True)
    parser.add_argument("--finished-output", required=True)
    parser.add_argument("--json-output", required=True)
    parser.add_argument("--level-id", default="level_326863")
    parser.add_argument("--display-name", default="For You 326863")
    parser.add_argument("--columns", type=int)
    parser.add_argument("--rows", type=int)
    args = parser.parse_args()
    size, palette, cells = analyze(args.finished)
    if args.columns is not None and args.columns != size[0]:
        raise ValueError("Requested width differs from the original pixel PNG")
    if args.rows is not None and args.rows != size[1]:
        raise ValueError("Requested height differs from the original pixel PNG")

    source = Image.open(args.source).convert("RGBA")
    pixel_map = Image.open(args.finished).convert("RGBA")
    for target in (args.image_output, args.finished_output, args.json_output):
        Path(target).parent.mkdir(parents=True, exist_ok=True)
    source.save(args.image_output, "PNG", optimize=True)
    pixel_map.save(args.finished_output, "PNG", optimize=True)

    # Preserve the original weave while removing hue. This is explicitly an
    # approximation, not a recovered copy of the original OpenGL material.
    rgba = np.array(source)
    luminance = rgba[:, :, 0] * 0.2126 + rgba[:, :, 1] * 0.7152 + rgba[:, :, 2] * 0.0722
    gray = (120 + np.floor(135 * luminance / 255)).astype(np.uint8)
    rgba[:, :, :3] = gray[:, :, None]
    gray_path = Path(args.image_output).with_name(args.level_id + "_unfilled.png")
    Image.fromarray(rgba).save(gray_path, "PNG", optimize=True)

    definition = {
        "levelId": args.level_id, "displayName": args.display_name,
        "width": size[0], "height": size[1],
        "palette": [dict(zip(("r", "g", "b", "a"), (v / 255 for v in color))) for color in palette],
        "cells": cells, "emptyCellValue": -1,
        "artImageResource": "LevelImages/" + Path(args.image_output).stem,
        "finishedImageResource": "LevelImages/" + Path(args.finished_output).stem,
        "unfilledImageResource": "LevelImages/" + gray_path.stem,
        "fillAnimationSeconds": 0.16, "completionDelaySeconds": 0.35,
        "rewardGems": 0, "starThreshold": 0,
        "sourceNote": "Pixel map recovered from APK finished_url PNG; alpha >=100, D65 Lab deltaE<=2; source-order groups. i512 is presentation art only.",
        "pixelMapSha256": hashlib.sha256(Path(args.finished).read_bytes()).hexdigest(),
        "dataEvidenceStatus": "recovered; pending runtime cache comparison",
        "animationEvidenceStatus": "reimplemented placeholders; timing unverified",
        "unfilledMaterialEvidenceStatus": "reimplemented grayscale approximation; original GL material pending",
    }
    Path(args.json_output).write_text(json.dumps(definition, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"size": size, "cells": len(cells), "palette": len(palette), "paintable": sum(v >= 0 for v in cells)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
