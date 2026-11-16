"""Present two actual Unity GPU frames; no invented game mockup."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json
import hashlib
ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / 'Acceptance/2026-11-09/yarn-material-v1'
RUNTIME = EVIDENCE / 'final-runtime'


def main():
    checks = json.loads((RUNTIME / 'interaction_report.json').read_text(encoding='utf-8-sig'))
    if not checks['fullPlaythrough'] or checks['failed']:
        raise RuntimeError('Final material acceptance has not passed yet')
    material = json.loads((ROOT / 'Assets/Resources/YarnMaterialsV1/material_manifest.json').read_text())
    current = hashlib.sha256((ROOT / 'Assets/Resources/Levels/level_326863.json').read_bytes()).hexdigest()
    assert current == material['levelJsonSha256'] == 'c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
    fit = Image.open(RUNTIME / '06_finished_art.png').convert('RGB')
    zoom = Image.open(RUNTIME / '07_yarn_material_closeup.png').convert('RGB')
    assert fit.size == zoom.size == (540, 960)
    preview = Image.new('RGB', (1064, 710), '#f3f0eb')
    draw = ImageDraw.Draw(preview)
    face = 'C:/Windows/Fonts/msyh.ttc'
    title = ImageFont.truetype(face, 28)
    label = ImageFont.truetype(face, 20)
    small = ImageFont.truetype(face, 17)
    draw.text((24, 16), '毛线材质 V1', font=title, fill='#28323c')
    draw.text((24, 66), '整体效果', font=label, fill='#485461')
    draw.text((540, 66), '3.2× 针脚细节', font=label, fill='#485461')
    preview.paste(fit.crop((20, 134, 520, 696)), (24, 104))
    preview.paste(zoom.crop((20, 134, 520, 696)), (540, 104))
    draw.text((24, 679), 'Unity 实机截图 · 密实 V 形针脚 / 柔和捻纹 / 哑光纤维', font=small, fill='#65717c')
    preview.save(EVIDENCE / 'material-v1-preview.png')
    pass_count = sum(item['pass'] for item in checks['checks'])
    report = f'''# 毛线材质 V1 · 2026-11-09

本轮按用户提供的毛线像素画参考图设计并应用材质 V1。它是获用户授权的材质设计变体，不是原作材质的恢复证明。

## 设计与接入

- 密实、连续的 V 形线圈；每个逻辑格子使用 2×2 针脚。
- 周期高度场定义线股起伏，生成的中性棉线底稿提供纤维粗糙度；哑光光照、针缝遮蔽和柔和外轮廓阴影。
- 图案填色、未填区域、底部线团绕线、移动纱线统一处理。深色纱线有显示层提亮，便于看清纤维。
- 4K 图案贴图、三线性过滤和 mipmap；相邻格子共用连续针脚坐标。
- 原版图案资产保留；未配置 V1 材质的关卡回退使用原资产。

## 实机验收

Unity 2022.3.62f2，Windows/D3D11，540×960。build-final.log：Succeeded，errors=0。

最终运行 {pass_count}/{len(checks['checks'])} 项检查通过，异常 {len(checks['errors'])}。全部 {checks['regionsPlayed']} 个连通区域通过 UI 射线与指针事件完成，{checks['filledCells']}/{checks['paintableCells']} 格填满。包含缩放、拖动、13 色选择、连线可见、异色拒绝、通关、重置、存档文件不变。此前 runtime/ 是试做阶段；最终效果以 final-runtime/ 为准。

- [材质预览（两张实机画面裁切）](material-v1-preview.png)
- [全图](final-runtime/06_finished_art.png)
- [3.2 倍放大](final-runtime/07_yarn_material_closeup.png)
- [编织过程及底部线团](final-runtime/03_thread_in_progress.png)
- [机器检查结果](final-runtime/interaction_report.json)

关卡 JSON SHA-256：`{current}`。与修改前记录一致；格子编号、分组、调色板数据和关卡数值没有修改。深色提亮仅发生在材质显示层。

截图验证的是指定视图下的静态渲染；缩放时的时域稳定性与实体触屏仍需人工设备复核。本轮没有给出原版还原度分数，没有宣称整体动画/特效的原版一致性验收完成。

## 再生成

- 中性生成底稿：`aigc/yarn-knit-reference-v1.png`
- 材质烘焙：`Tools/BakeYarnMaterial.py`（Python + Pillow + NumPy）
- 纹理导入：`Assets/Editor/YarnMaterialImport.cs`
- 运行资源：`Assets/Resources/YarnMaterialsV1/`

材质版本说明及源文件、构建程序集、最终运行证据的 SHA-256 见同目录 manifest.json。
'''
    (EVIDENCE / 'REPORT.md').write_text(report, encoding='utf-8')
    paths = [ROOT / 'Assets/Scripts/Presentation/PixelBoardRenderer.cs', ROOT / 'Assets/Scripts/Presentation/YarnThreadGraphic.cs',
             ROOT / 'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs', ROOT / 'Assets/Scripts/YarnScapesRuntime.cs',
             ROOT / 'Assets/Editor/YarnMaterialImport.cs', ROOT / 'Tools/BakeYarnMaterial.py', ROOT / 'Tools/CreateYarnMaterialPreview.py',
             ROOT / 'Assets/Resources/Levels/level_326863.json', ROOT / 'aigc/yarn-knit-reference-v1.png',
             ROOT / 'Builds/YarnScapesOffline_Data/Managed/Assembly-CSharp.dll']
    paths += list((ROOT / 'Assets/Resources/YarnMaterialsV1').glob('*.png'))
    paths += [p for p in RUNTIME.iterdir() if p.is_file()]
    paths += [EVIDENCE / 'material-v1-preview.png', EVIDENCE / 'REPORT.md']
    hashes = [dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths]
    (EVIDENCE / 'manifest.json').write_text(json.dumps(hashes, indent=2, ensure_ascii=False), encoding='utf-8')
    print(json.dumps(dict(passed=pass_count, checks=len(checks['checks']), filled=checks['filledCells'], preview=str(EVIDENCE/'material-v1-preview.png')), ensure_ascii=False))

if __name__ == '__main__':
    main()
