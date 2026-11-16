"""Compare a reconstructed level with the APK's actual small pixel PNG.

The original analyzer is IlLlLl1IL.java: alpha >= 100, D65 Lab, delta E <= 2,
first-color representatives, average unique RGBA colors, then nearest group.
This audit never modifies level data or original evidence.
"""
import argparse
import hashlib
import json
import math
from collections import Counter
from pathlib import Path

from PIL import Image


def lab(rgba):
    rgb = [v / 255 for v in rgba[:3]]
    r, g, b = [(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4) for v in rgb]
    xyz = [(0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047,
           0.2126 * r + 0.7152 * g + 0.0722 * b,
           (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883]
    x, y, z = [v ** (1 / 3) if v > 0.008856 else 7.787 * v + 16 / 116 for v in xyz]
    return (116 * y - 16, 500 * (x - y), 200 * (y - z))


def delta(a, b):
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def analyze(source):
    image = Image.open(source).convert("RGBA")
    pixels = [image.getpixel((x, y)) for y in range(image.height) for x in range(image.width)]
    pixels = [tuple(c) if c[3] >= 100 else (0, 0, 0, 0) for c in pixels]
    colors = list(dict.fromkeys(c for c in pixels if c[3] > 0))
    groups, representatives = [], []
    for color in colors:
        point = lab(color)
        index = next((i for i, ref in enumerate(representatives) if delta(point, ref) <= 2), None)
        if index is None:
            representatives.append(point)
            groups.append([color])
        else:
            groups[index].append(color)
    palette = [tuple(sum(c[k] for c in group) // len(group) for k in range(4)) for group in groups]
    centers = [lab(c) for c in palette]
    mapping = {c: min(range(len(centers)), key=lambda i: delta(lab(c), centers[i])) for c in colors}
    cells = [mapping[c] if c[3] > 0 else -1 for c in pixels]
    return image.size, palette, cells


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--level", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    size, palette, expected = analyze(args.source)
    actual = json.loads(Path(args.level).read_text(encoding="utf-8-sig"))
    current = actual["cells"]
    reconstructed = [tuple(round(c[k] * 255) for k in ("r", "g", "b", "a")) for c in actual["palette"]]
    report = {
        "source": str(Path(args.source).resolve()),
        "sourceSha256": hashlib.sha256(Path(args.source).read_bytes()).hexdigest(),
        "sourceRole": "finished_url small PNG; one PNG pixel is one game cell",
        "algorithmEvidence": "decoded/jadx/sources/defpackage/IlLlLl1IL.java:98-163,178-202,305-421",
        "expectedSize": list(size), "actualSize": [actual["width"], actual["height"]],
        "expectedPaintableCells": sum(c >= 0 for c in expected),
        "actualPaintableCells": sum(c >= 0 for c in current),
        "expectedColorGroups": len(palette), "actualColorGroups": len(reconstructed),
        "expectedPaletteRgba": palette, "actualPaletteRgba": reconstructed,
        "cellGroupMismatches": sum(a != b for a, b in zip(expected, current)) if len(expected) == len(current) else None,
        "expectedGroupCounts": dict(sorted(Counter(c for c in expected if c >= 0).items())),
        "actualGroupCounts": dict(sorted(Counter(c for c in current if c >= 0).items())),
        "pass": size == (actual["width"], actual["height"]) and palette == reconstructed and expected == current,
        "evidenceStatus": "recovered algorithm; pending comparison with original runtime color_analysis_result cache",
    }
    target = Path(args.output)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items() if k not in ("expectedPaletteRgba", "actualPaletteRgba", "expectedGroupCounts", "actualGroupCounts")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
