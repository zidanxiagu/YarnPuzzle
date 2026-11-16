"""Compare unchanged-camera V3/V4 Unity captures, preserving rendered RGB."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import argparse
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Acceptance/2026-11-10/yarn-edge-v4'
OLD=ROOT/'Acceptance/2026-11-10/yarn-material-v3/runtime'
p=argparse.ArgumentParser();p.add_argument('--run',default='runtime');args=p.parse_args()
RUN=OUT/args.run
def compose(path):
    a=np.asarray(Image.open(path).convert('RGBA'),dtype=np.float32)
    alpha=a[:,:,3:4]/255
    rgb=np.clip(a[:,:,:3]+np.array([234,231,224])*(1-alpha),0,255).astype('uint8')
    return Image.fromarray(rgb)
def center_crop(im,size=800):
    w,h=im.size;return im.crop((w//2-size//2,h//2-size//2,w//2+size//2,h//2+size//2))
font='C:/Windows/Fonts/msyh.ttc'
title=ImageFont.truetype(font,27);label=ImageFont.truetype(font,20);small=ImageFont.truetype(font,17)
for name,filename in [('edge','10_fiber_edge.png'),('stitch','08_geometry_oblique.png')]:
    canvas=Image.new('RGB',(1064,650),'#f3f0eb');draw=ImageDraw.Draw(canvas)
    draw.text((24,14),'轮廓修正 · 同一镜头实机对比',font=title,fill='#28323c')
    draw.text((24,61),'修改前 V3',font=label,fill='#485461')
    draw.text((540,61),'本次 '+args.run,font=label,fill='#485461')
    for x,folder in [(24,OLD),(540,RUN)]:
        im=compose(folder/filename)
        detail=center_crop(im)
        canvas.paste(detail.resize((500,500),Image.Resampling.LANCZOS),(x,102))
        if folder==RUN: detail.save(OUT/(args.run+'-'+name+'-detail.png'))
    draw.text((24,618),'Unity 相机直接渲染，仅裁切、排版和合成透明背景',font=small,fill='#65717c')
    canvas.save(OUT/(args.run+'-'+name+'-comparison.png'))
if (RUN/'11_outer_edge.png').exists():
    im=compose(RUN/'11_outer_edge.png')
    center_crop(im).save(OUT/(args.run+'-outer-detail.png'))
print(OUT/(args.run+'-edge-comparison.png'))
