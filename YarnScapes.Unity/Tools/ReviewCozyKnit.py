"""Review V9 using raw player captures; never replace or retouch the render."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Acceptance/2026-11-10/yarn-cozy-v9'
OLD = ROOT / 'Acceptance/2026-11-10/yarn-volume-v8/runtime-final'
parser = argparse.ArgumentParser()
parser.add_argument('--run', default='runtime')
args = parser.parse_args()
RUN = OUT / args.run


def material(path):
    # The offscreen camera stores premultiplied RGB, including opaque yarn.
    a = np.asarray(Image.open(path).convert('RGBA'), dtype=np.float32)
    return Image.fromarray(np.clip(a[:, :, :3] + np.array([234, 231, 224]) *
                                  (1-a[:, :, 3:4]/255), 0, 255).astype('uint8'))


font = 'C:/Windows/Fonts/msyh.ttc'
title = ImageFont.truetype(font, 26)
label = ImageFont.truetype(font, 19)
small = ImageFont.truetype(font, 16)
for suffix, filename in [('stitch', '08_geometry_oblique.png'),
                         ('edge', '10_fiber_edge.png'),
                         ('outer', '11_outer_edge.png')]:
    canvas = Image.new('RGB', (1064, 650), '#f3f0eb')
    draw = ImageDraw.Draw(canvas)
    draw.text((24, 14), '毛线 V9 · 同镜头、同倍率实机对照', font=title, fill='#28323c')
    draw.text((24, 62), 'V8 · 原有针脚与配色', font=label, fill='#485461')
    draw.text((540, 62), 'V9 · 短针密织与柔和配色', font=label, fill='#485461')
    for x, folder in [(24, OLD), (540, RUN)]:
        im = material(folder / filename)
        w, h = im.size
        crop = im.crop((w//2-600, h//2-600, w//2+600, h//2+600))
        canvas.paste(crop.resize((500, 500), Image.Resampling.LANCZOS), (x, 102))
    draw.text((24, 616), '仅裁切、缩放、排版及合成透明背景；保留原始截图供复核',
              font=small, fill='#65717c')
    canvas.save(OUT / (args.run + '-' + suffix + '-comparison.png'))

canvas = Image.new('RGB', (1064, 714), '#f3f0eb')
draw = ImageDraw.Draw(canvas)
draw.text((24, 14), '毛线 V9 · 游戏实际画面与线圈近景', font=title, fill='#28323c')
draw.text((24, 62), '3.2 倍游戏画面', font=label, fill='#485461')
draw.text((540, 62), '斜视相机检查接触暗部与毛边', font=label, fill='#485461')
game = Image.open(RUN/'07_yarn_material_closeup.png').convert('RGB')
canvas.paste(game.crop((20, 134, 520, 696)), (24, 102))
im = material(RUN/'08_geometry_oblique.png')
w, h = im.size
canvas.paste(im.crop((w//2-600, h//2-600, w//2+600, h//2+600)).resize(
    (500, 500), Image.Resampling.LANCZOS), (540, 102))
draw.text((24, 682), '实机渲染；参考图最终品质、原版动画一致性仍需单独验收',
          font=small, fill='#65717c')
canvas.save(OUT/(args.run+'-preview.png'))

report = json.loads((RUN/'interaction_report.json').read_text(encoding='utf-8-sig'))
assert report['failed'] == 0 and not report['errors'], report
assert all(c['pass'] for c in report['checks'])
assert report['stitchesPerCell'] == 12
for name in ['06_finished_art.png', '07_yarn_material_closeup.png']:
    a = np.asarray(Image.open(RUN/name).convert('RGB'))
    assert int(a.max()) - int(a.min()) > 30, f'Blank screen: {name}'

if report['fullPlaythrough']:
    assert report['filledCells'] == report['paintableCells'] == 1334
    initial = np.asarray(Image.open(RUN/'01_fit.png').convert('RGBA'), dtype=np.int16)[134:696, 20:520]
    reset = np.asarray(Image.open(RUN/'09_reset.png').convert('RGBA'), dtype=np.int16)[134:696, 20:520]
    replay = np.asarray(Image.open(RUN/'08_geometry_oblique.png').convert('RGBA'), dtype=np.int16)
    restore = np.asarray(Image.open(RUN/'12_restored_geometry.png').convert('RGBA'), dtype=np.int16)
    diff = np.abs(replay-restore)
    result = dict(resetMeanDifference=float(np.abs(initial-reset).mean()),
                  restoreMeanDifference=float(diff.mean()), restoreMaxDifference=int(diff.max()),
                  restorePixelsOver10=int(np.any(diff > 10, axis=2).sum()))
    assert result['resetMeanDifference'] < .15, result
    assert result['restoreMeanDifference'] < .1, result
    old_report = json.loads((OLD/'interaction_report.json').read_text(encoding='utf-8-sig'))
    result.update(checks=len(report['checks']), filled=report['filledCells'],
                  oldVertices=old_report['plushVertices'], vertices=report['plushVertices'],
                  vertexReductionPercent=100*(1-report['plushVertices']/old_report['plushVertices']),
                  boundaryFibers=report['boundaryFibers'],
                  geometryGenerationMilliseconds=report['geometryGenerationMilliseconds'],
                  meshUploadMilliseconds=report['meshUploadMilliseconds'],
                  managedHeapMiBAfterCollection=report['managedHeapBytes']/2**20,
                  unityAllocatedMiB=report['unityAllocatedBytes']/2**20)
    level = ROOT/'Assets/Resources/Levels/level_326863.json'
    result['levelSha256'] = hashlib.sha256(level.read_bytes()).hexdigest()
    assert result['levelSha256'] == 'c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
    (OUT/'visual_checks.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result), flush=True)
else:
    print('Rendering fixture only; no gameplay acceptance claimed.', flush=True)

# Equal gray, front view, identical camera scale: isolate tighter geometry
# from the simultaneously requested softer color palette.
canvas=Image.new('RGB',(1012,756),'#f3f0eb')
draw=ImageDraw.Draw(canvas)
draw.text((20,14),'中性灰同倍率 · 单独比较针脚与开口',font=title,fill='#28323c')
for x,folder,caption in [(20,OLD.parent/'swatch-final','V8 · 每格 3 列 × 3 行'),
                         (516,OUT/'swatch','V9 · 每格 3 列 × 4 行')]:
    draw.text((x,62),caption,font=label,fill='#485461')
    im=material(folder/'stage3_close.png')
    canvas.paste(im.resize((476,476),Image.Resampling.LANCZOS),(x,98))
    canvas.paste(material(folder/'stage3_normal.png').crop((52,90,260,250)),(x+134,584))
canvas.save(OUT/'neutral-density-comparison.png')

canvas=Image.new('RGB',(1064,706),'#f3f0eb')
draw=ImageDraw.Draw(canvas)
draw.text((24,14),'毛线 V9 · 常用倍率与针脚近景',font=title,fill='#28323c')
draw.text((24,60),'3.2 倍游戏画面',font=label,fill='#485461')
draw.text((540,60),'同一织面 · 斜视近景',font=label,fill='#485461')
canvas.paste(game.crop((20,134,520,634)),(24,98))
detail=material(RUN/'08_geometry_oblique.png')
cx,cy=detail.width//2,detail.height//2
canvas.paste(detail.crop((cx-600,cy-600,cx+600,cy+600)).resize((500,500),Image.Resampling.LANCZOS),(540,98))
names=['暖杏橙','浅玫瑰粉','奶油黄','暖米白','蓝灰','可可棕','鼠尾草绿','灰苔绿','雾蓝','牛仔蓝','烟紫','丁香紫','浅薰衣草']
colors=report.get('displayPalette',[])
for j,color in enumerate(colors):
    x=24+j*79
    draw.rounded_rectangle((x,620,x+65,644),radius=5,fill=color)
    draw.text((x,652),names[j],font=ImageFont.truetype(font,12),fill='#485461')
draw.text((24,682),'直接实机渲染，仅裁切和排版；色样为光照前的显示颜色',font=small,fill='#65717c')
canvas.save(OUT/(args.run+'-preview.png'))

coverage={}
for version,folder in [('v8',OLD.parent/'swatch-final'),('v9',OUT/'swatch')]:
    height=np.asarray(Image.open(folder/'geometry_height.png').convert('RGBA'))
    h,w=height.shape[:2]
    region=height[round(h*4/12):round(h*8/12),round(w*3/12):round(w*7/12),1]
    coverage[version]=dict(opaqueLoopCoverage=float(np.mean(region>128)),
                           exposedBedFraction=float(np.mean(region<=128)))
coverage['meaning']='Same 4x4-cell interior crop of neutral swatches; measures visible opaque loop coverage, not fur quality.'
(OUT/'geometry_coverage.json').write_text(json.dumps(coverage,indent=2),encoding='utf-8')
print(json.dumps(coverage),flush=True)

