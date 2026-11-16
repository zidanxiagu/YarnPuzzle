"""Review actual Unity V2 captures, compare reset, and record reproducible evidence."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import hashlib
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Acceptance/2026-11-09/yarn-material-v2'
RUN=OUT/'soft-runtime'

def main():
    r=json.loads((RUN/'interaction_report.json').read_text(encoding='utf-8-sig'))
    assert r['fullPlaythrough'] and r['failed']==0, 'Runtime acceptance is incomplete or failed'
    assert r['plushVertices']>0
    before=Image.open(RUN/'01_fit.png').convert('RGB').crop((20,134,520,696))
    reset=Image.open(RUN/'09_reset.png').convert('RGB').crop((20,134,520,696))
    difference=np.abs(np.array(before,dtype=float)-np.array(reset,dtype=float))
    reset_error=float(difference.mean())
    assert reset_error<.15, f'Reset left visible board differences: {reset_error}'
    old=Image.open(ROOT/'Acceptance/2026-11-09/yarn-material-v1/final-runtime/07_yarn_material_closeup.png').convert('RGB').crop((20,134,520,696))
    close=Image.open(RUN/'07_yarn_material_closeup.png').convert('RGB').crop((20,134,520,696))
    proof=np.asarray(Image.open(RUN/'08_geometry_oblique.png').convert('RGBA'),dtype=float)
    # The camera target contains premultiplied alpha; PNG compositing needs straight alpha.
    alpha=proof[:,:,3:4]/255
    rgb=np.where(alpha>.001,proof[:,:,:3]/np.maximum(alpha,.001),0)
    straight=np.concatenate((np.clip(rgb,0,255),proof[:,:,3:4]),2).astype('uint8')
    macro=Image.fromarray(straight)
    paper=Image.new('RGBA',macro.size,'#e8e3d9')
    paper.alpha_composite(macro)
    macro=paper.convert('RGB').resize((562,562),Image.Resampling.LANCZOS).crop((31,0,531,562))
    face='C:/Windows/Fonts/msyh.ttc'
    title=ImageFont.truetype(face,28); label=ImageFont.truetype(face,20); small=ImageFont.truetype(face,17)
    preview=Image.new('RGB',(1064,710),'#f3f0eb')
    draw=ImageDraw.Draw(preview)
    draw.text((24,16),'毛线材质 V2 · 蓬松立体',font=title,fill='#28323c')
    draw.text((24,66),'3.2× 实机针脚',font=label,fill='#485461')
    draw.text((540,66),'斜视近景 · 线圈与绒毛',font=label,fill='#485461')
    preview.paste(close,(24,104));preview.paste(macro,(540,104))
    draw.text((24,679),'真实线圈网格 / 深度遮挡 / 独立细绒毛 · Unity 渲染画面',font=small,fill='#65717c')
    preview.save(OUT/'plush-v2-preview.png')
    compare=Image.new('RGB',(1064,710),'#f3f0eb');draw=ImageDraw.Draw(compare)
    draw.text((24,16),'同一关卡、同一倍率 · 材质对比',font=title,fill='#28323c')
    draw.text((24,66),'V1 · 烘焙平面纹理',font=label,fill='#485461')
    draw.text((540,66),'V2 · 立体线圈与绒毛',font=label,fill='#485461')
    compare.paste(old,(24,104));compare.paste(close,(540,104))
    draw.text((24,679),'两侧均为实际 Windows 玩家截图裁切；未使用 AI 替代游戏画面',font=small,fill='#65717c')
    compare.save(OUT/'v1-v2-comparison.png')
    current=hashlib.sha256((ROOT/'Assets/Resources/Levels/level_326863.json').read_bytes()).hexdigest()
    assert current=='c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251'
    total=len(r['checks']);passed=sum(c['pass'] for c in r['checks'])
    result=dict(checks=total,passed=passed,filled=r['filledCells'],paintable=r['paintableCells'],regions=r['regionsPlayed'],plushVertices=r['plushVertices'],resetMeanPixelDifference=reset_error,levelJsonUnchanged=True)
    (OUT/'visual_checks.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    report=f'''# 毛线材质 V2 · 蓬松与立体感

当前用户要求优先：按参考图继续加强毛线的蓬松、立体效果。本轮从 V1 的烘焙纹理升级为真实线圈网格，作为新的设计版本交付；不以技术变更代替视觉判断，不声称已完全匹配参考图。

## 具体修改

- 每个逻辑格子包含 2×2 组立体针脚；弯曲的圆柱状线股有粗细变化和前后交叠。
- 使用真实网格法线进行哑光受光，深度缓冲产生线股之间的遮挡；底部有较暗的纤维层支撑。
- 独立短绒毛几何、棉线粗糙度、柔和纤维边光；降低硬高光。
- 透明相机层合入原 UI，保留画布遮罩、格子点击与缩放；按缩放切换 1024–4096 渲染分辨率，闲置时复用图像。
- 编织时立体针脚逐格出现并轻微鼓起。连线与底部线团也加入纤维颗粒。
- 修正格子交界的细白缝、重置时旧网格残留的风险。

## 实机证据

- [本版针脚与斜视近景](plush-v2-preview.png)
- [V1 / V2 同倍率对比](v1-v2-comparison.png)
- [实际完整游戏画面](soft-runtime/06_finished_art.png)
- [实际编织过程](soft-runtime/03_thread_in_progress.png)
- [斜视相机原始渲染](soft-runtime/08_geometry_oblique.png)

主预览左侧为 Unity 玩家实际 UI 截图，右侧为同一批网格的斜视相机近景；相机近景用于展示体积与绒毛，不是游戏默认镜头。图片只裁切、排版与处理透明背景，没有用生成图片替代运行效果。

## 验证

Unity 2022.3.62f2 / Windows / D3D11。build-soft.log：Succeeded，errors=0。

最终运行 {passed}/{total} 检查通过，异常 {len(r['errors'])}。通过 UI 射线与指针事件完成 {r['regionsPlayed']} 个连通区域，{r['filledCells']}/{r['paintableCells']} 格。验证选色、缩放、拖动、异色拒绝、连线、通关、重置，以及几何格子数与进度一致、存档文件未被验收改写。完成图含 {r['plushVertices']:,} 个身体/绒毛顶点。

重置后的棋盘像素与最初空棋盘对比：平均通道差 {reset_error:.6f}/255，未观察到立体残影。

关卡 JSON SHA-256：`{current}`，与修改前一致。关卡编号、颜色分组、格子与配表数据未修改。

## 边界

当前验证限 Windows。密集网格与透明渲染层的 Android 设备性能、实体双指操作尚未实测，发布移动端前需要设备性能分析。效果为参考图方向的新设计；真实阴影投射没有接入，当前体积依靠网格、深度遮挡与法线受光。原版 Lottie、镜头和完整动画一致性仍是独立事项。本轮截图不能证明所有缩放过程中的时域稳定性。

runtime/、final-runtime/ 为早期体积效果。复查后进一步柔化受光、加入沿线股走向的纤维微法线、修正绒毛根部位置并增加密度；最终结果以 soft-runtime/、build-soft.log 为准。源文件、构建程序集与最终证据哈希见 manifest.json。
'''
    (OUT/'REPORT.md').write_text(report,encoding='utf-8')
    paths=[ROOT/'Assets/Scripts/Presentation/PlushYarnBoard.cs',ROOT/'Assets/Scripts/Presentation/PixelBoardRenderer.cs',ROOT/'Assets/Scripts/Presentation/PixelCellView.cs',ROOT/'Assets/Scripts/Presentation/YarnThreadGraphic.cs',ROOT/'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs',ROOT/'Assets/Scripts/YarnScapesRuntime.cs',ROOT/'Assets/Editor/YarnMaterialImport.cs',ROOT/'Tools/PreparePlushAssets.py',ROOT/'Tools/CreatePlushPreview.py',ROOT/'Assets/Resources/Levels/level_326863.json',ROOT/'Builds/YarnScapesOffline_Data/Managed/Assembly-CSharp.dll']
    paths += [p for p in (ROOT/'Assets/Resources/YarnMaterialsV2').iterdir() if p.is_file() and p.suffix!='.meta']
    paths += [p for p in RUN.iterdir() if p.is_file()]
    paths += [OUT/'plush-v2-preview.png',OUT/'v1-v2-comparison.png',OUT/'visual_checks.json',OUT/'REPORT.md']
    hashes=[dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths]
    (OUT/'manifest.json').write_text(json.dumps(hashes,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps(result,ensure_ascii=False))

if __name__=='__main__':
    main()
