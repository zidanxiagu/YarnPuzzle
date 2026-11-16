"""Review V8 using raw player captures; never replace or retouch the render."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Acceptance/2026-11-10/yarn-volume-v8'
OLD = ROOT / 'Acceptance/2026-11-10/yarn-soft-v7/runtime'
parser = argparse.ArgumentParser()
parser.add_argument('--run', default='runtime-final')
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
    draw.text((24, 14), '毛线 V8 · 同镜头、同倍率实机对照', font=title, fill='#28323c')
    draw.text((24, 62), 'V7 · 修改前', font=label, fill='#485461')
    draw.text((540, 62), 'V8 · 接触阴影与短绒', font=label, fill='#485461')
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
draw.text((24, 14), '毛线 V8 · 游戏实际画面与线圈近景', font=title, fill='#28323c')
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
assert report['stitchesPerCell'] == 9
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

if (OUT/'swatch-final/stage0_normal.png').exists():
    swatch=OUT/'swatch-final'
    canvas=Image.new('RGB',(698,788),'#f3f0eb')
    draw=ImageDraw.Draw(canvas)
    draw.text((20,12),'中性灰正视小样 · 按顺序启用效果',font=title,fill='#28323c')
    labels=['0 · 效果关闭基准','1 · 接触遮蔽与软阴影','2 · 加入短绒与毛边','3 · 平衡顶部、侧面与暗部']
    for i in range(4):
        x=20+(i%2)*338;y=68+(i//2)*342
        draw.text((x,y),labels[i],font=small,fill='#485461')
        canvas.paste(material(swatch/f'stage{i}_normal.png'),(x,y+28))
    draw.text((20,762),'312×312 原尺寸截图；约 26 像素/逻辑格，匹配 3.2 倍视角',font=small,fill='#65717c')
    canvas.save(OUT/'neutral-stages.png')

    stage_checks=[]
    for view in ['fit','normal','close']:
        a=[np.asarray(Image.open(swatch/f'stage{i}_{view}.png').convert('RGBA'),dtype=float) for i in range(4)]
        for i in range(1,4):
            delta=np.abs(a[i]-a[i-1]);mask=np.maximum(a[i][:,:,3],a[i-1][:,:,3])>10
            stage_checks.append(dict(view=view,stage=i,meanChannelDifference=float(delta[mask].mean()),
                                     pixelsOver3=int(np.any(delta>3,axis=2).sum())))
    (OUT/'swatch_differences.json').write_text(json.dumps(stage_checks,indent=2),encoding='utf-8')
    height=np.asarray(Image.open(swatch/'geometry_height.png').convert('RGBA'))
    assert np.count_nonzero(height[:,:,1]>128)>100, 'Empty geometry height field'
    print(json.dumps(stage_checks),flush=True)
