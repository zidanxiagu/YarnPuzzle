# YarnScapes Offline Core

## Current application framework (2026-11-16)

The default boot now opens the original three-tab Home structure: Library,
Daily, My. Library categories lead into the existing weaving engine; progress,
completion, replay confirmation, return navigation and settings are connected.
The APK snapshot contains 577 unique metadata entries in 15 categories plus
dated daily records; 103 entries have recovered playable local assets.
See [FRAMEWORK.md](FRAMEWORK.md) for configuration paths, evidence boundaries,
the editor catalog, and executable acceptance. The older reports below describe
earlier milestones and are not claims of full original-game parity.

Acceptance status on 2026-11-09: **FAILED**. This is a reconstruction prototype,
not a visually faithful version. See `Acceptance/2026-11-09/REPORT.md` and the
original runtime captures in that directory before evaluating completion.
The subsequent interaction repair is recorded separately under
`Acceptance/2026-11-09/interaction-fix`; it does not supersede the visual failure.

This is the first Unity reconstruction slice for the local YarnScapes APK set.
It implements only the offline pixel filling loop:

`load level -> choose color and focus -> click matching cell -> weave connected region -> autosave -> completion placeholder`

The project uses Unity `2022.3.62f2` and has no online services, ads, subscriptions,
community, AI creation, or analytics.

## Reusable runtime package (2026-11-10)

Gameplay and reusable presentation now live in the embedded UPM package
[`Packages/com.weavekit.runtime`](Packages/com.weavekit.runtime/README.md).
Copy that whole folder into another project's `Packages/`, or add its
`package.json` through Package Manager. Import its Minimal Weave sample to run
a standalone two-color board without any YarnScapes level, UI, font or audio.

- `WeaveKit.Core` contains immutable grids, progress state, replaceable region
  routing and clock-free fill sessions. Its assembly has no Unity references.
- `WeaveKit.Unity` contains the board view, unscaled playback, viewport input,
  thread, independent pulse/flash and completion effects, and wool renderer.
  Colors, fonts, textures, shaders and timings are supplied explicitly.
- `Assets/Scripts/Presentation/YarnScapesComposition.cs` converts this project's
  recovered DTOs and loads its display palette/resources. `YarnScapesRuntime`
  retains screen composition, selection flow, audio and progress saving.
  Existing save JSON and level data remain compatible.

The package includes its shaders, filament map and thread strip, with import
metadata preserved. V8 shader sources and the active V3 filament map now reside
under `Packages/com.weavekit.runtime/Runtime/Resources/WeaveKit/Wool`; the thread
strip is under `.../WeaveKit`. The material generation scripts target these new
locations. Earlier `Record*Delivery` scripts and reports are historical records
of their pre-refactor source paths, not current release tooling.

Run **YarnScapes/Verify WeaveKit and Build** for the core contract suite and
Windows build. Refactor evidence and the standalone reuse project are under
`Acceptance/2026-11-10/refactor-reuse-v1`. The package guide documents event order,
ownership, cancellation, optional flat rendering, and integration limits.

## Open and run

1. Open this folder in Unity 2022.3.62f2.
2. Open `Assets/Scenes/Bootstrap.unity`.
3. Press Play.

Click a bottom spool to select its color and focus the first remaining cell.
Click a dark highlighted cell with the same number to weave its entire remaining
eight-neighbour region. Different-color cells are rejected. Color changes are
locked while a region is being woven; completing a color selects the next one.
Drag the board to pan; use the mouse wheel, +/− buttons or two-finger pinch to
zoom (1–8x). Double-click toggles zoom, and 全图 restores the fitted view. Drag the
spool strip horizontally to reach all 13 colors. 重置 cancels any current weaving
and clears the board; 保存 saves the currently filled cells. Region completion
also saves automatically. Desktop builds use a resizable portrait window and
keep the portrait play area visible in landscape windows.

Desktop UI raycasts, zoom, drag and connected filling have executable acceptance
coverage. The physical two-finger gesture still needs verification on a device.

