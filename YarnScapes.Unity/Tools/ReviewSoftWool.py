"""Arrange actual V2/V3 Unity frames without changing their material appearance."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import argparse

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Acceptance/2026-11-10/yarn-material-v3'
OLD=ROOT/'Acceptance/2026-11-09/yarn-material-v2/soft-runtime'
parser=argparse.ArgumentParser()
parser.add_argument('--run',default='runtime')
args=parser.parse_args()
RUN=OUT/args.run

def macro(folder):
    p=np.asarray(Image.open(folder/'08_geometry_oblique.png').convert('RGBA'),dtype=np.float32)
    # The Unity camera target has premultiplied RGB. Composite it directly.
    alpha=p[:,:,3:4]/255
    rgb=np.clip(p[:,:,:3]+np.array([234,231,224])*(1-alpha),0,255).astype('uint8')
    im=Image.fromarray(rgb)
    if folder==RUN:
        w,h=im.size
        im.crop((w//2-400,h//2-400,w//2+400,h//2+400)).save(OUT/(args.run+'-fiber-detail.png'))
    return im.resize((500,500),Image.Resampling.LANCZOS)

def edge():
    p=np.asarray(Image.open(RUN/'10_fiber_edge.png').convert('RGBA'),dtype=np.float32)
    alpha=p[:,:,3:4]/255
    rgb=np.clip(p[:,:,:3]+np.array([234,231,224])*(1-alpha),0,255).astype('uint8')
    im=Image.fromarray(rgb)
    w,h=im.size
    im.crop((w//2-400,h//2-400,w//2+400,h//2+400)).save(OUT/(args.run+'-edge-detail.png'))

font='C:/Windows/Fonts/msyh.ttc'
title=ImageFont.truetype(font,27)
label=ImageFont.truetype(font,20)
small=ImageFont.truetype(font,16)
canvas=Image.new('RGB',(1064,1216),'#f3f0eb')
draw=ImageDraw.Draw(canvas)
draw.text((24,15),'毛线 V2 → V3 · 同一关卡与镜头',font=title,fill='#28323c')
draw.text((24,60),'上一版',font=label,fill='#485461')
draw.text((540,60),'本次 · 细纤维与柔软毛层',font=label,fill='#485461')
for x,folder in [(24,OLD),(540,RUN)]:
    frame=Image.open(folder/'07_yarn_material_closeup.png').convert('RGB').crop((20,134,520,696))
    canvas.paste(frame,(x,98))
    canvas.paste(macro(folder),(x,686))
draw.text((24,665),'上：3.2× 游戏画面；下：斜视网格近景。均为 Unity 直接渲染。',font=small,fill='#65717c')
canvas.save(OUT/(args.run+'-comparison.png'))
macro(RUN).resize((1000,1000),Image.Resampling.LANCZOS).save(OUT/(args.run+'-macro.png'))
print(OUT/(args.run+'-comparison.png'))
if (RUN/'10_fiber_edge.png').exists(): edge()
