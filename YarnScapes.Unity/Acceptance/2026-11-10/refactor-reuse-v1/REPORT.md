# WeaveKit 重构验收（2026-11-10）

已将玩法与通用表现迁移到 `Packages/com.weavekit.runtime`；YarnScapes 通过 `YarnScapesComposition` 提供数据、颜色和资源。包内含 Core/Unity 独立程序集、毛线 shader/贴图、最小示例和接入说明。

## 已验证

- 核心契约：30/30，含 600 条随机路径与独立 BFS 区域/相邻性检查、非法数据、恢复、取消与重启、真实关卡、现有存档格式往返。
- YarnScapes 渲染 Windows Player：70/70，完整播放 32 区域、1334 格；用户进度文件未改变。
- 独立空工程：仅复制本包、包示例和 Unity 模块清单，构建与运行通过 22/22。覆盖纯包双颜色棋盘、UI 点击、牵线、取消/禁用/重启、几何恢复、完成特效清理、回调重入、独立 UI 反馈、无毛线平面模式和移除组件时清理。
- Core 无 Unity 程序集引用；Unity 层无 Assembly-CSharp/YarnScapes 引用。独立样例不含原作关卡、UI、字体、音频或存档。
- 原关卡 JSON SHA-256：`c7338c3adbbe2b8e96f29fdc826221e0ce60bc18d95120d2464eb99c01049251`。
- 网格仍为 5,699,400 顶点、43,424 条边界毛丝、每逻辑格 12 针脚。

## V9 同镜头比较

比较原始 RGBA PNG，不做缩放、滤镜或调色。均值单位为每通道 0–255；不比较时刻不同的正在牵线画面。

| 画面 | 逐像素结果 | 通道平均绝对差 | 有变化像素 |
|---|---|---:|---:|
| 01_fit.png | 完全相同 | 0.000000 | 0.0000% |
| 02_selected_zoom.png | 完全相同 | 0.000000 | 0.0000% |
| 06_finished_art.png | 完全相同 | 0.000000 | 0.0000% |
| 07_yarn_material_closeup.png | 完全相同 | 0.000000 | 0.0000% |
| 08_geometry_oblique.png | 有差异 | 0.000351 | 0.0844% |
| 10_fiber_edge.png | 有差异 | 0.000087 | 0.0219% |
| 11_outer_edge.png | 有差异 | 0.000066 | 0.0160% |
| 12_restored_geometry.png | 有差异 | 0.000399 | 0.0982% |
| 13_geometry_height.png | 完全相同 | 0.000000 | 0.0000% |

此表验证重构前后的回归情况，不代表已达到参考图或原作的美术一致性。

## 交付与复查

- 可复制包：`WeaveKit-0.1.0.zip`；接入指南位于包根 README.md。
- `contract-checks.txt`、`runtime/interaction_report.json`、`standalone/smoke-checks.txt`。
- 构建：`build-final.log`、`reuse-build-final.log`；渲染运行：`runtime.log`、`standalone.log`。
- `standalone/standalone-sample.png` 是独立工程实机画面；主项目截图在 `runtime/`。
- `manifest.json` 记录逐文件哈希与图像差异；`source-before.zip` 保存本轮重构前脚本。

适用范围：Unity 2022.3 内置管线 + UGUI。URP/HDRP、真机双指输入与移动设备性能未在本轮验证。原作 Lottie、原版时序与最终材质美术验收仍是已有未完成项；本轮没有将其标记为完成。