## Yarn material variants

The current filled surface uses **V9 compact wool with a soft display palette**.
`PlushYarnBoard` builds rounded interlocking tube meshes, a supporting yarn bed
and separate flyaway-fiber ribbons. A dedicated orthographic camera renders them
with depth testing into a transparent layer over the input grid; custom matte
shading uses actual tube normals. The mesh pops on filling. UI clipping and hit
testing stay on the original board. Render resolution adapts to zoom, and idle
frames reuse the rendered layer. `Assets/Resources/YarnMaterialsV2` contains the
shaders and fuzzy strand/spool assets. Regenerate those small assets with
`Tools/PreparePlushAssets.py`. V1 is retained for the blank surface and fallback.
V3 removes the continuous highlight and uses irregular cotton grain, two sparse
short-pile layers, and finer, tapered flyaways. It shares the original tube mesh
for the pile layers. The surface uses diffuse light without a specular lobe;
the strand ends sit slightly deeper in the knit bed while keeping the volume.
The soft-wool surface shaders and material data are in `Assets/Resources/YarnMaterialsV3`.
Regenerate the deterministic maps with `Tools/PrepareSoftWoolAssets.py`.

V4 replaces the square opaque backing with a recessed bed whose contour follows
filled neighbors. Same-color beds overlap; different-color edges share a small
curved boundary, and exposed edges retreat beneath the stitches. Strand ends
curve behind the bed and close to zero radius. Fine curved fibers are generated
on exposed/color boundaries and become less visible as adjacent cells fill.
Small spatially correlated changes in stitch position, tilt, thickness and loft
are fixed by board coordinates. These changes affect presentation only.
`Assets/Resources/YarnMaterialsV4` contains the new bed and boundary-fiber shaders.

V5 increases the filled surface from 2x2 to 3x3 stitches per logical cell, with
roughly one-third thinner strands and proportionally finer pile/flyaways.
Position drift, stitch-scale tension, slight bending, tilt and left/right
asymmetry vary deterministically across the weave. Curved same-color filament
detail and broad diffuse lighting retain fiber texture without polished edges.
The V5 surface and bed shaders are in `Assets/Resources/YarnMaterialsV5` and reuse
the V3 filament data and V4 neighbor-aware boundary fibers. Mesh chunks contain
16 cells to keep each mesh below the 16-bit index limit at the higher density.

V6 keeps the 3x3 stitch density, but uses narrower, almost constant-section legs
and a rounded crown extending into the following course. The neighboring foot
passes in front of that crown. This replaces the swollen isolated V5 capsules
with linked loops. Stitch tension and position have slightly wider, deterministic
variation. The surface and sparse pile use `Assets/Resources/YarnMaterialsV6`;
the bed and boundary-fiber shaders remain shared with V5/V4. The strand omits its
buried back section, surface flyaways decrease from 16 to 6 per leg, and boundary
fibers decrease from 8 to 4 per attachment. Complete 16-cell chunks release their
managed lists and mesh CPU copy after upload. Full-level unique vertices decrease
from 7,663,316 to 4,326,572; this count excludes repeated pile shader passes.

V7 widens the stitch shoulders into diagonal legs and tucks internal crowns
behind the following course, breaking up the aligned oval chains of V6. A small
nonperiodic shift per global fabric course combines with local tension, tilt,
asymmetry and crossing-depth variation. Horizontal drift is reduced at exposed
color boundaries to preserve the pixel artwork. Surface and bed diffuse shading
are slightly softer, and the short-pile coat is marginally thicker. The surface,
pile and bed shaders are in `Assets/Resources/YarnMaterialsV7`; the V4 boundary
fiber shader remains shared. Density and geometry counts stay at 3x3 stitches
per logical cell and 4,326,572 unique vertices for the completed level.

