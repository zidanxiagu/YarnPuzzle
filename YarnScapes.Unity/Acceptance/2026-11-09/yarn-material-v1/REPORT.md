# 毛线材质 V1 · 2026-11-09

本轮按用户提供的毛线像素画参考图设计并应用材质 V1。它是获用户授权的材质设计变体，不是原作材质的恢复证明。

## 设计与接入

- 密实、连续的 V 形线圈；每个逻辑格子使用 2×2 针脚。
- 周期高度场定义线股起伏，生成的中性棉线底稿提供纤维粗糙度；哑光光照、针缝遮蔽和柔和外轮廓阴影。
- 图案填色、未填区域、底部线团绕线、移动纱线统一处理。深色纱线有显示层提亮，便于看清纤维。
- 4K 图案贴图、三线性过滤和 mipmap；相邻格子共用连续针脚坐标。
- 原版图案资产保留；未配置 V1 材质的关卡回退使用原资产。

## 实机验收

Unity 2022.3.62f2，Windows/D3D11，540×960。build-final.log：Succeeded，errors=0。

最终运行 52/52 项检查通过，异常 0。全部 32 个连通区域通过 UI 射线与指针事件完成，1334/1334 格填满。包含缩放、拖动、13 色选择、连线可见、异色拒绝、通关、重置、存档文件不变。此前 runtime/ 是试做阶段；最终效果以 final-runtime/ 为准。

- [材质预览（两张实机画面裁切）](material-v1-preview.png)
- [全图](final-runtime/06_finished_art.png)
- [3.2 倍放大](final-runtime/07_yarn_material_closeup.png)
- [编织过程及底部线团](final-runtime/03_thread_in_progress.png)
- [机器检查结果](final-runtime/interaction_report.json)

关卡 JSON SHA-256：`c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251`。与修改前记录一致；格子编号、分组、调色板数据和关卡数值没有修改。深色提亮仅发生在材质显示层。

截图验证的是指定视图下的静态渲染；缩放时的时域稳定性与实体触屏仍需人工设备复核。本轮没有给出原版还原度分数，没有宣称整体动画/特效的原版一致性验收完成。

## 再生成

- 中性生成底稿：`aigc/yarn-knit-reference-v1.png`
- 材质烘焙：`Tools/BakeYarnMaterial.py`（Python + Pillow + NumPy）
- 纹理导入：`Assets/Editor/YarnMaterialImport.cs`
- 运行资源：`Assets/Resources/YarnMaterialsV1/`

材质版本说明及源文件、构建程序集、最终运行证据的 SHA-256 见同目录 manifest.json。
