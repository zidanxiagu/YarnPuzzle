import argparse
import json
import math
import os
from collections import Counter

from PIL import Image


def distance(a, b):
    return sum((a[index] - b[index]) ** 2 for index in range(3))


def nearest(point, centers):
    return min(range(len(centers)), key=lambda index: distance(point, centers[index]))


def initial_centers(points, count):
    ordered = sorted(points, key=lambda value: (value[0] + value[1] + value[2], value[0], value[1], value[2]))
    centers = []
    for index in range(count):
        sample_index = min(len(ordered) - 1, round(index * (len(ordered) - 1) / max(1, count - 1)))
        centers.append(list(ordered[sample_index]))
    return centers


def cluster(points, count):
    centers = initial_centers(points, count)
    for _ in range(18):
        buckets = [[] for _ in centers]
        for point in points:
            buckets[nearest(point, centers)].append(point)
        changed = False
        for index, bucket in enumerate(buckets):
            if not bucket:
                continue
            next_center = [round(sum(point[channel] for point in bucket) / len(bucket)) for channel in range(3)]
            if next_center != centers[index]:
                changed = True
                centers[index] = next_center
        if not changed:
            break
    usage = Counter(nearest(point, centers) for point in points)
    order = sorted(range(len(centers)), key=lambda index: (-usage[index], sum(centers[index]), centers[index]))
    remap = {old: new for new, old in enumerate(order)}
    return [centers[index] for index in order], remap


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--finished", required=True)
    parser.add_argument("--image-output", required=True)
    parser.add_argument("--finished-output", required=True)
    parser.add_argument("--json-output", required=True)
    parser.add_argument("--level-id", default="level_326863")
    parser.add_argument("--display-name", default="For You")
    parser.add_argument("--columns", type=int, required=True)
    parser.add_argument("--rows", type=int, required=True)
    parser.add_argument("--palette", type=int, default=16)
    args = parser.parse_args()

    source = Image.open(args.source).convert("RGBA")
    finished = Image.open(args.finished).convert("RGBA")
    os.makedirs(os.path.dirname(args.image_output), exist_ok=True)
    os.makedirs(os.path.dirname(args.finished_output), exist_ok=True)
    os.makedirs(os.path.dirname(args.json_output), exist_ok=True)
    source.save(args.image_output, "PNG", optimize=True)
    finished.save(args.finished_output, "PNG", optimize=True)

    points = []
    cells = []
    for row in range(args.rows):
        for column in range(args.columns):
            left = round(column * source.width / args.columns)
            right = round((column + 1) * source.width / args.columns)
            top = round(row * source.height / args.rows)
            bottom = round((row + 1) * source.height / args.rows)
            center_x = min(source.width - 1, (left + right) // 2)
            center_y = min(source.height - 1, (top + bottom) // 2)
            pixel = source.getpixel((center_x, center_y))
            if pixel[3] < 32:
                cells.append(None)
                continue
            # Keep the center sample. The source already contains the original pixel texture.
            point = tuple(pixel[:3])
            points.append(point)
            cells.append(point)

    palette_count = min(args.palette, max(1, len(set(points))))
    palette, remap = cluster(points, palette_count)
    cell_values = []
    for cell in cells:
        if cell is None:
            cell_values.append(-1)
        else:
            cell_values.append(remap[nearest(cell, palette)])

    definition = {
        "levelId": args.level_id,
        "displayName": args.display_name,
        "width": args.columns,
        "height": args.rows,
        "palette": [{"r": color[0] / 255.0, "g": color[1] / 255.0, "b": color[2] / 255.0, "a": 1.0} for color in palette],
        "cells": cell_values,
        "fillAnimationSeconds": 0.16,
        "completionDelaySeconds": 0.35,
        "rewardGems": 10,
        "starThreshold": 100,
        "emptyCellValue": -1,
        "artImageResource": "LevelImages/level_326863_i512",
        "finishedImageResource": "LevelImages/level_326863_finished",
        "sourceNote": "Recovered local YarnScapes level 326863 from decoded init_data; visual source is the original local i512 image.",
    }
    with open(args.json_output, "w", encoding="utf-8") as handle:
        json.dump(definition, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(json.dumps({"size": source.size, "cells": len(cell_values), "palette": len(palette), "filled": sum(value >= 0 for value in cell_values)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
