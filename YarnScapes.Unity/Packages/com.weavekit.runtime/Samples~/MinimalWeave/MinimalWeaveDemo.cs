using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeaveKit.Core;
using WeaveKit.Unity;

namespace WeaveKit.Samples
{
    /// <summary>Attach to an empty scene. Only WeaveKit + UGUI are required.</summary>
    public sealed class MinimalWeaveDemo : MonoBehaviour
    {
        private PixelBoardRenderer board;
        private BoardViewportInput viewport;
        private YarnThreadGraphic thread;
        private CompletionBurst burst;
        private RectTransform canvas;
        private RectTransform[] spools;
        private Sprite whiteSprite;
        private Text status;
        private int selected, completions;
        private int runtimeErrors;
        private readonly List<string> checks = new List<string>();
        private void Start()
        {
            Application.logMessageReceived += CaptureError;
            Application.targetFrameRate = 60;
            var cameraObject = new GameObject("BackgroundCamera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.96f, .95f, .92f);
            camera.cullingMask = 0;
            var root = new GameObject("WeaveKitSample", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = (RectTransform)root.transform;
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(640, 800);
            if (FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var panel = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            panel.transform.SetParent(canvas, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(.05f, .2f); panelRect.anchorMax = new Vector2(.95f, .9f);
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(1, 1, 1, .85f);
            var boardObject = new GameObject("Board", typeof(RectTransform), typeof(PixelBoardRenderer));
            boardObject.transform.SetParent(panelRect, false); board = boardObject.GetComponent<PixelBoardRenderer>();
            var cells = new int[48];
            for (var y = 0; y < 6; y++) for (var x = 0; x < 8; x++)
                cells[y * 8 + x] = (y == 0 || y == 5) && (x == 0 || x == 7) ? -1 : x < 4 ? 0 : 1;
            board.Build(new GridDefinition(8, 6, 2, cells), new BoardViewSettings
            {
                Width = 520, Palette = new[] { new Color(.69f, .79f, .68f), new Color(.92f, .69f, .51f) },
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                RouteStepSeconds = .008f, Wool = WeaveKitResources.LoadWool()
            }, index => board.TryFill(index, selected));
            viewport = panel.AddComponent<BoardViewportInput>(); viewport.Initialize(board);
            spools = new RectTransform[2];
            for (var i = 0; i < 2; i++)
            {
                var color = i;
                spools[i] = Button("Color " + (i + 1), new Vector2(.28f + .44f * i, .13f), board.GetColor(i), () => Select(color));
            }
            Button("Reset", new Vector2(.5f, .045f), Color.white, ResetBoard);
            status = Label("WeaveKit: select a color, then click its cells", new Vector2(.5f, .95f));
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * .5f);
            var threadObject = new GameObject("Thread", typeof(RectTransform), typeof(YarnThreadGraphic));
            threadObject.transform.SetParent(canvas, false);
            var threadRect = (RectTransform)threadObject.transform;
            threadRect.anchorMin = Vector2.zero; threadRect.anchorMax = Vector2.one; threadRect.offsetMin = threadRect.offsetMax = Vector2.zero;
            thread = threadObject.GetComponent<YarnThreadGraphic>();
            thread.Initialize(board, whiteSprite, WeaveKitResources.LoadThreadTexture());
            burst = gameObject.AddComponent<CompletionBurst>(); burst.Initialize(canvas, whiteSprite, whiteSprite);
            board.StitchStarted += index => thread.Begin(spools[selected], index, board.GetColor(selected));
            board.StitchReached += thread.Reach;
            board.FillCancelled += thread.Clear;
            board.FillEnded += () =>
            {
                completions++; thread.End();
                status.text = board.FilledCount + " / " + board.TotalCount;
                if (board.FilledCount == board.TotalCount) burst.Play();
            };
            board.SelectColor(0); Canvas.ForceUpdateCanvases(); viewport.Fit();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--weavekit-smoke") >= 0) StartCoroutine(Smoke());
        }
        private void Select(int color)
        {
            if (board.IsFilling) return;
            selected = color; board.SelectColor(color); viewport.FocusColor(color);
        }
        private void ResetBoard()
        {
            board.ResetBoard(); thread.Clear(); burst.Cancel(); viewport.Fit();
            status.text = "WeaveKit: select a color, then click its cells";
        }
        private RectTransform Button(string caption, Vector2 anchor, Color color, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(160, 42);
            go.GetComponent<Image>().color = color; go.GetComponent<Button>().onClick.AddListener(action);
            var text = Label(caption, Vector2.one * .5f); text.transform.SetParent(rect, false);
            return rect;
        }
        private Text Label(string value, Vector2 anchor)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(canvas, false);
            var label = go.GetComponent<Text>(); label.text = value; label.color = new Color(.2f, .24f, .25f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 18; label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false; label.rectTransform.anchorMin = label.rectTransform.anchorMax = anchor;
            label.rectTransform.sizeDelta = new Vector2(620, 42); return label;
        }
        private IEnumerator Smoke()
        {
            yield return new WaitForSecondsRealtime(.2f);
            Check("standalone_grid_without_project_data", board.TotalCount == 44);
            Check("bundled_wool_assets_render", board.UsesPlushGeometry);
            var seed = board.FindUnfilled(0);
            Check("start_region", board.TryFill(seed, 0));
            yield return new WaitForSecondsRealtime(.08f);
            Check("thread_receives_playback_events", thread.IsVisible && board.FilledCount > 0);
            board.ResetBoard(); thread.Clear();
            yield return new WaitForSecondsRealtime(.05f);
            Check("reset_cancels_without_completion", !board.IsFilling && board.FilledCount == 0 && completions == 0);
            board.TryFill(seed, 0); yield return null;
            board.gameObject.SetActive(false); yield return null;
            Check("disable_cancels_playback_handle", !board.IsFilling && !thread.IsVisible);
            board.gameObject.SetActive(true); board.ResetBoard(); thread.Clear();
            yield return null;
            Check("restart_after_disable", board.TryFill(seed, 0));
            while (board.IsFilling) yield return null;
            Check("region_completes_once", completions == 1 && board.FilledCount == 22);
            var partial = board.GetFilledIndices(); board.ResetBoard(); board.RestoreProgress(partial);
            Check("restore_rebuilds_visible_geometry", board.FilledCount == 22 && board.GetComponent<PlushYarnBoard>().CellCount == 22);
            ExecuteEvents.Execute(spools[1].gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Check("select_with_ui_event", board.SelectedColor == 1 && viewport.Zoom >= 3);
            var point = RectTransformUtility.WorldToScreenPoint(null, board.CellWorldPosition(board.FindUnfilled(1)));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            viewport.OnPointerDown(pointer); viewport.OnPointerClick(pointer);
            Check("viewport_click_starts_fill", board.IsFilling);
            while (board.IsFilling) yield return null;
            Check("two_color_completion", board.FilledCount == 44 && completions == 2 && burst.IsPlaying);
            burst.Cancel(); yield return null;
            Check("burst_cancel_removes_temporary_root", !burst.IsPlaying && canvas.Find("CompletionBurst") == null);
            burst.Play(); burst.enabled = false; yield return null;
            Check("burst_disable_cleans_up", !burst.IsPlaying && canvas.Find("CompletionBurst") == null);
            burst.enabled = true;
            viewport.Fit(); yield return new WaitForSecondsRealtime(.3f);
            var wool = board.GetComponent<PlushYarnBoard>();
            Check("surface_shadows_and_fibers_available", wool.HasContactShadows && wool.HasSoftFiberCoat && wool.BoundaryFiberCount > 0);

            // Cancellation from inside a callback must not let an old coroutine finish a new region.
            var clockObject = new GameObject("PlaybackReentrancyCheck", typeof(RegionPlayback));
            var clock = clockObject.GetComponent<RegionPlayback>();
            var model = new FillSession(new BoardState(new GridDefinition(2, 1, 1, new[] { 0, 0 })));
            clock.Initialize(model, .001f);
            var restarted = false; var finished = 0;
            clock.CellFilled += index => { if (!restarted) { restarted = true; clock.Cancel(); clock.TryPlay(1, 0); } };
            clock.Completed += () => finished++;
            clock.TryPlay(0, 0);
            yield return new WaitForSecondsRealtime(.15f);
            Check("callback_cancel_restart_is_isolated", finished == 1 && model.State.FilledCount == 2 && !clock.IsPlaying);
            Destroy(clockObject);

            var feedback = gameObject.AddComponent<UiFeedbackAnimator>();
            var tint = spools[0].GetComponent<Image>(); var originalColor = tint.color;
            var originalScale = spools[0].localScale;
            feedback.Pulse(spools[0], .4f); feedback.Flash(tint, originalColor, .4f);
            yield return new WaitForSecondsRealtime(.05f);
            Check("generic_feedback_animates", spools[0].localScale.x > originalScale.x && tint.color != originalColor);
            feedback.Cancel();
            Check("generic_feedback_cancel_restores_values", spools[0].localScale == originalScale && tint.color == originalColor);
            feedback.Pulse(spools[0], .05f); feedback.Flash(tint, originalColor, .05f);
            yield return new WaitForSecondsRealtime(.2f);
            Check("generic_feedback_finishes_at_rest", spools[0].localScale == originalScale && tint.color == originalColor);
            Destroy(feedback);

            // Removing only the component must release owned views/resources too.
            var disposable = new GameObject("DisposableBoard", typeof(RectTransform), typeof(PixelBoardRenderer));
            disposable.transform.SetParent(canvas, false);
            var disposableBoard = disposable.GetComponent<PixelBoardRenderer>();
            disposableBoard.Build(new GridDefinition(1, 1, 1, new[] { 0 }), new BoardViewSettings
            { Palette = new[] { Color.red }, Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") }, null);
            disposableBoard.RestoreProgress(new[] { 0 });
            Check("flat_board_works_without_wool_assets", !disposableBoard.UsesPlushGeometry && disposableBoard.FilledCount == 1);
            var disposableViewport = disposable.AddComponent<BoardViewportInput>(); disposableViewport.Initialize(disposableBoard);
            var observer = new GameObject("TargetLifetimeThread", typeof(RectTransform), typeof(YarnThreadGraphic));
            observer.transform.SetParent(canvas, false);
            var observerThread = observer.GetComponent<YarnThreadGraphic>(); observerThread.Initialize(disposableBoard, whiteSprite);
            observerThread.Begin(spools[0], 0, Color.white); observerThread.Reach(0);
            Destroy(disposableBoard); yield return null; yield return null;
            Check("component_removal_cleans_owned_children", disposable.transform.childCount == 0 && disposable.GetComponent<RegionPlayback>() == null);
            disposableViewport.ZoomBy(2); disposableViewport.FocusColor(0); disposableViewport.Fit();
            Check("destroyed_interface_targets_do_not_throw", !observerThread.IsVisible && disposableViewport.Pan == Vector2.zero && runtimeErrors == 0);
            Destroy(disposable); Destroy(observer);
            var output = OutputDirectory(); Directory.CreateDirectory(output);
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, "standalone-sample.png"), screenshot.EncodeToPNG()); Destroy(screenshot);
            File.WriteAllLines(Path.Combine(output, "smoke-checks.txt"), checks);
            Debug.Log("WEAVEKIT_STANDALONE PASS checks=" + checks.Count);
            Application.Quit(0);
        }
        private void Check(string name, bool passed)
        {
            checks.Add((passed ? "PASS " : "FAIL ") + name);
            if (passed) return;
            var output = OutputDirectory(); Directory.CreateDirectory(output);
            File.WriteAllLines(Path.Combine(output, "smoke-checks.txt"), checks);
            Debug.LogError("WEAVEKIT_STANDALONE FAIL " + name); Application.Quit(2);
            throw new InvalidOperationException(name);
        }
        private static string OutputDirectory()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++) if (args[i] == "--weavekit-output") return args[i + 1];
            return Path.Combine(Application.persistentDataPath, "WeaveKitSmoke");
        }
        private void CaptureError(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeErrors++; }
        private void OnDestroy()
        {
            Application.logMessageReceived -= CaptureError;
            if (whiteSprite != null) Destroy(whiteSprite);
        }
    }
}
