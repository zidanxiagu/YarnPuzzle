from PIL import Image, ImageDraw, ImageFont
from pathlib import Path

root = Path(__file__).resolve().parent.parent
source = root / 'YarnScapes.Unity/Acceptance/2026-11-10/refactor-reuse-v1/runtime/06_finished_art.png'
screen = Image.open(source).convert('RGB')
canvas = Image.new('RGB', (1740, 1130), '#F6F3ED')
draw = ImageDraw.Draw(canvas)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 36)
palettes = [
    ['#E8B5A3', '#D8ADBB', '#E9D8A8', '#F5EDDF', '#7D8790', '#B6A18E', '#B9CBB2', '#8FA994', '#ABC9D9', '#829FB7', '#A294B5', '#C9BCD8', '#DDD0E2'],
    ['#E6B7A6', '#D8A0A5', '#E8D7B9', '#F8EFE2', '#8D807C', '#B38F84', '#BCC8B5', '#94AA9D', '#B7CED2', '#91AAB5', '#A18DAB', '#C2B4C7', '#D8C9D7'],
    ['#E5C5BB', '#D9B7CC', '#E8E4D6', '#F5F3EE', '#7F899F', '#B6A6AE', '#BAD3C7', '#95B7AC', '#A8C7DC', '#929FC2', '#A395BC', '#BEB4D8', '#D4CCE3'],
]
for i in range(3):
    left = 30 + i * 580
    draw.text((left + 256, 12), chr(65 + i), fill='#45504E', font=font)
    canvas.paste(screen, (left, 70))
    for j, color in enumerate(palettes[i]):
        x = left + 5 + j * 41
        draw.rounded_rectangle((x, 1050, x + 32, 1082), radius=7, fill=color)
output = root / 'aigc/yarn-color-reference-layout.png'
canvas.save(output)
print(output)
