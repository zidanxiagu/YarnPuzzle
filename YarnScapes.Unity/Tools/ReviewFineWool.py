"""Compare V4/V5 same-camera Unity captures without changing the rendered look."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Acceptance/2026-11-10/yarn-dense-v5'
OLD = ROOT / 'Acceptance/2026-11-10/yarn-edge-v4/runtime'
parser = argparse.ArgumentParser()
parser.add_argument('--run', default='runtime')
args = parser.parse_args()
RUN = OUT / args.run

def compose(path):
    a = np.asarray(Image.open(path).convert('RGBA'), dtype=np.float32)
    rgb = np.clip(a[:,:,:3] + np.array([234,231,224]) * (1-a[:,:,3:4]/255),0,255)
    return Image.fromarray(rgb.astype('uint8'))

def crop_center(im, size=1200):
    w,h=im.size
    return im.crop((w//2-size//2,h//2-size//2,w//2+size//2,h//2+size//2))

face='C:/Windows/Fonts/msyh.ttc'
title=ImageFont.truetype(face,27)
label=ImageFont.truetype(face,20)
small=ImageFont.truetype(face,17)
for name,filename in [('stitch','08_geometry_oblique.png'),('edge','10_fiber_edge.png')]:
    canvas=Image.new('RGB',(1064,650),'#f3f0eb')
    draw=ImageDraw.Draw(canvas)
    draw.text((24,14),'细密毛线 · 同镜头、同倍率实机对比',font=title,fill='#28323c')
    draw.text((24,61),'修改前 V4 · 每格 2×2 针脚',font=label,fill='#485461')
    draw.text((540,61),'本次 V5 · 每格 3×3 针脚',font=label,fill='#485461')
    for x,folder in [(24,OLD),(540,RUN)]:
        detail=crop_center(compose(folder/filename))
        canvas.paste(detail.resize((500,500),Image.Resampling.LANCZOS),(x,102))
        if folder==RUN:
            detail.save(OUT/(args.run+'-'+name+'-detail.png'))
    draw.text((24,618),'Unity 相机直接渲染，仅裁切、排版和合成透明背景',font=small,fill='#65717c')
    canvas.save(OUT/(args.run+'-'+name+'-comparison.png'))
crop_center(compose(RUN/'11_outer_edge.png')).save(OUT/(args.run+'-outer-detail.png'))
canvas=Image.new('RGB',(1064,710),'#f3f0eb')
draw=ImageDraw.Draw(canvas)
draw.text((24,15),'细密毛线 V5 · 游戏实际画面与针脚近景',font=title,fill='#28323c')
draw.text((24,64),'3.2 倍游戏画面',font=label,fill='#485461')
draw.text((540,64),'针脚细节 · 斜视相机',font=label,fill='#485461')
canvas.paste(Image.open(RUN/'07_yarn_material_closeup.png').convert('RGB').crop((20,134,520,696)),(24,103))
canvas.paste(Image.open(OUT/(args.run+'-stitch-detail.png')).resize((500,500),Image.Resampling.LANCZOS),(540,103))
draw.text((24,680),'实际渲染画面；整体美术与参考图的一致性仍需继续验收',font=small,fill='#65717c')
canvas.save(OUT/(args.run+'-preview.png'))
print(OUT/(args.run+'-stitch-comparison.png'))
