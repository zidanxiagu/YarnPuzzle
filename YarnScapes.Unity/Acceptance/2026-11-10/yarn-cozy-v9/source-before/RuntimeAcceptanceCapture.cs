using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YarnScapes.Core;
using YarnScapes.Infrastructure;

namespace YarnScapes.Presentation
{
    // Opt-in player acceptance uses the same UGUI raycasts and event handlers as
    // pointer input. It never reads or writes the user's saved progress.
    public sealed class RuntimeAcceptanceCapture : MonoBehaviour
    {
        private string outputDirectory;
        private readonly Report report = new Report();
        private PixelBoardRenderer board;
        private BoardViewportInput viewport;
        private LevelDefinition definition;
        private string progressPath, progressBefore;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] != "--acceptance-output") continue;
                var capture = new GameObject("AcceptanceCapture").AddComponent<RuntimeAcceptanceCapture>();
                capture.outputDirectory = Path.GetFullPath(args[i + 1]);
                return;
            }
        }
        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            Directory.CreateDirectory(outputDirectory);
            progressPath = Path.Combine(Application.persistentDataPath, "yarnscapes_progress.json");
            progressBefore = File.Exists(progressPath) ? File.ReadAllText(progressPath) : null;
            yield return new WaitForSecondsRealtime(2);
            Canvas.ForceUpdateCanvases();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--wool-swatch")>=0)
            {
                yield return CaptureWoolSwatch();
                WriteReport();Application.Quit(report.failed==0?0:2);yield break;
            }
            board = FindObjectOfType<PixelBoardRenderer>();
            viewport = FindObjectOfType<BoardViewportInput>();
            definition = new ResourcesLevelRepository().Load("level_326863");
            Check("runtime_started", board != null && viewport != null);
            Check("reference_yarn_material_loaded", board.UsesReferenceYarnMaterial);
            Check("plush_3d_geometry_active", board.UsesPlushGeometry);
            var plushDiagnostic=FindObjectOfType<PlushYarnBoard>();
            var stageArg=AppearanceStage();
            if(plushDiagnostic!=null)
            {
                plushDiagnostic.SetAppearanceStage(stageArg);
                plushDiagnostic.SetNeutralDiagnostic(Array.IndexOf(Environment.GetCommandLineArgs(),"--neutral-material")>=0);
            }
            report.screenWidth = Screen.width; report.screenHeight = Screen.height;
            report.paintableCells = board.TotalCount;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--material-preview")>=0)
            {
                // Rendering fixture only. This is never counted as a played level.
                report.method="Material rendering fixture via RestoreProgress; no gameplay coverage";
                var indices=new List<int>();
                for(var j=0;j<definition.cells.Length;j++) if(definition.cells[j]>=0) indices.Add(j);
                board.RestoreProgress(indices.ToArray());
                viewport.Fit();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame(); Capture("06_finished_art.png");
                viewport.ZoomAt(3.2f,Center(board.Rect));
                yield return new WaitForEndOfFrame(); Capture("07_yarn_material_closeup.png");
                var previewPlush=FindObjectOfType<PlushYarnBoard>();
                previewPlush.SaveGeometryProof(Path.Combine(outputDirectory,"08_geometry_oblique.png"));
                previewPlush.SaveGeometryProof(Path.Combine(outputDirectory,"10_fiber_edge.png"),new Vector2(-18,7),2);
                previewPlush.SaveGeometryProof(Path.Combine(outputDirectory,"11_outer_edge.png"),new Vector2(-20,5.5f),2);
                report.plushVertices=previewPlush.VertexCount;
                report.boundaryFibers=previewPlush.BoundaryFiberCount;
                report.stitchVariation=previewPlush.MaxStitchVariation;
                report.stitchesPerCell=previewPlush.StitchesPerCell;
                RecordGeometryMetrics(previewPlush);
                WriteReport();Application.Quit(report.failed==0?0:2);yield break;
            }
            yield return new WaitForEndOfFrame(); Capture("01_fit.png");
            Check("plus_button_hit", Click(Center(GameObject.Find("ZoomIn").GetComponent<RectTransform>())));
            Check("plus_zoom", viewport.Zoom > 1);
            var center = Center((RectTransform)viewport.transform);
            var oldZoom = viewport.Zoom;
            var scroll = Pointer(center); scroll.scrollDelta = new Vector2(0, 3);
            Dispatch(scroll, ExecuteEvents.scrollHandler);
            Check("wheel_zoom", viewport.Zoom > oldZoom);
            var beforeMinus = viewport.Zoom;
            Click(Center(GameObject.Find("ZoomOut").GetComponent<RectTransform>()));
            Check("minus_button_zoom", viewport.Zoom < beforeMinus);
            var oldPan = viewport.Pan;
            var drag = Pointer(center);
            Dispatch(drag, ExecuteEvents.pointerDownHandler);
            Dispatch(drag, ExecuteEvents.beginDragHandler);
            drag.delta = new Vector2(-45, -55); drag.position += drag.delta;
            Dispatch(drag, ExecuteEvents.dragHandler);
            Dispatch(drag, ExecuteEvents.endDragHandler);
            Dispatch(drag, ExecuteEvents.pointerClickHandler);
            Check("drag_pan", Vector2.Distance(viewport.Pan, oldPan) > 1);
            Check("drag_does_not_fill", board.FilledCount == 0);
            Click(Center(GameObject.Find("FitBoard").GetComponent<RectTransform>()));
            Check("fit_button", Mathf.Approximately(viewport.Zoom, 1) && viewport.Pan.sqrMagnitude < .01f);
            // Reach every spool through its ScrollRect, then raycast its visible centre.
            var palette = GameObject.Find("Palette").GetComponent<ScrollRect>();
            var allSpools = true;
            for (var i = 0; i < definition.palette.Length; i++)
            {
                palette.horizontalNormalizedPosition = i / (float)(definition.palette.Length - 1);
                Canvas.ForceUpdateCanvases();
                var hit = Click(Center(GameObject.Find("Color_" + (i + 1)).GetComponent<RectTransform>()));
                allSpools &= hit && board.SelectedColor == i;
                yield return null;
            }
            Check("all_13_spools_select_via_raycast", allSpools);
            palette.horizontalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
            Click(Center(GameObject.Find("Color_1").GetComponent<RectTransform>()));
            Check("select_auto_focus", viewport.Zoom >= 3 && board.SelectedColor == 0);
            yield return new WaitForEndOfFrame(); Capture("02_selected_zoom.png");
            var seed = board.FindUnfilled(0);
            var expected = Connected(seed, 0);
            Check("wrong_color_rejected", !board.TryFill(seed, 1) && board.FilledCount == 0);
            var correctPoint = RectTransformUtility.WorldToScreenPoint(null, board.CellWorldPosition(seed));
            Check("cell_pointer_hit", Click(correctPoint));
            var selectedDuringFill = board.SelectedColor;
            Click(Center(GameObject.Find("Color_2").GetComponent<RectTransform>()));
            Check("color_locked_during_fill", board.SelectedColor == selectedDuringFill);
            yield return new WaitForSecondsRealtime(.18f);
            Check("thread_visible_during_fill", FindObjectOfType<YarnThreadGraphic>().IsVisible);
            yield return new WaitForEndOfFrame(); Capture("03_thread_in_progress.png");
            while (board.IsFilling) yield return null;
            report.firstRegionExpected = expected; report.firstRegionActual = board.FilledCount;
            Check("connected_region_count", board.FilledCount == expected && expected > 1);
            yield return new WaitForEndOfFrame(); Capture("04_region_filled.png");
            // Complete all remaining regions by raycasting spools and cells, with
            // no direct RestoreProgress or TryFill for successful fill operations.
            var regionCount = 1;
            var smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--acceptance-smoke") >= 0;
            for (var color = 0; color < definition.palette.Length && !smoke; color++)
            {
                var safety = definition.cells.Length;
                while (board.Remaining(color) > 0 && safety-- > 0)
                {
                    palette.horizontalNormalizedPosition = color / (float)(definition.palette.Length - 1);
                    Canvas.ForceUpdateCanvases();
                    Click(Center(GameObject.Find("Color_" + (color + 1)).GetComponent<RectTransform>()));
                    var index = board.FindUnfilled(color);
                    var previousCount = board.FilledCount;
                    var componentSize = Connected(index, color);
                    var started = Click(RectTransformUtility.WorldToScreenPoint(null, board.CellWorldPosition(index)));
                    if (!started || !board.IsFilling) { Check("region_click_starts_" + regionCount, false); break; }
                    while (board.IsFilling) yield return null;
                    Check("region_count_" + regionCount, board.FilledCount - previousCount == componentSize);
                    regionCount++;
                    if (regionCount % 8 == 0) WriteReport();
                }
            }
            report.regionsPlayed = regionCount;
            report.filledCells = board.FilledCount;
            report.fullPlaythrough = !smoke;
            if (!smoke)
            {
            Check("full_level_completed_by_pointer_events", board.FilledCount == board.TotalCount);
            yield return new WaitForSecondsRealtime(.7f);
            Check("completion_overlay_shown", GameObject.Find("CompletionOverlay") != null);
            yield return new WaitForEndOfFrame(); Capture("05_completion.png");
            var overlay = GameObject.Find("CompletionOverlay");
            if (overlay != null) Click(Center(overlay.GetComponentInChildren<Button>().GetComponent<RectTransform>()));
            yield return new WaitForSecondsRealtime(.8f);
            viewport.Fit();
            yield return new WaitForEndOfFrame(); Capture("06_finished_art.png");
            viewport.ZoomAt(3.2f, Center(board.Rect));
            yield return new WaitForEndOfFrame(); Capture("07_yarn_material_closeup.png");
            var plush = FindObjectOfType<PlushYarnBoard>();
            if (plush != null)
            {
                Check("geometry_contact_shadow_field_active",plush.HasContactShadows);
                Check("soft_fiber_coat_active", plush.HasSoftFiberCoat);
                Check("neighbor_aware_recessed_bed_active",plush.HasRecessedBed);
                report.plushVertices = plush.VertexCount;
                report.boundaryFibers=plush.BoundaryFiberCount;
                report.stitchVariation=plush.MaxStitchVariation;
                report.stitchesPerCell=plush.StitchesPerCell;
                RecordGeometryMetrics(plush);
                Check("geometry_cells_match_progress", plush.CellCount == board.FilledCount);
                plush.SaveGeometryProof(Path.Combine(outputDirectory,"08_geometry_oblique.png"));
                plush.SaveGeometryProof(Path.Combine(outputDirectory,"10_fiber_edge.png"),new Vector2(-18,7),2);
                plush.SaveGeometryProof(Path.Combine(outputDirectory,"11_outer_edge.png"),new Vector2(-20,5.5f),2);
                plush.SaveHeightDiagnostic(Path.Combine(outputDirectory,"13_geometry_height.png"));
            }
            viewport.Fit();
            }
            Click(Center(GameObject.Find("ResetLevel").GetComponent<RectTransform>()));
            Check("reset_clears_board_and_thread", board.FilledCount == 0 && !board.IsFilling && !FindObjectOfType<YarnThreadGraphic>().IsVisible);
            Check("reset_fits_board", Mathf.Approximately(viewport.Zoom, 1));
            Click(Center(GameObject.Find("SaveProgress").GetComponent<RectTransform>()));
            yield return new WaitForEndOfFrame(); Capture("09_reset.png");
            var resetPlush = FindObjectOfType<PlushYarnBoard>();
            Check("reset_clears_geometry", resetPlush == null || resetPlush.CellCount == 0);
            if(!smoke && resetPlush!=null)
            {
                // Saved-load order differs from the pointer-play order. Compare
                // the raw material camera afterwards, without altering storage.
                var restore=new List<int>();
                for(var j=0;j<definition.cells.Length;j++)if(definition.cells[j]>=0)restore.Add(j);
                board.RestoreProgress(restore.ToArray());
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                Check("restored_geometry_matches_count",resetPlush.CellCount==report.filledCells);
                // Match the target resolution used for the first proof.
                viewport.ZoomAt(3.2f,Center(board.Rect));
                yield return new WaitForEndOfFrame();
                resetPlush.SaveGeometryProof(Path.Combine(outputDirectory,"12_restored_geometry.png"));
                Click(Center(GameObject.Find("ResetLevel").GetComponent<RectTransform>()));
                yield return new WaitForEndOfFrame();
            }
            Check("progress_file_unchanged", progressBefore == (File.Exists(progressPath) ? File.ReadAllText(progressPath) : null));
            WriteReport();
            Debug.Log("INTERACTION_ACCEPTANCE " + (report.failed == 0 ? "PASS" : "FAIL") + " checks=" + report.checks.Count + " filled=" + report.filledCells);
            Application.Quit(report.failed == 0 ? 0 : 2);
        }
        private static int AppearanceStage()
        {
            var args=Environment.GetCommandLineArgs();
            for(var i=0;i+1<args.Length;i++) if(args[i]=="--appearance-stage" && int.TryParse(args[i+1],out var value)) return Mathf.Clamp(value,0,3);
            return 3;
        }
        private IEnumerator CaptureWoolSwatch()
        {
            report.method="Deterministic 12x12 neutral wool swatch; appearance diagnostics only, no gameplay coverage";
            var fixture=new LevelDefinition{levelId="wool-swatch",width=12,height=12,
                palette=new[]{new RgbaColor{r=.52f,g=.52f,b=.52f},new RgbaColor{r=.36f,g=.36f,b=.36f}},cells=new int[144]};
            for(var y=0;y<12;y++)for(var x=0;x<12;x++)
                fixture.cells[y*12+x]=(x<2||x>9||y<2||y>9||(x>7&&y<4))?-1:(x>=6&&y>=6?1:0);
            var swatchObject=new GameObject("NeutralWoolSwatch",typeof(RectTransform));
            var swatch=swatchObject.AddComponent<PlushYarnBoard>();
            Check("swatch_initializes",swatch.Initialize(fixture));
            swatch.SetNeutralDiagnostic(true);
            for(var j=0;j<fixture.cells.Length;j++)if(fixture.cells[j]>=0)swatch.Fill(j,fixture.palette[fixture.cells[j]].ToUnityColor(),false);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            for(var step=0;step<=3;step++)
            {
                swatch.SetAppearanceStage(step);
                swatch.SaveFrontProof(Path.Combine(outputDirectory,"stage"+step+"_fit.png"),6,96);
                swatch.SaveFrontProof(Path.Combine(outputDirectory,"stage"+step+"_normal.png"),6,312);
                swatch.SaveFrontProof(Path.Combine(outputDirectory,"stage"+step+"_close.png"),2.4f,960);
                if(step==3)swatch.SaveHeightDiagnostic(Path.Combine(outputDirectory,"geometry_height.png"));
            }
            Check("swatch_contact_field_allocated",swatch.HasContactShadows);
            report.plushVertices=swatch.VertexCount;report.stitchesPerCell=swatch.StitchesPerCell;
            Destroy(swatchObject);
        }
        private int Connected(int seed, int color)
        {
            var done = new HashSet<int>(board.GetFilledIndices());
            var found = new HashSet<int> { seed };
            var queue = new Queue<int>(); queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var x = current % definition.width + dx; var y = current / definition.width + dy;
                    if (x < 0 || y < 0 || x >= definition.width || y >= definition.height) continue;
                    var next = y * definition.width + x;
                    if (definition.cells[next] == color && !done.Contains(next) && found.Add(next)) queue.Enqueue(next);
                }
            }
            return found.Count;
        }
        private void RecordGeometryMetrics(PlushYarnBoard plush)
        {
            report.geometryGenerationMilliseconds=plush.GenerationMilliseconds;
            report.meshUploadMilliseconds=plush.UploadMilliseconds;
            report.renderTextureWidth=plush.RenderTargetWidth;
            report.managedHeapBytesBeforeCollection=GC.GetTotalMemory(false);
            // Acceptance-only collection separates live data from the temporary
            // geometry lists freed when a completed chunk is uploaded.
            report.managedHeapBytes=GC.GetTotalMemory(true);
            report.unityAllocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            report.graphicsDevice=SystemInfo.graphicsDeviceName;
            report.graphicsApi=SystemInfo.graphicsDeviceType.ToString();
        }
        private PointerEventData Pointer(Vector2 point)
        {
            var e = new PointerEventData(EventSystem.current) { position = point, pressPosition = point, button = PointerEventData.InputButton.Left, clickCount = 1 };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, hits);
            if (hits.Count > 0) e.pointerCurrentRaycast = e.pointerPressRaycast = hits[0];
            return e;
        }
        private bool Click(Vector2 point)
        {
            var e = Pointer(point);
            Dispatch(e, ExecuteEvents.pointerDownHandler);
            Dispatch(e, ExecuteEvents.pointerUpHandler);
            return Dispatch(e, ExecuteEvents.pointerClickHandler);
        }
        private static bool Dispatch<T>(PointerEventData e, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
        {
            var hit = e.pointerCurrentRaycast.gameObject;
            return hit != null && ExecuteEvents.ExecuteHierarchy(hit, e, handler) != null;
        }
        private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        private void Check(string name, bool pass)
        {
            report.checks.Add(new CheckResult { name = name, pass = pass });
            if (!pass) { report.failed++; Debug.LogWarning("Acceptance failed: " + name); }
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error) { report.errors.Add(message); report.failed++; WriteReport(); } }
        private void WriteReport() => File.WriteAllText(Path.Combine(outputDirectory, "interaction_report.json"), JsonUtility.ToJson(report, true));
        private void Capture(string fileName)
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var pixels=texture.GetPixels32();
            var lo=765;var hi=0;
            foreach(var pixel in pixels)
            {var sum=pixel.r+pixel.g+pixel.b;lo=Math.Min(lo,sum);hi=Math.Max(hi,sum);}
            Check("nonblank_capture_"+fileName,hi-lo>30);
            File.WriteAllBytes(Path.Combine(outputDirectory, fileName), texture.EncodeToPNG()); Destroy(texture);
        }
        [Serializable] private sealed class Report
        {
            public string method = "UGUI raycast and pointer event playback in a rendered Windows player";
            public int screenWidth, screenHeight, paintableCells, filledCells, firstRegionExpected, firstRegionActual, regionsPlayed, failed;
            public int plushVertices;
            public int boundaryFibers;
            public int stitchesPerCell;
            public float stitchVariation;
            public double geometryGenerationMilliseconds,meshUploadMilliseconds;
            public long managedHeapBytes,managedHeapBytesBeforeCollection,unityAllocatedBytes;
            public int renderTextureWidth;
            public string graphicsDevice,graphicsApi;
            public bool writesProgress = false;
            public bool fullPlaythrough;
            public bool originalVisualParityVerified = false;
            public bool physicalTouchPinchVerified = false;
            public List<CheckResult> checks = new List<CheckResult>();
            public List<string> errors = new List<string>();
        }
        [Serializable] private sealed class CheckResult { public string name; public bool pass; }
    }
}
