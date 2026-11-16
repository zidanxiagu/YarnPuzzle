"""Extract the original YarnScapes art/audio payload from the local APK.

The APK is kept outside the Unity project as the immutable source. This tool
copies game-facing Compose resources into Assets/Original/Raw, converts WebP
textures to PNG, and places a small set of key textures/audio clips under
Assets/Resources so the offline vertical slice can load them at runtime.
"""

from pathlib import Path
import hashlib
import json
import shutil
import zipfile

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
APK = ROOT / "base.apk"
PROJECT = Path(__file__).resolve().parents[1]
RAW = PROJECT / "Assets" / "Original" / "Raw"
CONVERTED = PROJECT / "Assets" / "Original" / "Textures"
RUNTIME_TEXTURES = PROJECT / "Assets" / "Resources" / "Textures"
RUNTIME_AUDIO = PROJECT / "Assets" / "Resources" / "Audio"


def is_game_resource(name: str) -> bool:
    return (
        name.startswith("assets/composeResources/yarnscapes.shared.generated.resources/")
        and (
            "/drawable" in name
            or "/files/render/" in name
            or "/files/sound/" in name
            or "/files/music/" in name
            or "/files/lottie/" in name
            or "/files/json/" in name
        )
    )


def copy_bytes(zf: zipfile.ZipFile, archive_name: str, target: Path) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    with zf.open(archive_name) as source, target.open("wb") as destination:
        shutil.copyfileobj(source, destination)


def copy_texture(zf: zipfile.ZipFile, archive_name: str) -> None:
    relative = archive_name.split("/drawable", 1)[-1].lstrip("/")
    raw_target = RAW / archive_name.replace("/", "_")
    copy_bytes(zf, archive_name, raw_target)

    if archive_name.lower().endswith((".webp", ".png", ".jpg", ".jpeg")):
        converted_target = CONVERTED / relative
        converted_target = converted_target.with_suffix(".png")
        converted_target.parent.mkdir(parents=True, exist_ok=True)
        with zf.open(archive_name) as source:
            image = Image.open(source).convert("RGBA")
            image.save(converted_target, "PNG")

        if Path(archive_name).name in {"yarn_piece.png", "yarn_piece_dark.png", "pixel_style_mask.png"} or Path(archive_name).name.startswith("spool_"):
            runtime_target = RUNTIME_TEXTURES / Path(archive_name).name
            runtime_target = runtime_target.with_suffix(".png")
            runtime_target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(converted_target, runtime_target)


def main() -> None:
    if not APK.exists():
        raise SystemExit(f"Missing source APK: {APK}")

    for folder in (RAW, CONVERTED, RUNTIME_TEXTURES, RUNTIME_AUDIO):
        folder.mkdir(parents=True, exist_ok=True)

    manifest = {
        "source": APK.name,
        "sha256": hashlib.sha256(APK.read_bytes()).hexdigest(),
        "files": [],
    }

    with zipfile.ZipFile(APK) as zf:
        for info in zf.infolist():
            name = info.filename
            if not is_game_resource(name):
                continue

            manifest["files"].append({"archive": name, "size": info.file_size})
            if name.lower().endswith((".webp", ".png", ".jpg", ".jpeg")):
                copy_texture(zf, name)
                continue

            raw_target = RAW / name.replace("/", "_")
            copy_bytes(zf, name, raw_target)
            if "/files/sound/" in name or "/files/music/" in name:
                runtime_target = RUNTIME_AUDIO / Path(name).name
                copy_bytes(zf, name, runtime_target)

    (PROJECT / "Assets" / "Original").mkdir(parents=True, exist_ok=True)
    (PROJECT / "Assets" / "Original" / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"Extracted {len(manifest['files'])} game-facing resources from {APK}")


if __name__ == "__main__":
    main()