V8 adds a separate orthographic height pass over the actual opaque loop geometry.
The height and coverage channels feed local horizon occlusion and a short ray
toward the shared light direction. These are 2.5D contact/soft-shadow approximations,
not Unity shadow maps or full volumetric light transport. The height pass excludes
the transparent bed, pile and flyaways, and refreshes with the animated geometry.
The strand and bed retain fill-order-independent shading. Directional diffuse
light now has a visible difference between the crown, sides and compressed gaps.

The V8 pile uses continuous length weighting so its outer layer is not removed
by a filtered texture-height threshold. Tuft coverage breaks up the outer shell;
curved sparse flyaways and a capped screen-space ribbon width support its edge.
The wider fibers and pile remain transparent. Shader sources are under
`Packages/com.weavekit.runtime/Runtime/Resources/WeaveKit/Wool`. The height target adds a full-board ARGBHalf
texture and a depth buffer: profile GPU time and memory on target devices.

The `--wool-swatch` acceptance option captures a deterministic neutral 12x12
fixture with exposed edges, an inset and a color boundary. Four switch states
share the same geometry: disabled effects, contact shadows, shadows plus fuzz,
and balanced final lighting. The fit/normal/close raw outputs let reviewers
separate visible changes from geometry or palette changes. This fixture has no
gameplay coverage. `--appearance-stage 0..3` and `--neutral-material` also work
with the existing opt-in material rendering fixture.

V9 separates horizontal stitch spacing from vertical course spacing: each logical
cell has 3 columns and 4 rows (12 stitches). Row pitch decreases from 1/3 to 1/4,
shortening the course by 25%. Paired legs approach each other sooner and the yarn
radius increases moderately to close the large triangular openings. Coordinate
drift, asymmetry, actual geometry contact shadows and short pile remain active.
The shared V8 shaders are unchanged; this revision adjusts geometry and dyes.
At 16 cells per chunk, body meshes remain below the 16-bit vertex limit.

`CozyYarnPalette` maps the recovered 13 color IDs of level_326863 to warm apricot,
dusty rose, butter cream, ivory, blue gray, cocoa, sage, moss, powder blue, denim,
and three muted purples. The board, supporting bed, spools and thread share the
same presentation lookup. This user-requested display redesign does not modify
the recovered level JSON or its fill groups. Other levels retain their source
palette with the existing dark-yarn lift. Acceptance checks the live UI and
thread colors against the board and bed, and records the 13 display hex colors.

V9 is currently built in the Windows player. Its dense geometry and render
texture require profiling before shipping on Android. Full-level playback,
reset screenshots, restored-board comparison and geometry close-ups are under
`Acceptance/2026-11-10/yarn-cozy-v9/runtime`; the accompanying report and
same-camera comparisons document the revision. Earlier evidence remains under
`Acceptance/2026-11-10/yarn-volume-v8`, `yarn-soft-v7`, `yarn-knit-v6`, `yarn-dense-v5`, `yarn-edge-v4` and `yarn-material-v3`.

The user-requested reference material is active for `level_326863`: dense soft
knit with 3x4 compact stitches per filled logical cell, matte crevice shading,
fine fibers and a soft outer shadow. Blank knit, spool wraps and the connecting
strand retain the earlier reference-inspired textures. It is a designed material variant
based on the supplied picture, rather than a recovered original-game shader.
Palette IDs, color definitions, cell layout and gameplay rules remain in the
unchanged level JSON. A small display-only dark-color lift keeps black yarn's
fibers visible.

`aigc/yarn-knit-reference-v1.png` is the neutral AI-generated source swatch.
`Tools/BakeYarnMaterial.py` combines a periodic V-loop height field with the
generated cotton-fiber roughness, bakes the level atlas, and creates strand/spool textures under
`Assets/Resources/YarnMaterialsV1`. Run it with Python/Pillow/NumPy to regenerate
the assets. `Assets/Editor/YarnMaterialImport.cs` enables uncompressed 4K textures
and trilinear mipmaps for both fit and zoom views. Other levels retain the
original art fallback. Runtime images and checks are recorded separately under
`Acceptance/2026-11-09/yarn-material-v1`.

