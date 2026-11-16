# YarnScapes 1.0.8 离线游戏框架

2026-11-16：默认启动进入首页，支持图库 → 关卡 → 完成 → 首页，以及每日、我的作品与设置。工程入口仍为 `Assets/Scenes/Bootstrap.unity`，Unity 2022.3.62f2。

## 已接通的流程

- 启动图标 → 图库；底部为原包实际显示的 Library / Daily / My 三个页签。
- 图库保留原包 15 个分类及各分类内的记录顺序。两列图片网格、滚动、分页、可玩/完整目录过滤、进度角标、继续上次作品。
- 未完成作品进入编织；返回、切到后台、退出以及区域编织完成时保存。返回中止当前编织播放，已经填好的格子保留。
- 完成作品进入结果页，显示成品图，可返回图库、打开本分类下一张、确认后重新编织；再次点击已完成作品直接进入结果页。
- 我的作品区分进行中与已完成，读取同一份逐关存档。
- 每日按原始 `online_date` 转本地日期显示月历，支持切换月份；未缓存作品有明确提示。离线包没有今天的数据时不制造“今日关卡”。
- 设置包括背景音乐、编织音效、完成颜色后自动切换、图库显示已完成作品；持久化到本地。
- 重置/重新编织需确认。存档采用临时文件替换与备份，读取损坏的主文件时尝试备份。

## 内容规模与边界

本轮直接读取根目录 `base.apk` 的 `assets/init_data`，重新核对结果：

| 内容 | 数量 |
| --- | ---: |
| 原始分类 | 15 |
| 分类记录（允许跨分类重复） | 652 |
| 每日记录 | 10 |
| 合计去重作品元数据 | 577 |
| 有本地关卡和图像、可离线编织 | 103 |
| 缺少本地图像的元数据条目 | 474 |

“103 关”表示已恢复的本地可玩内容，不表示原版服务器全部作品。每日记录均没有当前可用的本地图像。完整目录保留这些条目，但不会替换为其他图片或伪造配置。

## 配置入口

Unity 菜单 **YarnScapes → Level Configuration Catalog** 可以按 ID、paintId、分类检索，并打开每关配置。

| 路径 | 用途 |
| --- | --- |
| `Assets/Resources/Framework/app_config.json` | 默认分类、分页数量、底栏页签、默认过滤与设置 |
| `Assets/Resources/Framework/catalog.json` | 577 条作品元数据、分类及顺序、每日日期、资源可用状态 |
| `Assets/Resources/Levels/level_<id>.json` | 103 个可玩关卡的尺寸、色板、格子、资源和表现参数 |
| `Assets/Original/FrameworkSnapshot/` | 从 APK 逐字节保留的初始目录、15 个分类、每日原始 JSON |
| `Assets/Original/framework_recovery_manifest.json` | APK 与源 JSON 哈希、统计、缺失资源清单 |
| `Assets/Original/recovered_level_manifest.json` | 之前的像素图恢复记录 |

每关配置主要字段：

| 字段 | 含义与证据 |
| --- | --- |
| `levelId`, `paintId` | 本地关卡键和原始作品 UUID；恢复自原包 |
| `width`, `height`, `palette`, `cells`, `emptyCellValue` | 小 PNG 的像素与恢复的分色算法；`cells` 长度必须等于宽×高，色号不能越界 |
| `categories`, `unlockType`, `cornerMarker`, `distributeFlag`, `onlineDate`, `timeZone` | 保留原始元数据；不把未核实的角标语义当成玩法规则 |
| `sourceUrl`, `pixelMapUrl`, `gifUrl` | 原始来源，仅用于追溯；离线运行不请求这些地址 |
| `artImageResource`, `finishedImageResource`, `unfilledImageResource` | Unity Resources 路径，不带扩展名 |
| `fillAnimationSeconds`, `routeStepSeconds`, `completionDelaySeconds` | 当前 Unity 表现参数，可按关配置；未声称与原版时序完全一致 |
| `dataEvidenceStatus`, `animationEvidenceStatus` | 数据和表现的证据边界 |
| `rewardGems`, `starThreshold` | 保留兼容字段，设为 0；没有引入无依据的宝石奖励或星级系统 |

