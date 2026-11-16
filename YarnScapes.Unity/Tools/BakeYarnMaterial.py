"""Bake yarn material V1 from the generated neutral swatch; never modify level data."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageFilter, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/YarnMaterialsV1'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'aigc/yarn-knit-reference-v1.png'
LEVEL = ROOT / 'Assets/Resources/Levels/level_326863.json'


def periodic(gray):
    """Periodic-plus-smooth decomposition, so lighting does not create tile seams."""
    h, w = gray.shape
    boundary = np.zeros_like(gray)
    boundary[0, :] = gray[-1, :] - gray[0, :]
    boundary[-1, :] = -boundary[0, :]
    boundary[:, 0] += gray[:, -1] - gray[:, 0]
    boundary[:, -1] -= gray[:, -1] - gray[:, 0]
    yy, xx = np.mgrid[:h, :w]
    denom = 2 * np.cos(2 * np.pi * xx / w) + 2 * np.cos(2 * np.pi * yy / h) - 4
    denom[0, 0] = 1
    smooth = np.fft.fft2(boundary) / denom
    smooth[0, 0] = 0
    return gray - np.fft.ifft2(smooth).real


def save(array, name):
    Image.fromarray(np.clip(array, 0, 255).astype(np.uint8)).save(OUT / name, optimize=True)


def dense_stitch(fiber):
    """A periodic, plump V loop with a matte three-ply height field."""
    resolution = 192
    y, x = np.mgrid[:resolution, :resolution].astype(float) / resolution
    minimum = np.full(x.shape, 100.)
    phase, across = np.zeros_like(x), np.zeros_like(x)
    t = np.linspace(0, 1, 160)
    u = 1 - t
    for mirrored in (False, True):
        px = u**3 * .15 + 3*u*u*t*.02 + 3*u*t*t*.28 + t**3*.52
        py = u**3 * -.15 + 3*u*u*t*.15 + 3*u*t*t*.68 + t**3*.94
        if mirrored:
            px = 1 - px
        tx, ty = np.gradient(px), np.gradient(py)
        for j in range(len(t)):
            dx, dy = (x-px[j]+.5) % 1-.5, (y-py[j]+.5) % 1-.5
            distance = dx*dx + dy*dy
            nearer = distance < minimum
            minimum = np.minimum(minimum, distance)
            phase = np.where(nearer, t[j], phase)
            signed = (dx*ty[j] - dy*tx[j]) / (np.hypot(tx[j], ty[j]) + 1e-6)
            across = np.where(nearer, signed, across)
    bump = np.sqrt(np.maximum(0, 1 - minimum / .19**2))
    height = .11 + .23*bump
    height += .009*np.cos(2*np.pi*(phase*5 + across/.19*.8))*bump
    hy, hx = np.gradient(height, 1/resolution)
    diffuse = np.maximum(0, (.35*hx + .45*hy + .82) / np.sqrt(hx*hx + hy*hy + 1))
    shade = (.80 + .26*diffuse) * (.88 + .12*bump)
    gray = Image.fromarray(np.clip(shade * 220, 0, 255).astype(np.uint8)).resize((32, 32), Image.Resampling.LANCZOS)
    loops = np.tile(np.asarray(gray, dtype=float) / 220, (8, 8))
    # Keep the generated cotton fibers as the fine roughness layer, while the
    # controlled loop field closes its large holes and defines repeatable Vs.
    return loops * (.86 + .14*fiber / .9)


def main():
    level = json.loads(LEVEL.read_text(encoding='utf-8-sig'))
    source = Image.open(SOURCE).convert('L')
    # The generated swatch has measured repeat periods of about 46 x 33 px.
    # Extract eight columns/rows, remove low-frequency illumination, then make periodic.
    crop = source.crop((366, 264, 734, 528)).resize((256, 256), Image.Resampling.LANCZOS)
    gray = np.asarray(crop, dtype=float)
    low = np.asarray(crop.filter(ImageFilter.GaussianBlur(20)), dtype=float)
    gray = periodic(gray / np.maximum(low, 1))
    p02, p98 = np.percentile(gray, (2, 98))
    shade = np.clip(.66 + (gray - p02) / (p98 - p02) * .45, .57, 1.15)
    shade = dense_stitch(shade)
    neutral = np.clip(shade * 227, 0, 255)
    save(np.repeat(neutral[..., None], 3, axis=2), 'knit_tile.png')
    # Two knit columns and rows per logical cell; preserve exact logical color IDs.
    cell_px = 64
    height, width = level['height'], level['width']
    rows, cols = height * cell_px, width * cell_px
    ys, xs = np.mgrid[:rows, :cols]
    texture = shade[ys % 256, xs % 256]
    rng = np.random.default_rng(306863)
    texture = texture + rng.normal(0, .006, texture.shape)
    indices = np.asarray(level['cells']).reshape(height, width)
    mask = np.repeat(np.repeat(indices >= 0, cell_px, 0), cell_px, 1)
    rgba = np.zeros((rows, cols, 4), dtype=np.uint8)
    palette = np.array([[p[k] for k in ('r', 'g', 'b')] for p in level['palette']]) * 255
    # A matte fiber light response keeps black yarn readable and white yarn below clipping.
    display_palette = 17 + palette * .89
    tint = display_palette[np.maximum(indices, 0)]
    tint = np.repeat(np.repeat(tint, cell_px, 0), cell_px, 1)
    rgb = tint * texture[..., None] + np.maximum(texture[..., None] - .83, 0) * 11
    rgba[..., :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    alpha = Image.fromarray((mask * 255).astype(np.uint8))
    alpha = alpha.filter(ImageFilter.GaussianBlur(.65))
    rgba[..., 3] = np.minimum(np.array(alpha), mask * 255).astype(np.uint8)
    save(rgba, 'level_326863_filled.png')
    unfilled = rgba.copy()
    blank = 226 * (.90 + (texture - .86) * .32)
    unfilled[..., :3] = np.clip(blank[..., None], 0, 255).astype(np.uint8)
    Image.fromarray(unfilled).resize((1920, 1920), Image.Resampling.LANCZOS).save(OUT / 'level_326863_unfilled.png', optimize=True)
    silhouette = Image.fromarray((mask * 255).astype(np.uint8)).resize((960, 960), Image.Resampling.LANCZOS)
    shadow = Image.new('RGBA', (960, 960), (39, 29, 40, 0))
    shadow.putalpha(silhouette.filter(ImageFilter.GaussianBlur(2)).point(lambda x: int(x * .16)))
    shadow.save(OUT / 'level_326863_shadow.png', optimize=True)
    # A horizontal cylindrical yarn strand, seamless in U, with three-ply ridges.
    sy, sx = np.mgrid[0:128, 0:512]
    across = (sy / 127 - .5) * 2
    cylinder = np.sqrt(np.maximum(0, 1 - across ** 2))
    phase = sx / 32 + across * 1.2
    plies = .90 + .10 * np.cos(phase * np.pi * 2)
    fiber = .022 * np.sin(sx * 2.8 + sy * .9) + .012 * np.sin(sy * 5.7 - sx * .8)
    light = .48 + cylinder * .48 - across * .10
    strip = np.zeros((128, 512, 4), dtype=np.uint8)
    strip[..., :3] = np.clip((light * plies + fiber)[..., None] * 255, 0, 255).astype(np.uint8)
    strip[..., 3] = np.clip((1 - np.abs(across)) * 9, 0, 1) * 255
    save(strip, 'thread_strip.png')
    # Wrapped spool: horizontal turns, rounded cylinder and fine twisted fiber ridges.
    yy, xx = np.mgrid[:384, :256]
    u, v = xx / 255, yy / 383
    roundness = np.sqrt(np.maximum(0, 1 - ((u - .5) * 2) ** 2))
    turns = np.mod(v * 11 + .04 * np.sin(u * 5), 1)
    turn_round = np.maximum(0, np.sin(turns * np.pi)) ** .3
    # Coarser diagonal three-ply ridges survive mipmapping at the palette's size.
    twist = .055 * np.cos(2 * np.pi * (u * 14 + turns * 1.7))
    grain = shade[(yy * 2) % 256, (xx * 3) % 256] - .9
    fine = twist + .10 * grain + rng.normal(0, .012, xx.shape)
    spool_shade = (.73 + .27 * roundness) * (.70 + .30 * turn_round) + fine
    spool = np.zeros((384, 256, 4), dtype=np.uint8)
    spool[..., :3] = np.clip(spool_shade[..., None] * 255, 0, 255).astype(np.uint8)
    edge = .045 + .018 * np.cos(v * 22 * np.pi)
    spool[..., 3] = np.clip(np.minimum(u - edge, 1 - edge - u) * 100, 0, 1) * 255
    save(spool, 'spool_wrap.png')
    # A review sheet of the actual baked material, not an independent AI concept image.
    sheet = Image.new('RGB', (1440, 860), '#f4f0ea')
    draw = ImageDraw.Draw(sheet)
    draw.text((48, 24), 'YARN MATERIAL / V1 - DENSE SOFT KNIT', fill='#30343a', stroke_width=0)
    colors = ['#ef8a24', '#e54439', '#2e8fc9', '#f0c658', '#eadcc8', '#2b2d34']
    neutral_im = Image.fromarray(np.clip(neutral, 0, 255).astype(np.uint8)).resize((224, 224), Image.Resampling.LANCZOS)
    neutral_a = np.array(neutral_im, dtype=float) / 227
    for i, color in enumerate(colors):
        from PIL import ImageColor
        c = np.array(ImageColor.getrgb(color), dtype=float)
        swatch = Image.fromarray(np.clip(neutral_a[..., None] * c + np.maximum(neutral_a[..., None] - .83, 0) * 11, 0, 255).astype(np.uint8))
        sheet.paste(swatch, (36 + i * 230, 68))
    art = Image.fromarray(rgba)
    art.thumbnail((520, 520), Image.Resampling.LANCZOS)
    sheet.paste(art, (42, 326), art)
    close = Image.fromarray(rgba).crop((18 * 64, 19 * 64, 33 * 64, 31 * 64)).resize((800, 640), Image.Resampling.LANCZOS)
    close.thumbnail((800, 510), Image.Resampling.LANCZOS)
    sheet.paste(close, (590, 326), close)
    (ROOT / 'aigc').mkdir(exist_ok=True)
    sheet.save(ROOT / 'aigc/yarn-material-v1-design.png')
    manifest = dict(version='yarn-material-v1', logicalCellPixels=64, stitchesPerCell=2,
                    sourceSwatchSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                    levelJsonSha256=hashlib.sha256(LEVEL.read_bytes()).hexdigest(),
                    levelDataModified=False, treatment='periodic V-loop height field / generated cotton fiber roughness / matte tint and fiber lift')
    (OUT / 'material_manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print(json.dumps(manifest))

if __name__ == '__main__':
    main()
