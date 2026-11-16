# WeaveKit 0.1.0

可单独复制到其他项目的 Unity 本地包。玩法核心、播放计时、视口、牵线、UI 动画和毛线渲染不依赖 YarnScapes 的关卡类型、存档、字体、UI、配色或音频。

## 安装与最小示例

1. 将整个 `com.weavekit.runtime` 文件夹复制到目标工程的 `Packages/` 下；保留 `.meta` 文件。也可以在 Package Manager 中选择 **Add package from disk** 并选本包的 `package.json`。
2. 本包面向 Unity 2022.3、内置渲染管线和 UGUI。纯玩法核心不需要 Unity；毛线着色器尚未适配 URP/HDRP。
3. 在 Package Manager 中导入 **Minimal Weave** 示例，运行菜单 **WeaveKit/Create Sample Scene**，然后 Play。也可以在空场景挂载 `MinimalWeaveDemo`。
4. 示例只生成一个 8×6 双颜色网格，使用本包材质和 Unity 内置字体。选择 Color 1/2 后点格子，滚轮/拖拽操作视口，Reset 重置。

## 依赖方向

```text
宿主项目：关卡 DTO / 资产加载 / 配色 / 存档 / 奖励 / 音频 / 页面
                         ↓ 注入数据与资源，订阅事件
WeaveKit.Unity：棋盘视图 / RegionPlayback / 视口 / 牵线 / UI 特效 / 毛线
                         ↓
WeaveKit.Core：GridDefinition / BoardState / 路径策略 / FillSession
```

`WeaveKit.Core.asmdef` 设置 `noEngineReferences: true`；`WeaveKit.Unity.asmdef` 只引用 Core 和 `Unity.ugui`。因此通用层不能反向访问宿主的 `Assembly-CSharp`。

## 按需接入

| 需要的能力 | 接入点 | 宿主提供 |
|---|---|---|
| 连通填色玩法 | `GridDefinition`、`BoardState`、`FillSession` | 行优先色号数组、调度/存档 |
| 替换路径算法 | `IRegionRoutePlanner` | `Build` 返回同色未完成格的路径，允许回溯重复 |
| Unity 逐步播放 | `RegionPlayback.Initialize(session, seconds)` | 订阅 Started、CellFilled、Reached、Completed、Cancelled |
| 完整 UGUI 棋盘 | `PixelBoardRenderer.Build(grid, settings, onClick, planner)` | 字体、显示色、可选贴图与毛线资产 |
| 视口缩放与拖动 | `BoardViewportInput.Initialize(target, settings, camera)` | `IBoardViewportTarget`；Screen Space Camera 模式传 UI 相机 |
| 牵线动画 | `YarnThreadGraphic.Initialize(target, sparkle, texture, style)` | `ICellPositionSource`、起点 RectTransform、颜色与素材 |
| 放大回弹、闪白 | `UiFeedbackAnimator.Pulse/Flash` | 任意 Transform / UGUI Graphic，可独立使用 |
| 完成爆发 | `CompletionBurst.Initialize(parent, mask, sparkle, settings)` | 贴图与样式，可独立使用 |
| 立体毛线与接触阴影 | `PlushYarnBoard.Initialize(grid, palette, assets)` | 显式 shader/纹理引用或 `WeaveKitResources.LoadWool()` |

默认路径为确定性八邻接 DFS，回溯保证牵线每步相邻。可传 `new DepthFirstRegionRoutePlanner(GridConnectivity.Four)` 改为四邻接。`ConnectedRoute()` 使用构建时注入的同一策略。

## 只使用玩法核心

```csharp
using WeaveKit.Core;

var grid = new GridDefinition(3, 2, 2, new[] { 0, 0, -1, 1, 1, 0 });
var session = new FillSession(new BoardState(grid));
if (session.TryStart(0, 0))
{
    while (session.Advance(out var step))
    {
        // step.Index: 牵线经过的格；step.NewlyFilled: 首次完成该格。
        // 实际游戏由自己的时钟每次 Advance，勿在一帧内循环播放。
    }
}
int[] save = session.State.GetFilledIndices();
session.Reset();
session.Restore(save);
```

色号从 0 开始，默认空格是 -1。`GridDefinition` 复制输入并验证尺寸与色号；调用方修改原始数组不会改变运行中关卡。`Restore` 忽略越界、空格和重复记录。存档格式、关卡 ID、奖励以及数据迁移留给宿主。

## 事件、时钟与资源所有权

- `RegionPlayback` 使用不受 `Time.timeScale` 影响的时间。开始后下一帧发 Started；每步先更新状态和 CellFilled，再发 Reached；最后一步等待结束后发 Completed。回溯步有 Reached，没有重复的 CellFilled。
- Cancel/禁用组件会保留已完成格、停止播放并发 Cancelled，不发 Completed。Reset 再清空状态。恢复进度要求当前会话空闲。每个会话由一个播放控制器持有，播放期间不要从其他控制器写状态。
- `PixelBoardRenderer.FillCancelled` 可接 `thread.Clear`；恢复只重建已完成格，没有牵线或完成动画。更换网格定义时销毁旧视图并创建新视图，单个视图只 Build 一次。
- 视图拥有自动生成的切片、格子对象、播放组件、材质、网格、相机和 RenderTexture；销毁组件时释放。贴图、字体和调用方传入的 Sprite 仍由调用方拥有。
- `UiFeedbackAnimator.Cancel()` 会恢复缩放和最终颜色。完成爆发取消/禁用时清理临时对象，不销毁传入的 Sprite。牵线禁用时清空轨迹。
- `WeaveKitResources` 是可选的包内资源加载入口。渲染组件自身不认识宿主资源路径；使用 Addressables 或其他资产系统时直接填入设置对象即可。

## 视觉与性能边界

本包保留当前的每格 3×4 针脚结构和确定性几何；通过 `WoolRenderAssets.GeometrySeed` 改变随机种子。显示色由宿主提供，立体毛线目前支持最多 255 色。`RenderLayer` 默认 30，请为离屏毛线舞台保留该层，并从宿主普通相机的 culling mask 中排除；示例背景相机不渲染该层。

毛线渲染使用内置管线、Shader Model 3.0、透明合成、ARGBHalf 高度场和按视口自适应的 1024–4096 宽 RenderTexture。它不依赖场景灯光，但大量格子的几何与渲染纹理有实际内存成本；移动设备与不同 GPU 需要另做性能验收。若只需平面棋盘，将 `BoardViewSettings.Wool` 留空，不加载毛线组件。

这些是当前重建的表现组件；完成爆发仍为程序动画，不代表原作 Lottie 或全部视觉表现已经一致。

## 验证示例

示例支持 `--weavekit-smoke --weavekit-output <目录>`，自动检查填色、UI 点击、取消重启、恢复、独立反馈动画、特效清理和组件移除，输出检查文本与截图。`WeaveKit.Samples.Editor.MinimalWeaveBuilder.Build` 可用于 batchmode 构建示例 Windows Player。

