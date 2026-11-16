"""Recover every locally captured YarnScapes level into a Unity Resources DTO.

The small ``*_finished.png`` files are the authoritative maps: one pixel is one
game cell.  The grouping algorithm is shared with ``AuditLevel.py`` and has
already been checked against the existing level_326863.json byte-for-byte at
the palette/cell level.  The larger ``*_source.webp`` files are presentation
art, so this tool also writes PNG copies that Unity can import reliably.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from AuditLevel import analyze


def unity_guid(path: Path) -> str:
    """Return a stable Unity-style GUID for generated assets."""
    return hashlib.md5(path.as_posix().encode("utf-8")).hexdigest()


def write_texture_meta(path: Path) -> None:
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        return
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {unity_guid(path)}\n"
        "TextureImporter:\n"
        "  internalIDToNameTable: []\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 0\n"
        "    sRGBTexture: 1\n"
        "    linearTexture: 0\n"
        "    fadeOut: 0\n"
        "    borderMipMap: 0\n"
        "    mipMapsPreserveCoverage: 0\n"
        "    alphaTestReferenceValue: 0.5\n"
        "    mipMapFadeDistanceStart: 1\n"
        "    mipMapFadeDistanceEnd: 3\n"
        "  bumpmap:\n"
        "    convertToNormalMap: 0\n"
        "    externalNormalMap: 0\n"
        "    heightScale: 0.25\n"
        "    normalMapFilter: 0\n"
        "    flipGreenChannel: 0\n"
        "  isReadable: 0\n"
        "  streamingMipmaps: 0\n"
        "  streamingMipmapsPriority: 0\n"
        "  vTOnly: 0\n"
        "  ignoreMipmapLimit: 0\n"
        "  grayScaleToAlpha: 0\n"
        "  generateCubemap: 6\n"
        "  cubemapConvolution: 0\n"
        "  seamlessCubemap: 0\n"
        "  textureFormat: 1\n"
        "  maxTextureSize: 4096\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 1\n"
        "    aniso: 1\n"
        "    mipBias: 0\n"
        "    wrapU: 1\n"
        "    wrapV: 1\n"
        "    wrapW: 1\n"
        "  nPOTScale: 0\n"
        "  lightmap: 0\n"
        "  compressionQuality: 50\n"
        "  spriteMode: 0\n"
        "  spriteExtrude: 1\n"
        "  spriteMeshType: 1\n"
        "  alignment: 0\n"
        "  spritePivot: {x: 0.5, y: 0.5}\n"
        "  spritePixelsToUnits: 100\n"
        "  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n"
        "  spriteGenerateFallbackPhysicsShape: 1\n"
        "  alphaUsage: 1\n"
        "  alphaIsTransparency: 1\n"
        "  textureType: 0\n"
        "  textureShape: 1\n"
        "  singleChannelComponent: 0\n"
        "  flipbookRows: 1\n"
        "  flipbookColumns: 1\n"
        "  maxTextureSizeSet: 0\n"
        "  compressionQualitySet: 0\n"
        "  textureFormatSet: 0\n"
        "  ignorePngGamma: 0\n"
        "  applyGammaDecoding: 0\n"
        "  swizzle: 50462976\n"
        "  cookieLightType: 0\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def write_text_meta(path: Path) -> None:
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        return
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {unity_guid(path)}\n"
        "TextScriptImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def deduplicate_catalog(entries: list[dict]) -> list[dict]:
    result = []
    seen = set()
    for entry in entries:
        level_id = str(entry.get("id", "")).strip()
        if not re.fullmatch(r"\d+", level_id) or level_id in seen:
            continue
        seen.add(level_id)
        result.append(entry)
    return result


def copy_source_png(source_path: Path, target_path: Path) -> None:
    if target_path.exists():
        return
    with Image.open(source_path) as image:
        image.convert("RGBA").save(target_path, "PNG", optimize=True)
    write_texture_meta(target_path)


def write_unfilled(source_path: Path, target_path: Path) -> None:
    if target_path.exists():
        return
    with Image.open(source_path) as image:
        rgba = image.convert("RGBA")
        pixels = rgba.load()
        for y in range(rgba.height):
            for x in range(rgba.width):
                r, g, b, a = pixels[x, y]
                luminance = r * 0.2126 + g * 0.7152 + b * 0.0722
                gray = max(0, min(255, int(120 + int(135 * luminance / 255))))
                pixels[x, y] = (gray, gray, gray, a)
        rgba.save(target_path, "PNG", optimize=True)
    write_texture_meta(target_path)


def make_definition(entry: dict, level_id: str, finished_path: Path, source_png: Path, unfilled_png: Path) -> dict:
    size, palette, cells = analyze(finished_path)
    level_name = str(entry.get("groupCategory", "For You")).strip() or "For You"
    definition = {
        "levelId": f"level_{level_id}",
        "displayName": f"{level_name} {level_id}",
        "width": size[0],
        "height": size[1],
        "palette": [dict(zip(("r", "g", "b", "a"), (v / 255 for v in color))) for color in palette],
        "cells": cells,
        "emptyCellValue": -1,
        "artImageResource": f"LevelImages/Local/{source_png.stem}",
        "finishedImageResource": f"LevelImages/Local/{finished_path.stem}",
        "unfilledImageResource": f"LevelImages/Local/{unfilled_png.stem}",
        "fillAnimationSeconds": 0.16,
        "completionDelaySeconds": 0.35,
        "rewardGems": 0,
        "starThreshold": 0,
        "sourceNote": "Pixel map recovered from APK finished_url PNG; alpha >=100, D65 Lab deltaE<=2; source-order groups. i512 is presentation art only.",
        "pixelMapSha256": hashlib.sha256(finished_path.read_bytes()).hexdigest(),
        "paintId": entry.get("paintId", ""),
        "groupCategory": entry.get("groupCategory", ""),
        "categories": entry.get("categories", ""),
        "dataEvidenceStatus": "recovered; pending runtime cache comparison",
        "animationEvidenceStatus": "reimplemented placeholders; timing unverified",
        "unfilledMaterialEvidenceStatus": "reimplemented grayscale approximation; original GL material pending",
    }
    return definition


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", default="../Assets/Original/level_catalog.json")
    parser.add_argument("--asset-dir", default="../Assets/Resources/LevelImages/Local")
    parser.add_argument("--levels-dir", default="../Assets/Resources/Levels")
    parser.add_argument("--manifest", default="../Assets/Original/recovered_level_manifest.json")
    parser.add_argument("--runtime-catalog", default="../Assets/Resources/Levels/level_catalog.json")
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()

    catalog_path = Path(args.catalog).resolve()
    asset_dir = Path(args.asset_dir).resolve()
    levels_dir = Path(args.levels_dir).resolve()
    manifest_path = Path(args.manifest).resolve()
    runtime_catalog_path = Path(args.runtime_catalog).resolve()
    asset_dir.mkdir(parents=True, exist_ok=True)
    levels_dir.mkdir(parents=True, exist_ok=True)

    entries = deduplicate_catalog(json.loads(catalog_path.read_text(encoding="utf-8-sig")))
    manifest = []
    missing = []
    generated = 0
    for entry in entries:
        level_id = str(entry["id"])
        finished = asset_dir / f"level_{level_id}_finished.png"
        source_webp = asset_dir / f"level_{level_id}_source.webp"
        source_png = asset_dir / f"level_{level_id}_source.png"
        unfilled_png = asset_dir / f"level_{level_id}_unfilled.png"
        level_json = levels_dir / f"level_{level_id}.json"
        if not finished.exists() or not source_webp.exists():
            missing.append(level_id)
            continue

        copy_source_png(source_webp, source_png)
        write_unfilled(source_webp, unfilled_png)
        if args.overwrite or not level_json.exists():
            definition = make_definition(entry, level_id, finished, source_png, unfilled_png)
            level_json.write_text(json.dumps(definition, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            write_text_meta(level_json)
            generated += 1
        else:
            definition = json.loads(level_json.read_text(encoding="utf-8-sig"))
            size, palette, cells = analyze(finished)

        manifest.append({
            "id": level_id,
            "levelResource": f"Levels/level_{level_id}",
            "displayName": f"{str(entry.get('groupCategory', 'For You')).strip() or 'For You'} {level_id}",
            "groupCategory": entry.get("groupCategory", ""),
            "categories": entry.get("categories", ""),
            "paintId": entry.get("paintId", ""),
            "sourceResource": f"LevelImages/Local/{source_png.stem}",
            "finishedResource": f"LevelImages/Local/{finished.stem}",
            "unfilledResource": f"LevelImages/Local/{unfilled_png.stem}",
            "width": definition.get("width", size[0] if 'size' in locals() else 0),
            "height": definition.get("height", size[1] if 'size' in locals() else 0),
            "paletteCount": len(definition.get("palette", palette if 'palette' in locals() else [])),
            "paintableCells": sum(v >= 0 for v in definition.get("cells", cells if 'cells' in locals() else [])),
            "pixelMapSha256": hashlib.sha256(finished.read_bytes()).hexdigest(),
            "levelJson": str(level_json.relative_to(catalog_path.parents[1])).replace("\\", "/"),
        })

    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps({
        "schemaVersion": 1,
        "sourceCatalog": str(catalog_path.relative_to(catalog_path.parents[1])).replace("\\", "/"),
        "uniqueLevelCount": len(manifest),
        "generatedLevelCount": generated,
        "missingLevelIds": missing,
        "levels": manifest,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    write_text_meta(manifest_path)

    runtime_catalog_path.parent.mkdir(parents=True, exist_ok=True)
    runtime_catalog_path.write_text(json.dumps({
        "schemaVersion": 1,
        "levels": [{
            "levelId": item["levelResource"].split("/")[-1],
            "displayName": item["displayName"],
            "groupCategory": item["groupCategory"],
            "categories": item["categories"],
            "sourceResource": item["sourceResource"],
            "finishedResource": item["finishedResource"],
            "unfilledResource": item["unfilledResource"],
        } for item in manifest],
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    write_text_meta(runtime_catalog_path)
    print(json.dumps({
        "catalogEntries": len(entries),
        "uniqueLevels": len(manifest),
        "generated": generated,
        "missing": missing,
        "manifest": str(manifest_path),
        "runtimeCatalog": str(runtime_catalog_path),
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
