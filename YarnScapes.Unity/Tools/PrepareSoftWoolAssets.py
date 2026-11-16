"""Deterministic, tileable filament and short-pile maps for the V3 wool shader.

These are material data, not replacement screenshots or generated concept art.
R = fine yarn grain, G = tiny curved fiber coverage, B = broad grain,
A = pile length. U follows a strand, V wraps around its circumference.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Packages/com.weavekit.runtime/Runtime/Resources/WeaveKit/Wool'
OUT.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(87131)
size = 1024
source=Image.open(ROOT/'aigc/yarn-knit-reference-v1.png').convert('L').resize((size,size))
a=np.asarray(source,dtype=np.float32)
# Preserve irregular cotton grain, remove the source swatch's knit structure.
fine=a-np.asarray(source.filter(ImageFilter.GaussianBlur(1.4)),dtype=np.float32)
middle=a-np.asarray(source.filter(ImageFilter.GaussianBlur(4)),dtype=np.float32)
noise=Image.fromarray(np.clip(128+rng.normal(0,27,a.shape),0,255).astype('uint8'))
tufts=np.asarray(noise.filter(ImageFilter.GaussianBlur(.75)),dtype=np.float32)-128
red=np.clip(128+fine*1.65+tufts*1.1,0,255).astype('uint8')
base=Image.fromarray(red)
blue=Image.fromarray(np.clip(128+middle*.8,0,255).astype('uint8'))

# Supersampling retains a soft subpixel edge on the separate pile strands.
scale = 2
coverage = Image.new('L', (size * scale, size * scale))
length = Image.new('L', coverage.size)
draw = ImageDraw.Draw(coverage)
height = ImageDraw.Draw(length)
for _ in range(18000):
    x, y = rng.uniform(0, size, 2)
    extent = rng.uniform(5, 21)
    bend = rng.uniform(-5, 5)
    slope = rng.uniform(-.50,.85)
    points = [(x + t * extent, y + t * extent * slope + np.sin(t * np.pi) * bend) for t in np.linspace(0, 1, 7)]
    opacity = int(rng.uniform(140, 255))
    pile = int(rng.uniform(95, 255))
    width = int(rng.choice([1, 1, 2]))
    for dx in (-size, 0, size):
        for dy in (-size, 0, size):
            if x + dx < -30 or x + dx > size or y + dy < -30 or y + dy > size + 10:
                continue
            line = [((px + dx) * scale, (py + dy) * scale) for px, py in points]
            draw.line(line, fill=opacity, width=width)
            height.line(line, fill=pile, width=width + 1)
coverage = coverage.resize((size, size), Image.Resampling.LANCZOS)
length = length.resize((size, size), Image.Resampling.LANCZOS)
Image.merge('RGBA', (base, coverage, blue, length)).save(OUT / 'wool_filaments.png')
print('V3 wool data prepared: 1024x1024 RGBA, deterministic fine filaments and pile.')