The runtime includes all 103 unique levels captured from the APK. Each
`Assets/Resources/Levels/level_<id>.json` uses the small `finished_url` PNG as
its authoritative pixel map: alpha >= 100, D65 Lab delta-E <= 2, and palette
groups ordered by first pixel occurrence. The larger `i512_url` art is kept as
local presentation art under `Assets/Resources/LevelImages/Local`.

Use **选关** in the top bar to open the offline level picker. A desktop player
can also start directly with `--level-id 326861` (or `--level-id=level_326861`).
The generated `Assets/Resources/Levels/level_catalog.json` is the runtime
catalog, while `Assets/Original/recovered_level_manifest.json` records each
source hash, dimensions, palette count and recovered resource path. The
`level_0001.json` fixture remains available for smoke tests.

The recovered local level catalog is stored in `Assets/Original/level_catalog.json`
and the imported source/finished images are under
`Assets/Resources/LevelImages/Local`. This gives the next vertical slice access
to the local level set without any network dependency.

## Original assets

`Tools/ExtractOriginalAssets.py` extracts game-facing resources from `base.apk`.
The extracted source files are kept under `Assets/Original/Raw`; converted textures
and the runtime audio/texture subset are under `Assets/Resources`.

The original Lottie files are preserved under `Assets/Original/Raw` and decoded
JSONs under `Assets/Original/LottieJson`. They are **not played by this runtime**.
The current completion burst is a custom placeholder using original textures;
reusing textures does not make its animation or timing match the original.
The unfilled grayscale texture and reconstructed spool layout are approximations.
The current shaded thread, eight-neighbour traversal presentation, focus jump,
and completion burst are approximations. The original GLES yarn shader, exact
route/timing, camera transitions and completion sequence remain visual acceptance
blockers. Tintable mask derivatives preserve original alpha silhouettes while
converting their RGB to white; regenerate them with `Tools/PrepareUiMasks.py`.
The unsupported +10 gem reward was removed.

## Visual quality acceptance

The final core level must match the original game's visual quality and behavior.
That includes the original textures and materials, yarn and cell rendering,
animations, particles and completion effects, UI layout and typography, colors,
camera framing, transitions, and audio/visual timing. The current runtime UI,
fixture level, and simplified completion burst are scaffolding only; they are not
the final visual-quality implementation.

Acceptance is based on side-by-side comparison against recovered runtime captures
and the evidence images under the sibling `YarnScapes_Project/compare/evidence/S2`
folder when those files are available.

## Current evidence boundary

The APK set exposes Android/Compose classes and `com.gamebase.yarnscapescore` view
classes, but it does not expose `assets/bin/Data`, `libil2cpp.so`,
`global-metadata.dat`, or `libunity.so`. The pixel maps and color-analysis rules
are recoverable from PNGs and Android code; the absence of Unity data is not a
reason to substitute invented level values. Region membership now follows the
recovered eight-neighbour rule; matching the original traversal order, camera
movement and rendered thread remains separate work.

## Repeatable acceptance capture

Launch the Windows player with `-screen-fullscreen 0 -screen-width 540
-screen-height 960 --acceptance-output <absolute-output-directory>`. It saves
direct GPU frames and `interaction_report.json`, then exits. It raycasts the UI
to select all spools, zooms and pans, rejects a wrong color, weaves a region,
and then completes every remaining region through spool/cell pointer events.
It checks final completion, reset and unchanged persistent progress. This opt-in
mode starts with a clean in-memory board and never writes progress. Append
`--acceptance-smoke` to stop after the first region and reset; use that for quick
layout checks. The reports explicitly distinguish full playthrough from smoke
coverage, and do not claim original visual parity or physical touch testing.

`Tools/AuditLevel.py --source <original-small-PNG> --level <level-JSON> --output
<report-JSON>` checks cell IDs and group colors against recovered analysis rules.
It shares the recovered analyzer with the importer; matching the original
runtime's analysis cache is still a separate check.
