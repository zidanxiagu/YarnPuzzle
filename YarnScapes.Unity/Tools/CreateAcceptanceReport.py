"""Package actual runtime evidence without claiming full visual parity."""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def main():
    root = Path(__file__).resolve().parents[1]
    target = root / "Acceptance/2026-11-09"
    before = json.loads((target / "level_data_audit_before.json").read_text(encoding="utf-8"))
    after = json.loads((target / "level_data_audit_after.json").read_text(encoding="utf-8"))
    probe = json.loads((target / "after/unity_capture.json").read_text(encoding="utf-8"))
    font = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 23)
    small = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 21)
    panels = [("original_offline_core.png", "原版 1.0.8：初始全图"),
              ("original_fill_in_progress.png", "原版：选色放大后的编织过程"),
              ("after/unity_unfilled.png", "Unity 修正后：整体仍不通过")]
    comparison = Image.new("RGB", (1668, 1092), "#edf0f4")
    draw = ImageDraw.Draw(comparison)
    for i, (path, title) in enumerate(panels):
        x = 12 + i * 552
        draw.text((x + 4, 14), title, fill="#152237", font=font)
        picture = Image.open(target / path).convert("RGB").resize((540, 960), Image.Resampling.LANCZOS)
        comparison.paste(picture, (x, 58))
    draw.text((16, 1030), "原版与 Unity 使用不同图片：这里只对照界面和交互状态，不计算逐像素相似度。", fill="#333b49", font=small)
    draw.text((16, 1060), "原版广告属于截图内容；不在离线核心关卡重建范围内。", fill="#566173", font=small)
    comparison.save(target / "comparison.png")

    report = f"""# YarnScapes 核心关卡自验收（2026-11-09）

**整体结论：不通过。当前工程是可运行原型，不满足“达到原版美术品质和效果”的交付标准。**

## 本次依据

- 原版：MuMu 设备 1，Android 15，1080×1920；已核实包名 `com.yarn.scapes.color.by.number.free.games`、versionName `1.0.8`、versionCode `1001080`。
- 采集了原版局内初始全图、选色自动放大、单次点击后的编织过程、自动切换下一色等画面。原始 PNG 保存在本目录。
- Unity：2022.3.62f2，Windows Direct3D 11；以 540×960 直接从 GPU 导出画面，避免 Windows DPI 和桌面窗口遮挡影响判断。
- 数值来源：APK 的 `finished_url` 小 PNG（60×60），以及 `IlLlLl1IL.java` 中的色彩分析规则。`i512_url` 大图是成品效果图，不能拿来随意聚类生成关卡色号。
- 原版实机本次打开的是图库首张 Love 图片；Unity 当前是 326863 星星图。因此运行截图只证明布局、材质状态和交互差异，不能用于同关卡逐像素相似度评分。326863 的数据检查另以原包 PNG 为依据。

## 验收表

| 项目 | 实际证据 | 结论 |
|---|---|---|
| 关卡尺寸与可填格数 | 原图 {after['expectedSize'][0]}×{after['expectedSize'][1]}，{after['expectedPaintableCells']} 个可填格 | 数据层对照通过 |
| 色组与逐格色号 | 旧版 {before['actualColorGroups']} 组，原算法 {before['expectedColorGroups']} 组，{before['cellGroupMismatches']} 格色号不同；修后 {after['actualColorGroups']} 组、{after['cellGroupMismatches']} 差异 | 已修；仍缺原版运行缓存的独立核对 |
| 初始画面 | 原版白底、简洁返回按钮、全图灰阶；Unity 仍有自制标题、大块编织背景和密集数字 | 不通过 |
| 选色与镜头 | 原版选色后自动放大，出现细网格；当前色号加粗/高亮；Unity 无对应的缩放、平移与数字显隐状态 | 不通过 |
| 连通填色 | 原版单次点击启动连续编织并能自动切换下一色；Unity 探针单击填 {probe['singleCorrectClickFilledCells']} 格，源数据同色八邻域区域为 {probe['expectedConnectedCells']} 格 | 不通过 |
| 毛线材质与动画 | 原版实拍可见线轴到格子的毛线牵引、扭线、逐格出现、闪光；Unity 只是把成品贴图片段切换出来，附加缩放/闪白 | 不通过，必须重做核心表现层 |
| 色板 | 原版使用细小线轴、彩色编号、选中环及色组完成后的移除/移动；当前仅重建静态线轴与滚动编号 | 部分修复，仍不通过 |
| 完成动画 | 原版 Lottie 仅存入资源目录，运行代码没有播放器；当前仍是自制粒子爆发和遮罩弹层 | 不通过；本次未完成原版结算流程 |
| 构建与启动 | Windows 构建成功，0 errors；真实 GPU 截图启动无托管异常 | 仅证明可运行 |

## 这轮实际修正

1. 从小 PNG 按 alpha ≥100、D65 Lab 色差 ≤2、首次出现顺序生成色号，替换从大图强制聚类的错误做法。
2. 未填图改为灰阶，选中色组加深；这仍是表现近似，不等于移植了原版 OpenGL 材质。
3. 色板改为可滚动的原始线轴贴图组合，提供编号，避免把所有色块挤成细小色条。
4. 关卡贴图关闭 NPOT 缩放、mipmap 和压缩；画板按有效视口布局。
5. 移除无证据的“+10 宝石”显示，给临时完成特效明确改名 Placeholder，防止把资源复用误称为动画还原。
6. 增加独立 GPU 截帧与行为探针入口，隔离于普通游玩，不写用户存档；修订 README，明确失败项。

## 后续重构的顺序与完成条件

1. **先恢复同一图片、同一阶段的基准**：初始、选色放大、编织中、色组完成、全图完成。当前缺 326863 的原版实机完整流程。
2. **替换当前逐格 UGUI 表现方式**：按原版格子状态、灰阶/高亮/成色材质、数字显示层级和镜头变换渲染；恢复同色连通区域与编织路径。
3. **接通原始动画资源**：线轴到格子的动态毛线、逐格编织、闪光、色组完成、原版完成过渡及音画时序。Lottie 文件存在不算接通。
4. **再做界面细节与量化验收**：同分辨率、同图片、同进度逐项对照排版、颜色、针脚与动画关键帧；通过后才扩展其他关卡。

只实现离线核心关卡，广告、图库等周边功能不作为开发范围。截图中的原版广告也不需要复刻。

## 复核说明

- `comparison.png`：原版实机与 Unity 修正后的视觉对照（不同图片）。
- `before/unity_unfilled.png`：修前 GPU 画面；`after/unity_unfilled.png`：修后 GPU 画面。
- `level_data_audit_before.json` / `level_data_audit_after.json`：修前、修后数值检查。
- `after/unity_capture.json`：错误颜色拒绝={probe['wrongColorRejected']}；连通填色一致={probe['connectedFillParityPass']}；完成动画验证={probe['completionAnimationVerified']}。
- `after/unity_filled.png` 只是直接恢复已填集合的渲染样本；不代表用玩家输入通关，也不证明完成动画正确。
- 数值导入与检查共享恢复出的分析实现；其相等是内部一致性检查。原版小 PNG 的原始色值、格数另有直接来源，完整运行缓存一致性仍未验证。
- 早期桌面截图无效；“色板被遮挡”不能据此成立。源码中文正常，早期终端显示的乱码属于读取编码问题。
- 完整构建日志：`Logs/acceptance_final_build.log`；运行日志：`Logs/acceptance_gpu_after.log`。

本报告没有给出“还原度百分比”，因为当前证据不足以支持这种分数。
"""
    (target / "REPORT.md").write_text(report, encoding="utf-8")

    selected = ["original_offline_core.png", "original_color_selected.png", "original_fill_in_progress.png",
                "original_after_single_click.png", "before/unity_unfilled.png", "after/unity_unfilled.png",
                "after/unity_filled.png", "after/unity_capture.json", "level_data_audit_before.json",
                "level_data_audit_after.json", "comparison.png", "REPORT.md"]
    entries = []
    for relative in selected:
        path = target / relative
        entries.append({"file": relative, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                        "bytes": path.stat().st_size,
                        "status": "observed" if relative.startswith("original_") else "reimplemented_or_analysis"})
    (target / "manifest.json").write_text(json.dumps({"date": "2026-11-09", "overall": "FAILED", "artifacts": entries}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"report": str(target / "REPORT.md"), "comparison": str(target / "comparison.png"),
                      "overall": "FAILED", "artifacts": len(entries)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
