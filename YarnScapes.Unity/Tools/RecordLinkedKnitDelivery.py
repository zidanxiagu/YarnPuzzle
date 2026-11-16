"""Record only checks and artifacts actually produced by the final V6 player."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Acceptance/2026-11-10/yarn-knit-v6'
RUN = OUT/'runtime-rounded'
report = json.loads((RUN/'interaction_report.json').read_text(encoding='utf-8-sig'))
checks = json.loads((OUT/'visual_checks.json').read_text(encoding='utf-8-sig'))
landscape = json.loads((OUT/'landscape-rounded/interaction_report.json').read_text(encoding='utf-8-sig'))
assert report['fullPlaythrough'] and report['failed'] == 0 and not report['errors']
assert report['filledCells'] == report['paintableCells'] == 1334
assert all(c['pass'] for c in report['checks'])
assert landscape['failed'] == 0 and not landscape['fullPlaythrough']
build = (OUT/'build-rounded-crown.log').read_text(encoding='utf-8-sig', errors='replace')
assert 'Windows player result: Succeeded, errors=0' in build
assert 'INTERACTION_ACCEPTANCE PASS' in (OUT/'player-runtime-rounded.log').read_text(
    encoding='utf-8-sig', errors='replace')

body = f'''# 毛线 V6：连续线圈与几何减负

日期：2026-11-10。Windows 构建及内部交互检查通过；参考图最终美术品质、原版动画一致性仍未验收通过。

## 续接要求与核验

用户最后要求：针脚更细密柔软、排列更自然，继续向提供的图片靠近。
本轮读取旧会话，检查 V5 源码、原始运行截图，并重新运行旧版的外观夹具。V5 确认为每格 3×3 针脚、整关 7,663,316 个独立顶点。
旧版夹具使用隐藏窗口时游戏截图为空黑屏，几何相机有输出。因此它只用于核验加载、几何计数和同镜头几何基线，不作为游戏画面或完整玩法证据。V5 的对照游戏图仍以旧目录下实际截图为准。

## 本轮修改

- 保留每格 3×3 的密度。线股名义半径由 .097×2/3 改为 .085×2/3，缩小约 12.4%，同时减小中段鼓包，避免每条线腿都像一片独立鳞片。
- 上端增加回环，延伸到上一行针脚下方，形成线脚压住回环的交叠。外轮廓自验发现旧线头的渐尖逻辑会把回环顶部变成小尖角，最终改为等粗的水平切线圆弧，仅在埋入底层的线脚收尖。
- 稍增固定的位置漂移、松紧、伸展变化，保留整体编织方向。变化仍取决于关卡坐标，不随镜头移动重新随机。
- 表面毛层随线股缩薄。方向性纤维的条纹在近景自验时过强，最终将其对比幅度从 .055 下调至 .010。
- 线股保留朝向相机的 240 度截面，省去埋在底层下方的背面；每条腿的独立表面游丝从 16 根减为 6 根，边缘附件从 8 根减为 4 根。两层细毛 shader 仍保留。
- 填满的 16 格网格块上传后释放托管列表和 Mesh CPU 副本，不再保留不需要继续修改的数据。
- 验收截图增加非空白检查。隐藏的 Windows 游戏窗口无法得到有效背缓冲截图，最终回放使用正常显示的游戏窗口。

## 最终证据

- [V5/V6 同镜头针脚对照](runtime-rounded-stitch-comparison.png)
- [颜色交界对照](runtime-rounded-edge-comparison.png)
- [外轮廓对照](runtime-rounded-outer-comparison.png)
- [游戏画面和近景](runtime-rounded-preview.png)
- [全图](runtime-rounded/06_finished_art.png)
- [连线过程](runtime-rounded/03_thread_in_progress.png)

对照图只裁切、缩放、排版及合成透明背景。没有后期模糊、锐化、调色或用生成图替换实机画面。近景为斜视几何检查相机，默认游戏仍是正视。所有试验图与失败日志都保留；preview、baseline 和 runtime 第一轮不等同于最终构建。

## 验证结果

- Unity 2022.3.62f2 Windows 构建：Succeeded，errors=0；见 build-rounded-crown.log。
- 最终 540×960 玩家指针事件回放：{len(report['checks'])}/{len(report['checks'])} 项通过，运行错误 0，完成 {report['regionsPlayed']} 个区域、{report['filledCells']}/1334 格。
- 覆盖全部 13 个线团的射线选择、自动定位、滚轮和按钮缩放、拖动、异色拒绝、填色时锁色、牵线可见、完成弹层、重置和恢复画面。
- 最终构建 960×540 横屏冒烟回放：{len(landscape['checks'])}/{len(landscape['checks'])} 项通过。
- 网格独立顶点数：{report['plushVertices']:,}，相对 V5 减少 {checks['vertexReductionPercent']:.2f}%；边界毛丝 {report['boundaryFibers']:,} 根。此计数不把毛层重复渲染的顶点再次相加，也不等于 GPU 总工作量或帧率提升比例。
- 重置与初始棋盘的平均通道差：{checks['resetMeanDifference']:.8f}/255。
- 改变填充顺序恢复后，同镜头几何图平均通道差：{checks['restoreMeanDifference']:.8f}/255；最大差 {checks['restoreMaxDifference']}/255；通道差超过 10 的像素 {checks['restorePixelsOver10']}。透明毛丝混合顺序仍会造成局部差异，不宣称逐像素完全一致。
- 整次回放累计几何生成耗时 {report['geometryGenerationMilliseconds']:.1f} ms、网格上传及释放耗时 {report['meshUploadMilliseconds']:.1f} ms。这是累计 CPU 时间，不是单帧耗时或移动设备性能结论。
- 验收强制回收后的托管堆 {checks['managedHeapMiBAfterCollection']:.1f} MiB；Unity 分配统计 {checks['unityAllocatedMiB']:.1f} MiB。二者口径不同，不相加，也不代表进程或显存总量。
- 验收没有写用户存档；关卡 JSON SHA-256 保持 `{checks['levelSha256']}`。

## 仍未完成

1. 参考图最终品质仍需视觉验收。当前线圈之间的开口与纵向列感仍比参考图明显，不能只凭“改成回环”和检查通过就认定柔软度、编织结构已经达标。
2. 原版同关卡、同阶段的完整实机基准；现有原版运行截图与当前 326863 不是同一图片。
3. 原版 Lottie 的实际播放、结算过渡、编织路径与镜头/音画时序。当前完成弹层和自制爆发仍是占位表现。
4. Android 性能及实体双指操作。本轮只覆盖 Windows；约 433 万顶点与大渲染纹理仍需在目标设备分析。

本轮交付是毛线呈现与内存处理的一次可运行修订，没有把以上未完成项标为完成。周边功能继续不在单机核心范围内。
'''
(OUT/'REPORT.md').write_text(body, encoding='utf-8')
paths = [ROOT/p for p in [
    'Assets/Scripts/Presentation/PlushYarnBoard.cs',
    'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs',
    'Assets/Resources/Levels/level_326863.json',
    'Tools/ReviewLinkedKnit.py', 'Tools/RecordLinkedKnitDelivery.py', 'README.md',
    'Builds/YarnScapesOffline.exe', 'Builds/YarnScapesOffline_Data/Managed/Assembly-CSharp.dll',
    'Builds/YarnScapesOffline_Data/resources.assets']]
paths += list((ROOT/'Assets/Resources/YarnMaterialsV6').glob('*'))
paths += [p for p in RUN.iterdir() if p.is_file()]
paths += [p for p in (OUT/'landscape-rounded').iterdir() if p.is_file()]
paths += [OUT/p for p in [
    'REPORT.md', 'visual_checks.json', 'build-rounded-crown.log', 'player-runtime-rounded.log',
    'runtime-rounded-preview.png', 'runtime-rounded-stitch-comparison.png',
    'runtime-rounded-edge-comparison.png', 'runtime-rounded-outer-comparison.png']]
manifest = [dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size,
                 sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths if p.is_file()]
(OUT/'manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
print(OUT/'REPORT.md')
