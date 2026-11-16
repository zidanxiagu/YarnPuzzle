using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YarnScapes.Core;
using YarnScapes.Infrastructure;
using YarnScapes.Presentation;
using WeaveKit.Unity;

public sealed class YarnScapesRuntime : MonoBehaviour
{
    private const string FirstLevelId = "level_326863";
    private static readonly Color PageBackground = new Color(0.93f, 0.93f, 0.93f, 1f);
    private static readonly Color PanelBackground = new Color(0.98f, 0.98f, 0.98f, 1f);
    private static readonly Color Ink = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color MutedInk = new Color(0.36f, 0.39f, 0.44f, 1f);

    private ResourcesLevelRepository levelRepository;
    private LocalProgressStore progressStore;
    private PixelBoardRenderer board;
    private LevelDefinition level;
    private Text progressText;
    private Text statusText;
    private Text selectedColorText;
    private Text titleText;
    private GameObject completionOverlay;
    private Transform canvasRoot;
    private int selectedColor;
    private AudioSource audioSource;
    private Font rubikRegular;
    private Font rubikMedium;
    private Font rubikBold;
    private BoardViewportInput viewportInput;
    private YarnThreadGraphic thread;
    private CompletionBurst completionBurst;
    private readonly System.Collections.Generic.List<Sprite> ownedSprites = new System.Collections.Generic.List<Sprite>();
    private Button[] paletteButtons;
    private Text[] paletteLabels;
    private ScrollRect paletteScroll;
    private float lastStitchSound;
    public static bool AcceptanceMode => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--acceptance-output") >= 0;

    private void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        levelRepository = new ResourcesLevelRepository();
        progressStore = new LocalProgressStore();
        level = levelRepository.Load(FirstLevelId);
        if (level == null)
        {
            level = levelRepository.Load("level_0001");
        }
        if (level == null)
        {
            return;
        }

