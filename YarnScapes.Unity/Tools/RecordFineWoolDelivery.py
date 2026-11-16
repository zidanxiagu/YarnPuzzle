"""Record V5 material delivery using the completed, rendered player run."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Acceptance/2026-11-10/yarn-dense-v5'
RUN=OUT/'runtime'
report=json.loads((RUN/'interaction_report.json').read_text(encoding='utf-8-sig'))
assert report['fullPlaythrough'] and report['failed']==0 and not report['errors']
assert report['filledCells']==report['paintableCells']==1334
assert report['stitchesPerCell']==9 and all(c['pass'] for c in report['checks'])

def pixels(name,crop=None):
    im=Image.open(RUN/name).convert('RGBA')
    if crop:
        im=im.crop(crop)
    return np.asarray(im,dtype=np.int16)

reset=np.abs(pixels('01_fit.png',(20,134,520,696))-pixels('09_reset.png',(20,134,520,696)))
restored=np.abs(pixels('08_geometry_oblique.png')-pixels('12_restored_geometry.png'))
results=dict(passed=len(report['checks']),total=len(report['checks']),
             regions=report['regionsPlayed'],filled=report['filledCells'],
             stitchesPerCell=report['stitchesPerCell'],stitches=report['filledCells']*report['stitchesPerCell'],
             densityRatioToV4=9/4,nominalStrandScaleToV4=2/3,
             vertices=report['plushVertices'],boundaryFibers=report['boundaryFibers'],
             resetMeanChannelDifference=float(reset.mean()),
             restoreMeanChannelDifference=float(restored.mean()),
             restoreMaxChannelDifference=int(restored.max()),
             restoreChangedPixels=int(np.count_nonzero(np.any(restored>0,axis=2))),
             restorePixelsOver10=int(np.count_nonzero(np.any(restored>10,axis=2))),
             platform='Windows / Unity 2022.3.62f2 / D3D11')
print(json.dumps(results,ensure_ascii=False),flush=True)
assert results['resetMeanChannelDifference']<.15
assert results['restoreMeanChannelDifference']<.1
level=ROOT/'Assets/Resources/Levels/level_326863.json'
level_hash=hashlib.sha256(level.read_bytes()).hexdigest()
assert level_hash=='c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
assert 'Windows player result: Succeeded, errors=0' in (OUT/'build-final.log').read_text(encoding='utf-8-sig',errors='replace')
assert 'INTERACTION_ACCEPTANCE PASS' in (OUT/'player-runtime.log').read_text(encoding='utf-8-sig',errors='replace')
results['levelJsonUnchanged']=True
(OUT/'visual_checks.json').write_text(json.dumps(results,indent=2),encoding='utf-8')

body=f'''# 细密毛线 V5

用户反馈：边缘比之前自然，但针脚偏粗、排列过于规整，要求继续向参考图的细密柔软质感靠近。

## 修改

- 每个逻辑格从 2×2 改为 3×3 针脚，针脚密度为上一版的 2.25 倍，线股名义尺寸缩至 2/3。关卡格数、调色板和填色规则保持原样。
- 调整线圈的轮廓与交叠，并保留弯回底层、逐渐收拢的线头。
- 在连续织面起伏上叠加针脚尺度的松紧、侧弯、倾角、左右不对称和厚度变化。所有变化固定在棋盘坐标上，不会随镜头移动或重新读取进度而重新随机。
- 绒毛层厚度、游离毛丝长度和宽度随针脚同步缩细；表面加入同色细纤维层次，并使用较宽的漫反射受光。没有镜面高光。
- 保留 V4 按相邻格收边、搭接和减弱内部毛丝的处理。
- 每网格块由 24 格减为 16 格，随更高针脚密度控制单个网格的顶点上限。

## 实机证据

- [V4 / V5 同镜头、同倍率针脚对比](runtime-stitch-comparison.png)
- [颜色交界对比](runtime-edge-comparison.png)
- [游戏画面和针脚近景](runtime-preview.png)
- [外轮廓细节](runtime-outer-detail.png)
- [完整关卡画面](runtime/06_finished_art.png)
- [连线填色过程](runtime/03_thread_in_progress.png)

以上来自 Unity Windows player 直接渲染。对比图只裁切、缩放、排版和合成透明背景，无后期模糊、锐化、调色或生成图替换。斜视相机用于查看几何和纤维细节，并非默认游戏视角。preview1 为外观夹具，不计作玩法覆盖；保留了第一版和最终版的构建日志。

## 验证

- build-final.log：Succeeded，errors=0。
- UGUI 射线与指针事件回放：{len(report['checks'])}/{len(report['checks'])} 项通过，运行异常 0。
- 完成 {report['regionsPlayed']} 个区域、{report['filledCells']}/1334 格；覆盖选色、缩放、拖动、异色拒绝、连线填色、完成和重置。
- 填完后生成 {results['stitches']:,} 个针脚，独立网格顶点数 {report['plushVertices']:,}，额外边界毛丝 {report['boundaryFibers']:,} 根。毛层复用网格，不重复计入此顶点数。
- 重置棋盘与初始棋盘的平均通道差：{results['resetMeanChannelDifference']:.8f}/255。
- 不同顺序恢复全部进度后，与完成关卡的同镜头原始渲染相比：平均通道差 {results['restoreMeanChannelDifference']:.8f}/255，最大差 {results['restoreMaxChannelDifference']}/255，变化像素 {results['restoreChangedPixels']:,}，通道差超过 10 的像素 {results['restorePixelsOver10']:,}。透明纤维的绘制混合顺序可产生少量差异，原图保留供复核。
- 验收模式未写入用户存档。关卡 JSON SHA-256 与修改前一致：`{level_hash}`。

## 验收范围

本轮实机中，针脚比 V4 更细密，单个针脚的外形和松紧变化更明显；仍保留整体编织方向。完整画面和同倍率近景均保留，便于继续对照参考图评价纤维柔软度与重复感。整体美术未宣称达到参考图或原版最终标准。

覆盖 Windows；Android 性能和实体双指操作仍待验证。更细密的网格增加了渲染与内存开销。原版动画、镜头、特效及周边功能不是本轮新增结果。

源文件、材质、最终构建程序集和证据哈希见 manifest.json。
'''
(OUT/'REPORT.md').write_text(body,encoding='utf-8')
paths=[ROOT/name for name in [
    'Assets/Scripts/Presentation/PlushYarnBoard.cs',
    'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs',
    'Tools/ReviewFineWool.py','Tools/RecordFineWoolDelivery.py','README.md',
    'Assets/Resources/Levels/level_326863.json',
    'Assets/Resources/YarnMaterialsV3/wool_filaments.png',
    'Assets/Resources/YarnMaterialsV4/BoundaryWoolFiber.shader',
    'Builds/YarnScapesOffline_Data/Managed/Assembly-CSharp.dll']]
for folder in [ROOT/'Assets/Resources/YarnMaterialsV5',RUN]:
    paths.extend(p for p in folder.iterdir() if p.is_file())
paths.extend(OUT/name for name in ['runtime-stitch-comparison.png','runtime-edge-comparison.png',
    'runtime-preview.png','runtime-outer-detail.png','REPORT.md','visual_checks.json','build-final.log','player-runtime.log'])
manifest=[dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,
               sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths]
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
