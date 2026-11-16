"""Prepare an isolated reuse project, then record actual runtime evidence and package hashes.

Run --prepare-sample before building the independent project; run without it after both players finish.
Uses only local files and never launches Unity or modifies user progress.
"""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import zipfile
from PIL import Image
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output', default='Acceptance/2026-11-10/refactor-reuse-v1')
parser.add_argument('--baseline', default='Acceptance/2026-11-10/yarn-cozy-v9/runtime')
parser.add_argument('--prepare-sample', action='store_true')
args = parser.parse_args()
output = (ROOT / args.output).resolve()
package = ROOT / 'Packages/com.weavekit.runtime'
output.mkdir(parents=True, exist_ok=True)

if args.prepare_sample:
    project = output / 'reuse-project'
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        (project / folder).mkdir(parents=True, exist_ok=True)
    shutil.copytree(package, project / 'Packages/com.weavekit.runtime', dirs_exist_ok=True)
    shutil.copytree(package / 'Samples~/MinimalWeave', project / 'Assets/MinimalWeave', dirs_exist_ok=True)
    shutil.copy2(ROOT / 'Packages/manifest.json', project / 'Packages/manifest.json')
    shutil.copy2(ROOT / 'ProjectSettings/ProjectVersion.txt', project / 'ProjectSettings/ProjectVersion.txt')
    print(project)
    raise SystemExit(0)

runtime = json.loads((output / 'runtime/interaction_report.json').read_text(encoding='utf-8-sig'))
assert runtime['failed'] == 0 and runtime['fullPlaythrough'] and runtime['filledCells'] == 1334
assert not runtime['errors']
contracts = (output / 'contract-checks.txt').read_text(encoding='utf-8-sig').splitlines()
standalone = (output / 'standalone/smoke-checks.txt').read_text(encoding='utf-8-sig').splitlines()
assert contracts and standalone and all(line.startswith('PASS ') for line in contracts + standalone)
for log_name in ('build-final.log', 'runtime.log', 'reuse-build-final.log', 'standalone.log'):
    log = (output / log_name).read_text(encoding='utf-8-sig', errors='replace')
    assert 'error CS' not in log and 'NullReferenceException' not in log and 'MissingReferenceException' not in log, log_name
level_hash = hashlib.sha256((ROOT / 'Assets/Resources/Levels/level_326863.json').read_bytes()).hexdigest()
assert level_hash == 'c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
images = []
for name in ('01_fit.png', '02_selected_zoom.png', '06_finished_art.png', '07_yarn_material_closeup.png',
             '08_geometry_oblique.png', '10_fiber_edge.png', '11_outer_edge.png', '12_restored_geometry.png', '13_geometry_height.png'):
    before = np.asarray(Image.open(ROOT / args.baseline / name).convert('RGBA'), dtype=np.int16)
    after = np.asarray(Image.open(output / 'runtime' / name).convert('RGBA'), dtype=np.int16)
    assert before.shape == after.shape, name
    delta = np.abs(after - before)
    images.append({'file': name, 'identical': bool(np.array_equal(before, after)),
                   'mean_absolute_channel_delta_255': float(delta.mean()),
                   'changed_pixel_percent': float(np.any(delta > 0, axis=2).mean() * 100)})

files = []
for path in sorted(package.rglob('*')):
    if path.is_file(): files.append({'path': path.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
manifest = {'package': 'com.weavekit.runtime', 'version': '0.1.0', 'unity': '2022.3.62f2',
            'contract_checks': len(contracts), 'random_route_oracle_cases': 600,
            'runtime_checks': len(runtime['checks']), 'standalone_checks': len(standalone),
            'level_sha256': level_hash, 'filled_cells': runtime['filledCells'], 'regions': runtime['regionsPlayed'],
            'vertices': runtime['plushVertices'], 'boundary_fibers': runtime['boundaryFibers'],
            'visual_comparison': images, 'package_files': files}
(output / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
with zipfile.ZipFile(output / 'WeaveKit-0.1.0.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for item in files:
        path = ROOT / item['path']
        archive.write(path, 'com.weavekit.runtime/' + path.relative_to(package).as_posix())
rows = '\n'.join(f"| {x['file']} | {'完全相同' if x['identical'] else '有差异'} | {x['mean_absolute_channel_delta_255']:.6f} | {x['changed_pixel_percent']:.4f}% |" for x in images)
report = f'''# WeaveKit 重构验收（2026-11-10）

已将玩法与通用表现迁移到 `Packages/com.weavekit.runtime`；YarnScapes 通过 `YarnScapesComposition` 提供数据、颜色和资源。包内含 Core/Unity 独立程序集、毛线 shader/贴图、最小示例和接入说明。

## 已验证

- 核心契约：{len(contracts)}/{len(contracts)}，含 600 条随机路径与独立 BFS 区域/相邻性检查、非法数据、恢复、取消与重启、真实关卡、现有存档格式往返。
- YarnScapes 渲染 Windows Player：{len(runtime['checks'])}/{len(runtime['checks'])}，完整播放 {runtime['regionsPlayed']} 区域、{runtime['filledCells']} 格；用户进度文件未改变。
- 独立空工程：仅复制本包、包示例和 Unity 模块清单，构建与运行通过 {len(standalone)}/{len(standalone)}。覆盖纯包双颜色棋盘、UI 点击、牵线、取消/禁用/重启、几何恢复、完成特效清理、回调重入、独立 UI 反馈、无毛线平面模式和移除组件时清理。
- Core 无 Unity 程序集引用；Unity 层无 Assembly-CSharp/YarnScapes 引用。独立样例不含原作关卡、UI、字体、音频或存档。
- 原关卡 JSON SHA-256：`{level_hash}`。
- 网格仍为 {runtime['plushVertices']:,} 顶点、{runtime['boundaryFibers']:,} 条边界毛丝、每逻辑格 {runtime['stitchesPerCell']} 针脚。

## V9 同镜头比较

比较原始 RGBA PNG，不做缩放、滤镜或调色。均值单位为每通道 0–255；不比较时刻不同的正在牵线画面。

| 画面 | 逐像素结果 | 通道平均绝对差 | 有变化像素 |
|---|---|---:|---:|
{rows}

此表验证重构前后的回归情况，不代表已达到参考图或原作的美术一致性。

## 交付与复查

- 可复制包：`WeaveKit-0.1.0.zip`；接入指南位于包根 README.md。
- `contract-checks.txt`、`runtime/interaction_report.json`、`standalone/smoke-checks.txt`。
- 构建：`build-final.log`、`reuse-build-final.log`；渲染运行：`runtime.log`、`standalone.log`。
- `standalone/standalone-sample.png` 是独立工程实机画面；主项目截图在 `runtime/`。
- `manifest.json` 记录逐文件哈希与图像差异；`source-before.zip` 保存本轮重构前脚本。

适用范围：Unity 2022.3 内置管线 + UGUI。URP/HDRP、真机双指输入与移动设备性能未在本轮验证。原作 Lottie、原版时序与最终材质美术验收仍是已有未完成项；本轮没有将其标记为完成。
'''
(output / 'REPORT.md').write_text(report, encoding='utf-8')
print(json.dumps({k: v for k, v in manifest.items() if k != 'package_files'}, ensure_ascii=False, indent=2))