同一作品可以在多个分类出现。分类列表用有序 ID 数组表达，不按首次出现分类去重后丢失其它归属；`category_names` 标签与具体分类快照的成员列表是两个独立来源。

缺图作品没有可玩的 `LevelDefinition`。修改格子或尺寸后，同时更新目录中的尺寸、色数、格数摘要并重新验证。不要直接修改 `Original/FrameworkSnapshot`。`Tools/BuildFrameworkData.py` 可从 APK 重建快照、目录与关卡元数据；运行它会重新生成目录和原始元数据字段，手工修改前应备份。

## 代码结构

| 层 | 文件 |
| --- | --- |
| 入口与局内界面 | `Assets/Scripts/YarnScapesRuntime.cs` |
| 首页、页签、导航、结果页 | `Assets/Scripts/Presentation/YarnScapesApp.cs` |
| 公共 UI | `Assets/Scripts/Presentation/FrameworkUI.cs` |
| 元数据、配置、路由状态 | `Assets/Scripts/Core/FrameworkModels.cs`、`LevelModels.cs` |
| 分类/每日与关卡加载 | `Assets/Scripts/Infrastructure/FrameworkRepository.cs`、`ResourcesLevelRepository.cs` |
| 存档与设置 | `Assets/Scripts/Infrastructure/LocalProgressStore.cs` |
| 通用编织引擎及毛线效果 | `Packages/com.weavekit.runtime`，本轮保持原有玩法与毛线设计 |

首页状态在进入/退出编织时保留；同一时刻只创建一个局内实例。资源缩略图按页加载。桌面保持竖版内容区域，宽窗口居中显示。中文字体使用 Windows 的微软雅黑/黑体；跨平台字体打包需后续单独处理。

## 验证与使用

- 正常双击 `Builds/YarnScapesOffline.exe`，默认进入首页。
- `--level-id 326861` 保留直接进关的调试入口。
- Unity 菜单 **YarnScapes → Verify Framework and Build** 校验所有可玩配置、资源、分类引用、存档、设置与备份恢复，再构建 Windows Player。
- `--framework-acceptance-output <绝对目录>` 执行可视化导航验收，在指定目录使用隔离存档，生成截图和 `runtime_checks.json`；测试目录应为新目录。
- 对同一目录追加 `--framework-resume`，在新进程中校验设置和逐格进度恢复。
- 原有 `--acceptance-output <目录> --acceptance-smoke` 仍用于局内指针、缩放、编织和重置回归。
- 验收记录在 `Acceptance/2026-11-16/framework/`。

## 原版证据与尚未恢复的部分

**recovered**：原包 Home 的三个底栏路由（`com/pixelstudio/yarnscapes/home/IIILLlIi1IilI.java:126`）、分类/每日 JSON、作品 ID/UUID、资源映射、可玩像素图、原始标题/底栏图标。

**reimplemented**：Unity 页面布局、分页、页面生命周期、离线缺图提示、JSON 存档、完成页与过渡、设置实现。Windows 已验证不等于逐像素匹配原版，也不等于移动设备触摸验证。

**unknown / not implemented**：服务器未下发的图片与实时推荐、每日实时内容、远端 A/B 配置、广告与付费、社区与 AI 创作、原版分享/导出/回放、道具库存及全部外围机制。章节地图原包 `chapters` 为空，不补造地图。已有毛线渲染仍是获认可的重建设计，原版 GLES 材质与 Lottie 时序尚未复原。

这次交付的验收范围是可运行的离线框架与全量本地配置接入；不宣称所有原版页面及联网功能均已还原。
