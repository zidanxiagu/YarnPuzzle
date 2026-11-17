# 编辑器飞线缺失修复

2026-11-17，Unity 2022.3.62f2，Windows / Direct3D 11。

## 原因与修复

`YarnThreadGraphic` 继承 `MaskableGraphic`，但没有声明 `CanvasRenderer` 依赖。
在独立工程的 Editor Play Mode 中，区域逐步编织、Begin/Reach 和线头坐标更新正常，
飞线对象却没有 CanvasRenderer，因而没有上传网格或材质。
本次只为该类添加 `[RequireComponent(typeof(CanvasRenderer))]`，使 UGUI 注册之前就创建渲染组件。

此前只验证 Windows Player，且 `IsVisible` 只检查内部 opacity，不能证明已绘制。
本次新增了即时组件依赖检查，以及 Player 内长线/线头网格、透明度与裁剪状态检查。

## 实测

- 复用独立检出工程测试，包含当前工作区的棋盘加载优化；没有改动该优化。
- 使用真实 `YarnScapesRuntime`、`level_326863`、隔离存档，以及 `ClickScreenPoint` 触发编织。
- Editor 修复前：飞线 opacity=1、weaving=true、坐标变化，但没有 CanvasRenderer；网格顶点数为 0。
- Editor 修复后：同一场景可见长线与星光；采样中飞线网格为 392、400、408 等顶点，UI/Default 材质就绪、alpha=1。
- 修复前离屏截图中仍能看见很小的星光；未独立复现用户描述的“星光也完全消失”。修复后长线和星光均可见。
- 合同检查：31 项通过，包括未访问惰性 `Graphic.canvasRenderer` getter 之前确认渲染组件存在。
- Windows 构建：Succeeded，0 errors。
- Windows Player：31 项检查通过，0 失败；新增长线与线头实际上传网格检查，用户存档未变化。

Editor 截图使用临时相机离屏渲染 UI，避免隐藏窗口抓屏全黑。采样记录在切换相机前已能证明缺失的渲染组件。

## 证据

- `flight-editor-before.json` / `flight-editor-before.png`
- `flight-editor-after.json` / `flight-editor-after.png`
- `contract-checks.txt`
- `player-checks.json` / `player-thread.png`

本机工程运行时请先退出 Play，等待编译完成，再进入关卡；当前实例不会凭新增依赖声明自动重建。
