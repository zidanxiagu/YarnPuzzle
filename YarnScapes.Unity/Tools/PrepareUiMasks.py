from pathlib import Path
from PIL import Image
root = Path(__file__).resolve().parents[1] / 'Assets' / 'Resources' / 'Textures'
for name in ('pixel_sparkle', 'pixel_style_mask'):
    image = Image.open(root / (name + '.png')).convert('RGBA')
    print(name, image.size, image.getextrema())
    # Preserve the recovered silhouette/alpha, allow UI tinting of its black RGB.
    tinted = Image.new('RGBA', image.size, (255, 255, 255, 0))
    tinted.putalpha(image.getchannel('A'))
    tinted.save(root / (name + '_tintable.png'))
