"""Validate the completed player run and record the V3 material delivery."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import hashlib
import json
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Acceptance/2026-11-10/yarn-material-v3'
RUN=OUT/'runtime'
report=json.loads((RUN/'interaction_report.json').read_text(encoding='utf-8-sig'))
assert report['fullPlaythrough'] and report['failed']==0 and not report['errors']
assert report['filledCells']==report['paintableCells']==1334
assert any(c['name']=='soft_fiber_coat_active' and c['pass'] for c in report['checks'])
checks=len(report['checks'])
assert all(c['pass'] for c in report['checks'])
level=ROOT/'Assets/Resources/Levels/level_326863.json'
level_hash=hashlib.sha256(level.read_bytes()).hexdigest()
assert level_hash=='c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
a=np.asarray(Image.open(RUN/'01_fit.png').convert('RGB').crop((20,134,520,696)),dtype=float)
b=np.asarray(Image.open(RUN/'09_reset.png').convert('RGB').crop((20,134,520,696)),dtype=float)
reset_error=float(np.abs(a-b).mean())
assert reset_error<.15, f'Reset left differences: {reset_error}'
log=(OUT/'build-final.log').read_text(encoding='utf-8-sig',errors='replace')
assert 'Windows player result: Succeeded, errors=0' in log
player_log=(OUT/'player-runtime.log').read_text(encoding='utf-8-sig',errors='replace')
assert 'INTERACTION_ACCEPTANCE PASS' in player_log

face='C:/Windows/Fonts/msyh.ttc'
title=ImageFont.truetype(face,28);label=ImageFont.truetype(face,20);small=ImageFont.truetype(face,17)
canvas=Image.new('RGB',(1064,710),'#f3f0eb');draw=ImageDraw.Draw(canvas)
draw.text((24,15),'毛线 V3 · 细纤维与柔软毛层',font=title,fill='#28323c')
draw.text((24,64),'3.2× 游戏实际画面',font=label,fill='#485461')
draw.text((540,64),'纤维近景 · 斜视相机',font=label,fill='#485461')
canvas.paste(Image.open(RUN/'07_yarn_material_closeup.png').convert('RGB').crop((20,134,520,696)),(24,103))
canvas.paste(Image.open(OUT/'runtime-fiber-detail.png').resize((500,500),Image.Resampling.LANCZOS),(540,103))
draw.text((540,625),'细短绒毛 / 渐尖毛丝 / 哑光受光',font=small,fill='#65717c')
draw.text((24,680),'Unity 直接渲染画面，仅裁切、排版和合成透明背景',font=small,fill='#65717c')
canvas.save(OUT/'soft-wool-v3-preview.png')
results=dict(passed=checks,total=checks,regions=report['regionsPlayed'],filled=report['filledCells'],
             vertices=report['plushVertices'],resetMeanChannelDifference=reset_error,
             levelJsonUnchanged=True,platform='Windows / Unity 2022.3.62f2 / D3D11')
(OUT/'visual_checks.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
body=f'''# 毛线 V3：细纤维和柔软毛层

本轮用户反馈：V2 有立体感，但偏硬、像塑料，要求纤维和毛边细腻柔软。

## 本次修改

- 保留真实的线圈体积和交叠。线股使用更圆润的截面，端头稍微沉入编织底层。
- 用参考棉线样片的细颗粒生成材料数据，去除长条梳齿纹；微法线打散光滑受光。
- 去掉连续亮边与镜面高光，使用宽而柔和的漫反射、同色纤维散射。
- 两层稀疏短绒毛贴着表面，外层更轻；毛丝比上一版更细、更短，逐渐变尖。
- 每个线股以 20 根短毛与 6 根游离毛丝补充轮廓，颜色接近本色，避免灰白雾边。
- 绒毛层复用身体网格；UI 输入、关卡 JSON 和存档格式保持原样。

## 实际证据

- [游戏画面与纤维近景](soft-wool-v3-preview.png)
- [与 V2 同位置、同倍率对比](runtime-comparison.png)
- [纤维原像素裁切](runtime-fiber-detail.png)
- [红/橙/黑交界的毛边裁切](runtime-edge-detail.png)
- [完整关卡画面](runtime/06_finished_art.png)
- [编织过程](runtime/03_thread_in_progress.png)

以上均来自 Unity Windows player 直接渲染。画面仅裁切、排版与合成透明背景，没有使用生成图替代实机结果，也没有后期模糊、锐化或调色。斜视相机用于展示纤维近景，不是游戏默认镜头。

preview-* 是材料外观的恢复进度夹具，明确不计作玩法验收。最终玩法结果以 runtime/ 为准。

## 运行验证

- build-final.log：Succeeded，errors=0。
- {checks}/{checks} 项检查通过，运行异常 0。
- 通过 UGUI 射线与指针事件完成 {report['regionsPlayed']} 个区域，{report['filledCells']}/1334 格。
- 选色、缩放、拖动、异色拒绝、编织连线、完成和重置通过。
- 细绒毛层已激活；几何格子数与完成进度一致。
- 重置后棋盘与初始棋盘的平均通道差：{reset_error:.6f}/255。
- 验收未改写用户存档；关卡 JSON SHA-256 与改动前一致：`{level_hash}`。

本轮检查覆盖 Windows。Android 性能与实体双指手势未验证；短绒毛透明层增加了渲染工作量。当前完成图的独立网格顶点数为 {report['plushVertices']:,}；重复渲染的毛层不重复计入此数。

## 视觉结论与边界

实机复查中，V3 相比 V2 的连续硬亮边减少，纤维和轮廓更细碎，线圈保持厚度与交叠。当前仍是按参考图方向迭代的设计稿，不声称完全达到参考照片的真实棉线质感或原版整体美术标准。原版动画、镜头和特效的一致性仍是独立事项。

初轮预览、编译错误与中间效果日志保留，便于追溯；最终构建和截图见上方固定路径。源文件、材料数据、构建程序集与最终证据哈希见 manifest.json。
'''
(OUT/'REPORT.md').write_text(body,encoding='utf-8')
paths=[ROOT/'Assets/Scripts/Presentation/PlushYarnBoard.cs',ROOT/'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs',
       ROOT/'Assets/Editor/YarnMaterialImport.cs',ROOT/'Tools/PrepareSoftWoolAssets.py',ROOT/'Tools/ReviewSoftWool.py',
       ROOT/'Tools/RecordSoftWoolDelivery.py',ROOT/'README.md',level,ROOT/'Builds/YarnScapesOffline_Data/Managed/Assembly-CSharp.dll']
paths += [p for p in (ROOT/'Assets/Resources/YarnMaterialsV3').iterdir() if p.is_file()]
paths += [p for p in RUN.iterdir() if p.is_file()]
paths += [OUT/n for n in ['soft-wool-v3-preview.png','runtime-comparison.png','runtime-fiber-detail.png','runtime-edge-detail.png','visual_checks.json','REPORT.md','build-final.log','player-runtime.log']]
manifest=[dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths]
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(results,ensure_ascii=False))
