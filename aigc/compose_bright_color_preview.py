from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).resolve().parent
source = Image.open(folder / 'yarn-color-bright-peach-2026-11-11.png').convert('RGB')
canvas = Image.new('RGB', (720, 1110), '#FFF9EF')
draw = ImageDraw.Draw(canvas)
title_font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 27)
caption_font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 18)
draw.text((28, 22), '鲜彩奶油 · 配色预览', font=title_font, fill='#55505F')
canvas.paste(source.crop((26, 180, 694, 927)), (26, 74))
canvas.paste(source.crop((26, 981, 694, 1154)), (26, 836))
colors = ['#ED8FAD', '#F5AE83', '#F5D56E', '#9ED5A8', '#88CFF0', '#B99CE3']
for i, color in enumerate(colors):
    x = 28 + i * 112
    draw.rounded_rectangle((x, 1030, x + 98, 1054), radius=7, fill=color)
draw.text((28, 1072), '桃粉  ·  杏橙  ·  奶油黄  ·  薄荷绿  ·  天蓝  ·  丁香紫', font=caption_font, fill='#706773')
output = folder / 'yarn-color-bright-preview-2026-11-11.png'
canvas.save(output)
print(output)