        rubikRegular = Resources.Load<Font>("Fonts/Rubik-Regular");
        rubikMedium = Resources.Load<Font>("Fonts/Rubik-Medium");
        rubikBold = Resources.Load<Font>("Fonts/Rubik-Bold");
        BuildInterface();
    }

    private void BuildInterface()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var backdrop = CreatePanel(canvasObject.transform, "WindowBackground", PageBackground);
        Stretch(backdrop.rectTransform);
        var frame = new GameObject("PortraitFrame", typeof(RectTransform));
        frame.transform.SetParent(canvasObject.transform, false);
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(.5f, .5f);
        frameRect.sizeDelta = new Vector2(1080, 1920);
        canvasRoot = frame.transform;

        var background = CreatePanel(canvasRoot, "Background", PageBackground);
        Stretch(background.rectTransform);
        ApplyTiledBackground(background);

        var topBar = CreatePanel(canvasRoot, "TopBar", PanelBackground);
        Anchor(topBar.rectTransform, new Vector2(0f, 0.91f), Vector2.one, Vector2.zero, Vector2.zero);
        titleText = CreateText(topBar.transform, level.displayName, 31, TextAnchor.MiddleLeft, Ink, rubikBold);
        Anchor(titleText.rectTransform, new Vector2(0.065f, 0.14f), new Vector2(0.78f, 0.86f), Vector2.zero, Vector2.zero);
        progressText = CreateText(topBar.transform, "0%", 27, TextAnchor.MiddleRight, MutedInk, rubikMedium);
        Anchor(progressText.rectTransform, new Vector2(0.78f, 0.14f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);

        var boardPanel = CreatePanel(canvasRoot, "BoardPanel", new Color(1f, 1f, 1f, 0.82f));
        Anchor(boardPanel.rectTransform, new Vector2(0.035f, 0.275f), new Vector2(0.965f, 0.86f), Vector2.zero, Vector2.zero);
        boardPanel.gameObject.AddComponent<RectMask2D>();
        var boardObject = new GameObject("PixelBoard", typeof(RectTransform), typeof(PixelBoardRenderer));
        boardObject.transform.SetParent(boardPanel.transform, false);
        board = boardObject.GetComponent<PixelBoardRenderer>();
        var yarnTexture = Resources.Load<Texture2D>("Textures/yarn_piece");
        var yarnSprite = CreateSprite(yarnTexture);
        var artTexture = Resources.Load<Texture2D>(level.artImageResource);
        board.Build(YarnScapesComposition.Grid(level), YarnScapesComposition.BoardSettings(level, yarnSprite, artTexture), HandleCellClicked);
        viewportInput = boardPanel.gameObject.AddComponent<BoardViewportInput>();
        viewportInput.Initialize(board);
        var saved = progressStore.Get(level.levelId);
        if (!AcceptanceMode && saved != null && saved.filledIndices != null)
        {
            board.RestoreProgress(saved.filledIndices);
        }

        var palettePanel = CreatePanel(canvasRoot, "Palette", PanelBackground);
        Anchor(palettePanel.rectTransform, new Vector2(0.035f, 0.08f), new Vector2(0.965f, 0.235f), Vector2.zero, Vector2.zero);
        palettePanel.gameObject.AddComponent<RectMask2D>();
        var paletteContent = new GameObject("PaletteContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        paletteContent.transform.SetParent(palettePanel.transform, false);
        var contentRect = paletteContent.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.sizeDelta = Vector2.zero;
        var paletteLayout = paletteContent.GetComponent<HorizontalLayoutGroup>();
        paletteLayout.spacing = 10f;
        paletteLayout.padding = new RectOffset(18, 18, 10, 10);
        paletteLayout.childAlignment = TextAnchor.MiddleCenter;
        paletteLayout.childForceExpandWidth = false;
        paletteLayout.childForceExpandHeight = false;
        paletteContent.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = palettePanel.gameObject.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = palettePanel.rectTransform;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        paletteScroll = scroll;
        paletteButtons = new Button[level.palette.Length];
        paletteLabels = new Text[level.palette.Length];
        for (var i = 0; i < level.palette.Length; i++)
        {
            CreatePaletteButton(paletteContent.transform, i);
        }
        board.SelectColor(selectedColor);

        selectedColorText = CreateText(canvasRoot, "颜色 1", 22, TextAnchor.MiddleCenter, MutedInk, rubikMedium);
        Anchor(selectedColorText.rectTransform, new Vector2(0.2f, 0.045f), new Vector2(0.8f, 0.085f), Vector2.zero, Vector2.zero);
        statusText = CreateText(canvasRoot, "先选线团，再点高亮格 · 滚轮/双指缩放 · 拖动画布", 19, TextAnchor.MiddleCenter, MutedInk, rubikRegular);
        Anchor(statusText.rectTransform, new Vector2(0.08f, 0.245f), new Vector2(0.92f, 0.27f), Vector2.zero, Vector2.zero);

        var resetButton = CreateButton(canvasRoot, "重置", 20, new Color(1f, 1f, 1f, 0.9f), Ink);
        resetButton.name = "ResetLevel";
        Anchor(resetButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.012f), new Vector2(0.31f, 0.052f), Vector2.zero, Vector2.zero);
        resetButton.onClick.AddListener(ResetLevel);
        var saveButton = CreateButton(canvasRoot, "保存", 20, new Color(1f, 1f, 1f, 0.9f), Ink);
        saveButton.name = "SaveProgress";
        Anchor(saveButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0.012f), new Vector2(0.92f, 0.052f), Vector2.zero, Vector2.zero);
        saveButton.onClick.AddListener(SaveProgress);

        var zoomOut = CreateButton(canvasRoot, "−", 32, Color.white, Ink);
        zoomOut.name = "ZoomOut";
        Anchor(zoomOut.GetComponent<RectTransform>(), new Vector2(.60f, .865f), new Vector2(.70f, .905f), Vector2.zero, Vector2.zero);
        zoomOut.onClick.AddListener(() => viewportInput.ZoomBy(1 / 1.3f));
        var zoomIn = CreateButton(canvasRoot, "+", 32, Color.white, Ink);
        zoomIn.name = "ZoomIn";
        Anchor(zoomIn.GetComponent<RectTransform>(), new Vector2(.72f, .865f), new Vector2(.82f, .905f), Vector2.zero, Vector2.zero);
        zoomIn.onClick.AddListener(() => viewportInput.ZoomBy(1.3f));
        var fit = CreateButton(canvasRoot, "全图", 22, Color.white, Ink);
        fit.name = "FitBoard";
        Anchor(fit.GetComponent<RectTransform>(), new Vector2(.84f, .865f), new Vector2(.965f, .905f), Vector2.zero, Vector2.zero);
        fit.onClick.AddListener(viewportInput.Fit);

        var threadObject = new GameObject("YarnThread", typeof(RectTransform), typeof(YarnThreadGraphic));
        threadObject.transform.SetParent(canvasRoot, false);
        Stretch(threadObject.GetComponent<RectTransform>());
        thread = threadObject.GetComponent<YarnThreadGraphic>();
        var sparkleSprite = CreateSprite(Resources.Load<Texture2D>("Textures/pixel_sparkle_tintable"));
        thread.Initialize(board, sparkleSprite, Resources.Load<Texture2D>("WeaveKit/thread_strip"));
        completionBurst = gameObject.AddComponent<CompletionBurst>();
        completionBurst.Initialize((RectTransform)canvasRoot, CreateSprite(Resources.Load<Texture2D>("Textures/pixel_style_mask_tintable")), sparkleSprite);
        board.StitchStarted += index => thread.Begin(paletteButtons[selectedColor].GetComponent<RectTransform>(), index, board.GetColor(selectedColor));
        board.StitchReached += index =>
        {
            thread.Reach(index);
            if (Time.unscaledTime - lastStitchSound > .11f) { Play("thread_middle"); lastStitchSound = Time.unscaledTime; }
        };
        board.FillEnded += HandleFillEnded;
        board.FillCancelled += () => { if (thread != null) thread.Clear(); };

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        var music = Resources.Load<AudioClip>("Audio/background_music");
        if (music != null)
        {
            audioSource.clip = music;
            audioSource.volume = 0.14f;
            audioSource.Play();
        }
        UpdateProgressText();
        RefreshPalette();
    }

    private void Update()
    {
        if (board != null && board.IsFilling) UpdateProgressText();
    }

    private void ApplyTiledBackground(Image background)
    {
        var texture = Resources.Load<Texture2D>("Textures/play_background_tile_texture");
        if (texture == null)
        {
            return;
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        ownedSprites.Add(sprite);
        background.sprite = sprite;
        background.type = Image.Type.Tiled;
        background.color = new Color(1f, 1f, 1f, 0.32f);
    }

    private void CreatePaletteButton(Transform parent, int index)
    {
        var button = CreateButton(parent, string.Empty, 20, Color.white, Color.white);
        button.name = "Color_" + (index + 1);
        paletteButtons[index] = button;
        button.transition = Selectable.Transition.None;
        button.GetComponent<Image>().color = Color.clear;
        var element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 140f;
        element.preferredHeight = 210f;
        AddSpoolPart(button.transform, "spool_bottom", new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.46f), Color.white);
        AddSpoolPart(button.transform, "spool_stick", new Vector2(0.36f, 0.35f), new Vector2(0.64f, 0.83f), Color.white);
        var wrapTexture = Resources.Load<Texture2D>("YarnMaterialsV2/spool_wrap") ?? Resources.Load<Texture2D>("YarnMaterialsV1/spool_wrap");
        if (wrapTexture != null)
        {
            var wrap = CreatePanel(button.transform, "SoftYarnWrap", board.GetColor(index));
            wrap.sprite = Sprite.Create(wrapTexture, new Rect(0, 0, wrapTexture.width, wrapTexture.height), new Vector2(.5f, .5f), 100);
            ownedSprites.Add(wrap.sprite);
            wrap.raycastTarget = false;
            Anchor(wrap.rectTransform, new Vector2(.10f, .25f), new Vector2(.90f, .86f), Vector2.zero, Vector2.zero);
        }
        else for (var row = 0; row < 9; row++)
        {
            var y = 0.26f + row * 0.047f;
            AddSpoolPart(button.transform, "yarn_piece", new Vector2(0.13f, y), new Vector2(0.87f, y + 0.22f), board.GetColor(index));
        }
        AddSpoolPart(button.transform, "spool_top", new Vector2(0.1f, 0.69f), new Vector2(0.9f, 0.99f), Color.white);
        var number = CreateText(button.transform, (index + 1).ToString(), 26, TextAnchor.MiddleCenter, Ink, rubikBold);
        paletteLabels[index] = number;
        Anchor(number.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.2f), Vector2.zero, Vector2.zero);
        button.onClick.AddListener(() =>
        {
            SelectColor(index, true);
        });
    }

    private void SelectColor(int index, bool focus)
    {
        if (board.IsFilling) return;
        selectedColor = index;
        board.SelectColor(index);
        if (focus) viewportInput.FocusColor(index);
        RefreshPalette();
        EnsureSelectedSpoolVisible();
        statusText.text = board.Remaining(index) > 0 ? "点击深色数字格，线团会连续编织相连区域" : "这个颜色已完成，请选择其他线团";
        Play("thread_start");
    }

    private void RefreshPalette()
    {
        for (var i = 0; i < paletteButtons.Length; i++)
        {
            var remaining = board.Remaining(i);
            paletteButtons[i].GetComponent<Image>().color = i == selectedColor ? new Color(.78f, .86f, .95f, .65f) : Color.clear;
            paletteLabels[i].text = remaining == 0 ? "✓" : (i + 1).ToString();
            paletteLabels[i].color = i == selectedColor ? new Color(.1f, .36f, .64f) : Ink;
            paletteButtons[i].interactable = !board.IsFilling && remaining > 0;
        }
        selectedColorText.text = "颜色 " + (selectedColor + 1) + " · 剩余 " + board.Remaining(selectedColor) + " 格";
    }

    private void EnsureSelectedSpoolVisible()
    {
        Canvas.ForceUpdateCanvases();
        var rect = paletteButtons[selectedColor].GetComponent<RectTransform>();
        var left = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x + paletteScroll.content.anchoredPosition.x;
        var right = left + rect.rect.width;
        var shift = left < 8 ? 8 - left : right > paletteScroll.viewport.rect.width - 8 ? paletteScroll.viewport.rect.width - 8 - right : 0;
        var position = paletteScroll.content.anchoredPosition;
        position.x = Mathf.Clamp(position.x + shift, Mathf.Min(0, paletteScroll.viewport.rect.width - paletteScroll.content.rect.width), 0);
        paletteScroll.content.anchoredPosition = position;
        paletteScroll.StopMovement();
    }

    private void AddSpoolPart(Transform parent, string resource, Vector2 min, Vector2 max, Color color)
    {
        var part = CreatePanel(parent, resource, color);
        part.sprite = CreateSprite(Resources.Load<Texture2D>("Textures/" + resource));
        part.preserveAspect = true;
        part.raycastTarget = false;
        Anchor(part.rectTransform, min, max, Vector2.zero, Vector2.zero);
    }

    private void HandleCellClicked(int index)
    {
        if (board.IsFilling) return;
        if (!board.TryFill(index, selectedColor))
        {
            statusText.text = "请先选择格子上的颜色";
            Play("cluster");
            return;
        }

        statusText.text = "正在编织…";
        RefreshPalette();
    }

    private void HandleFillEnded()
    {
        thread.End();
        Play("thread_end");
        UpdateProgressText();
        if (!AcceptanceMode) progressStore.SaveLevel(level, board.FilledCount, board.FilledCount == board.TotalCount, board.GetFilledIndices());
        RefreshPalette();
        if (board.FilledCount == board.TotalCount)
        {
            viewportInput.Fit();
            StartCoroutine(CompleteRoutine());
        }
        else if (board.Remaining(selectedColor) == 0)
        {
            for (var i = 1; i < level.palette.Length; i++)
            {
                var next = (selectedColor + i) % level.palette.Length;
                if (board.Remaining(next) > 0) { SelectColor(next, true); break; }
            }
        }
        else statusText.text = "继续点击同色高亮格，或选择其他线团";
    }

    private IEnumerator CompleteRoutine()
    {
        yield return new WaitForSecondsRealtime(level.completionDelaySeconds);
        if (!AcceptanceMode) progressStore.SaveLevel(level, board.FilledCount, true, board.GetFilledIndices());
        statusText.text = "全部编织完成";
        Play("sound_finished");
        completionBurst.Play();
        ShowCompletionOverlay();
    }

    private void ShowCompletionOverlay()
    {
        if (completionOverlay != null)
        {
            return;
        }

        completionOverlay = new GameObject("CompletionOverlay", typeof(RectTransform), typeof(Image));
        completionOverlay.transform.SetParent(canvasRoot, false);
        var image = completionOverlay.GetComponent<Image>();
        image.color = new Color(0.98f, 0.98f, 0.98f, 0.97f);
        Stretch(completionOverlay.GetComponent<RectTransform>());
        var label = CreateText(completionOverlay.transform, "完成！", 46, TextAnchor.MiddleCenter, Ink, rubikBold);
        Anchor(label.rectTransform, new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.72f), Vector2.zero, Vector2.zero);
        var close = CreateButton(completionOverlay.transform, "继续", 25, new Color(0.12f, 0.55f, 0.92f, 1f), Color.white);
        Anchor(close.GetComponent<RectTransform>(), new Vector2(0.3f, 0.34f), new Vector2(0.7f, 0.45f), Vector2.zero, Vector2.zero);
        close.onClick.AddListener(() =>
        {
            Destroy(completionOverlay);
            completionOverlay = null;
        });
    }

    private void ResetLevel()
    {
        StopAllCoroutines();
        completionBurst.Cancel();
        if (completionOverlay != null)
        {
            Destroy(completionOverlay);
            completionOverlay = null;
        }
        board.ResetBoard();
        thread.Clear();
        selectedColor = 0;
        board.SelectColor(0);
        viewportInput.Fit();
        paletteScroll.horizontalNormalizedPosition = 0;
        paletteScroll.StopMovement();
        RefreshPalette();
        statusText.text = "先选线团，再点高亮格 · 滚轮/双指缩放 · 拖动画布";
        if (!AcceptanceMode) progressStore.SaveLevel(level, 0, false, board.GetFilledIndices());
        UpdateProgressText();
    }

    private void SaveProgress()
    {
        if (!AcceptanceMode) progressStore.SaveLevel(level, board.FilledCount, board.FilledCount == board.TotalCount, board.GetFilledIndices());
        statusText.text = "进度已保存";
    }

    private void UpdateProgressText()
    {
        if (progressText != null && board != null)
        {
            var progress = board.TotalCount == 0 ? 0 : Mathf.RoundToInt(board.FilledCount * 100f / board.TotalCount);
            progressText.text = progress + "%";
        }
    }

    private void Play(string clipName)
    {
        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, 0.8f);
        }
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null)
        {
            return null;
        }
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        ownedSprites.Add(sprite);
        return sprite;
    }

    private void OnDestroy()
    {
        foreach (var sprite in ownedSprites) if (sprite != null) Destroy(sprite);
        ownedSprites.Clear();
    }

    private static Image CreatePanel(Transform parent, string name, Color color)
    {
        var objectInstance = new GameObject(name, typeof(RectTransform), typeof(Image));
        objectInstance.transform.SetParent(parent, false);
        objectInstance.GetComponent<Image>().color = color;
        return objectInstance.GetComponent<Image>();
    }

    private static Text CreateText(Transform parent, string value, int size, TextAnchor alignment, Color color, Font font)
    {
        var objectInstance = new GameObject("Text", typeof(RectTransform), typeof(Text));
        objectInstance.transform.SetParent(parent, false);
        var text = objectInstance.GetComponent<Text>();
        text.text = value;
        text.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(Transform parent, string caption, int size, Color backgroundColor, Color textColor)
    {
        var objectInstance = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        objectInstance.transform.SetParent(parent, false);
        var image = objectInstance.GetComponent<Image>();
        image.color = backgroundColor;
        var button = objectInstance.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = backgroundColor,
            highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.18f),
            pressedColor = Color.Lerp(backgroundColor, Color.black, 0.12f),
            selectedColor = backgroundColor,
            disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.35f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };
        if (!string.IsNullOrEmpty(caption))
        {
            var label = CreateText(objectInstance.transform, caption, size, TextAnchor.MiddleCenter, textColor, Resources.Load<Font>("Fonts/Rubik-Medium"));
            Stretch(label.rectTransform);
        }
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
