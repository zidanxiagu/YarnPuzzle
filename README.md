# YarnPuzzle

YarnScapes 离线 Unity 工程、可复用的 WeaveKit 包，以及素材恢复、材质迭代和验收资料。

## 获取与运行

本仓库使用 **Git LFS** 保存 APK、图片、音频和归档文件。首次获取：

```sh
git lfs install
git clone https://github.com/zidanxiagu/YarnPuzzle.git
cd YarnPuzzle
git lfs pull
```

1. 使用 **Unity 2022.3.62f2** 打开 `YarnScapes.Unity`，不是仓库根目录。
2. 等待 Unity 导入素材并恢复 Packages。
3. 打开 `Assets/Scenes/Bootstrap.unity`，点击 Play。

游戏支持选色、连通区域编织、缩放拖拽、自动存档与完成反馈。工程详细说明见
[YarnScapes.Unity/README.md](YarnScapes.Unity/README.md)。

## 目录

| 路径 | 内容 |
| --- | --- |
| `YarnScapes.Unity/Assets` | 场景、项目脚本、原始及转换后的素材、关卡 |
| `YarnScapes.Unity/Packages/com.weavekit.runtime` | 独立 Core/Unity 程序集、毛线渲染、动画反馈及最小示例 |
| `YarnScapes.Unity/ProjectSettings` | Unity 项目配置 |
| `YarnScapes.Unity/Tools` | 素材准备、关卡导入和验收脚本 |
| `YarnScapes.Unity/Acceptance` | 历次报告、截图、日志、检查结果和独立复用工程 |
| `YarnScapes.Unity/aigc` | 毛线材质参考与设计图 |
| `aigc` | 配色概念图及排版脚本 |
| 根目录 `*.apk` / `*.apk.manifest.bin` | 本地原始安装包及清单输入 |

Unity 缓存、自动生成的工程文件、用户设置和构建输出不纳入版本控制；首次打开后可重新生成。
`Assets` 和嵌入包的 `.meta` 文件均保留。

## 复用与验证

[WeaveKit 接入说明](YarnScapes.Unity/Packages/com.weavekit.runtime/README.md)
介绍独立包的接口、最小示例和接入限制。包可整体复制到其他 Unity 工程的 `Packages` 目录。

在 Unity 菜单执行 **YarnScapes / Verify WeaveKit and Build** 可运行核心检查并构建 Windows Player。
已记录的历史结果及验证范围见
[重构验收报告](YarnScapes.Unity/Acceptance/2026-11-10/refactor-reuse-v1/REPORT.md)。

## 当前设计状态

- 当前工程采用 V9 紧密毛线及重构后的 WeaveKit，毛线效果已获用户认可。
- 用户要求颜色鲜亮、柔和，避免灰褐和过低饱和。
  [鲜彩奶油配色预览](aigc/yarn-color-bright-preview-2026-11-11.png) 是概念设计，**尚未应用到工程**。
- 原版 Lottie、动画时序一致性、Android 真机手势与性能仍需单独验证。
  历史检查通过不代表这些事项已经完成。

部分工具与历史报告包含制作时的 Windows 绝对路径；在其他电脑运行工具前应检查其输入与输出路径。
