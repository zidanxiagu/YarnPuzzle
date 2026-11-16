"""Record runtime evidence and image regressions for the V4 edge revision."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Acceptance/2026-11-10/yarn-edge-v4'
RUN = OUT / 'runtime'
report = json.loads((RUN / 'interaction_report.json').read_text(encoding='utf-8-sig'))
assert report['fullPlaythrough'] and report['failed'] == 0 and not report['errors']
assert report['filledCells'] == report['paintableCells'] == 1334
assert all(c['pass'] for c in report['checks'])
assert any(c['name'] == 'neighbor_aware_recessed_bed_active' for c in report['checks'])

def pixels(name, crop=None):
    im = Image.open(RUN / name).convert('RGBA')
    if crop:
        im = im.crop(crop)
    return np.asarray(im, dtype=np.int16)

reset_diff = np.abs(pixels('01_fit.png', (20,134,520,696)) - pixels('09_reset.png', (20,134,520,696)))
restore_diff = np.abs(pixels('08_geometry_oblique.png') - pixels('12_restored_geometry.png'))
reset_error = float(reset_diff.mean())
restore_error = float(restore_diff.mean())
results = dict(passed=len(report['checks']), total=len(report['checks']),
               regions=report['regionsPlayed'], filled=report['filledCells'],
               vertices=report['plushVertices'], boundaryFibers=report['boundaryFibers'],
               resetMeanChannelDifference=reset_error,
               restoreMeanChannelDifference=restore_error,
               restoreMaxChannelDifference=int(restore_diff.max()),
               restoreChangedPixels=int(np.count_nonzero(np.any(restore_diff > 0, axis=2))),
               restorePixelsOver10=int(np.count_nonzero(np.any(restore_diff > 10, axis=2))),
               platform='Windows / Unity 2022.3.62f2 / D3D11')
print(json.dumps(results, ensure_ascii=False), flush=True)
assert reset_error < .15, f'Reset regression: {reset_error}'
assert restore_error < .1, f'Restored rendering differs: {restore_error}'

level = ROOT / 'Assets/Resources/Levels/level_326863.json'
level_hash = hashlib.sha256(level.read_bytes()).hexdigest()
assert level_hash == 'c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
assert 'Windows player result: Succeeded, errors=0' in (OUT / 'build-step4.log').read_text(encoding='utf-8-sig', errors='replace')
assert 'INTERACTION_ACCEPTANCE PASS' in (OUT / 'player-runtime.log').read_text(encoding='utf-8-sig', errors='replace')
results['levelJsonUnchanged'] = True
(OUT / 'visual_checks.json').write_text(json.dumps(results, indent=2), encoding='utf-8')

body = f'''# 毛线边缘 V4

按讨论顺序落实了四项修改：

1. 方形底板改为下沉的编织支撑层。根据邻格的颜色与完成状态收边或搭接，异色交界使用共享曲线，减少白缝和直角切边。
2. 线股两端弯回底层，并逐渐收拢封口；保留针脚前后交叠。
3. 在外轮廓和异色交界补充细短、弯曲、渐尖的毛丝。毛丝更多沿编织方向散开，相邻格填满后自动减弱，避免内部出现一圈白毛。
4. 对位置、倾角、粗细和高度施加小幅、空间连续的变化。变化由棋盘坐标确定，不随画面移动或重新读取而重新随机。

## 实机对比

- [V3 / V4 同位置边缘对比](runtime-edge-comparison.png)
- [针脚与收口对比](runtime-stitch-comparison.png)
- [图案外轮廓近景](runtime-outer-detail.png)
- [完整游戏画面](runtime/06_finished_art.png)
- [3.2 倍游戏画面](runtime/07_yarn_material_closeup.png)
- [编织过程](runtime/03_thread_in_progress.png)

所有最终截图来自 Unity Windows player。对比图仅裁切、缩放、排版和合成透明背景，无后期模糊、锐化、调色或生成图替换。斜视相机专门展示几何轮廓，不是默认游戏镜头。

step1 至 step4 是局部渲染夹具，明确不计作玩法验收。最终整关验收以 runtime/ 为准。中间编译错误、过长毛丝的预览和修正日志均保留。

## 验证结果

- 最终构建：build-step4.log，Succeeded，errors=0。
- Windows 实机 UGUI 射线和指针事件回放：{len(report['checks'])}/{len(report['checks'])} 项通过，运行异常 0。
- 通过选色和棋盘点击完成 {report['regionsPlayed']} 个区域，{report['filledCells']}/1334 格。
- 覆盖选色、缩放、拖动、异色拒绝、连线填色、完成和重置；本轮验收未写入用户存档。
- 重置棋盘与初始棋盘的平均通道差：{reset_error:.8f}/255。
- 按不同顺序恢复全部进度后，与完成关卡的同镜头原始渲染相比：平均通道差 {restore_error:.8f}/255，最大差 {results['restoreMaxChannelDifference']}/255，变化像素 {results['restoreChangedPixels']:,}，通道差超过 10 的像素 {results['restorePixelsOver10']:,}。透明纤维的混合顺序可造成微小像素差，原始画面均保留用于核验。
- 关卡 JSON SHA-256 与修改前一致：`{level_hash}`。

当前完成图独立网格顶点数 {report['plushVertices']:,}，其中额外边界毛丝 {report['boundaryFibers']:,} 根；重复渲染的毛层不重复计入顶点数。Android 性能和实体双指操作未验证。

## 视觉验收范围

本轮修正了方形底板和端口造成的整齐切边，外轮廓随针脚起伏，细毛丝补充柔软感。仍需在参考图和实机之间继续评估线股密实度、纤维层次与整体质感，不宣称已完全达到参考图或原版整体美术标准。原版动画、镜头和特效的一致性不属于本轮新增结果。

源文件、构建程序集和证据哈希见 manifest.json。
'''
(OUT / 'REPORT.md').write_text(body, encoding='utf-8')
paths = [ROOT / name for name in [
    'Assets/Scripts/Presentation/PlushYarnBoard.cs',
    'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs',
    'Tools/ReviewYarnEdges.py', 'Tools/RecordYarnEdgeDelivery.py', 'README.md',
    'Assets/Resources/Levels/level_326863.json',
    'Builds/YarnScapesOffline_Data/Managed/Assembly-CSharp.dll']]
for folder in [ROOT / 'Assets/Resources/YarnMaterialsV4', RUN]:
    paths.extend(p for p in folder.iterdir() if p.is_file())
paths.extend(OUT / name for name in ['runtime-edge-comparison.png', 'runtime-stitch-comparison.png',
             'runtime-outer-detail.png', 'visual_checks.json', 'REPORT.md', 'build-step4.log', 'player-runtime.log'])
manifest = [dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size,
                 sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths]
(OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
