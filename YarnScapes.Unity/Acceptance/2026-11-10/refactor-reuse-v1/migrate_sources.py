from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[3]
PACKAGE = ROOT / 'Packages/com.weavekit.runtime'
UNITY = PACKAGE / 'Runtime/Unity'

def replace(text, before, after):
    assert before in text, before[:120]
    return text.replace(before, after)

def move(source, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    assert not target.exists(), target
    shutil.move(str(source), str(target))
    meta = Path(str(source) + '.meta')
    if meta.exists(): shutil.move(str(meta), str(target) + '.meta')

for name in ('PixelBoardRenderer', 'PixelCellView', 'BoardViewportInput', 'YarnThreadGraphic', 'PlushYarnBoard'):
    source = ROOT / f'Assets/Scripts/Presentation/{name}.cs'
    target = UNITY / f'{name}.cs'
    move(source, target)
    text = target.read_text(encoding='utf-8-sig').replace('namespace YarnScapes.Presentation', 'namespace WeaveKit.Unity')
    if name == 'PixelBoardRenderer':
        text = text.replace('using System.Collections;\n', '').replace('using YarnScapes.Core;', 'using WeaveKit.Core;')
        text = replace(text, 'MonoBehaviour\n', 'MonoBehaviour, IBoardViewportTarget\n')
        text = replace(text, 'private LevelDefinition level;', 'private GridDefinition level;\n        private BoardViewSettings settings;\n        private BoardState state;\n        private FillSession session;\n        private RegionPlayback playback;\n        private Color[] palette;')
        text = text.replace('        private bool[] filled;\n', '').replace('        private Coroutine filling;\n', '')
        text = replace(text, 'public int FilledCount { get; private set; }', 'public int FilledCount => state?.FilledCount ?? 0;')
        text = replace(text, 'public int TotalCount { get; private set; }', 'public int TotalCount => state?.TotalCount ?? 0;')
        text = replace(text, 'public bool IsFilling => filling != null;', 'public bool IsFilling => playback != null && playback.IsPlaying;')
        start = text.index('        public void Build(LevelDefinition')
        end = text.index('            Rect.anchorMin', start)
        text = text[:start] + '''        public void Build(GridDefinition definition, BoardViewSettings viewSettings, Action<int> clicked,
            IRegionRoutePlanner routePlanner = null)
        {
            if (state != null) throw new InvalidOperationException("Create a new board view when changing definitions.");
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (viewSettings == null || viewSettings.Palette == null || viewSettings.Palette.Length != definition.ColorCount)
                throw new ArgumentException("Provide one display color per grid color.", nameof(viewSettings));
            if (viewSettings.Width <= 0 || float.IsNaN(viewSettings.Width) || float.IsInfinity(viewSettings.Width))
                throw new ArgumentOutOfRangeException(nameof(viewSettings.Width));
            level = definition; settings = viewSettings; palette = (Color[])settings.Palette.Clone();
            fallback = settings.FallbackSprite; cellClicked = clicked;
            state = new BoardState(level); session = new FillSession(state, routePlanner);
            playback = gameObject.AddComponent<RegionPlayback>();
            playback.Initialize(session, settings.RouteStepSeconds);
            playback.Started += index => StitchStarted?.Invoke(index);
            playback.Reached += index => StitchReached?.Invoke(index);
            playback.CellFilled += index => FillVisual(index, true);
            playback.Completed += () => FillEnded?.Invoke();
            cells = new PixelCellView[level.Cells.Count];
            UsesReferenceYarnMaterial = settings.SmoothFilledTexture;
            artSprites = BuildSprites(settings.FilledTexture, settings.SmoothFilledTexture);
            unfilledSprites = BuildSprites(settings.UnfilledTexture, settings.SmoothUnfilledTexture);
''' + text[end:]
        text = replace(text, 'var shadowTexture = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_shadow");', 'var shadowTexture = settings.ShadowTexture;')
        text = replace(text, 'var font = Resources.Load<Font>("Fonts/Rubik-Medium") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");', 'var font = settings.Font;')
        text = text.replace('                if (Paintable(i)) TotalCount++;\n', '')
        text = replace(text, '''            plushBoard = gameObject.AddComponent<PlushYarnBoard>();
            if (!plushBoard.Initialize(level)) { Destroy(plushBoard); plushBoard = null; }''', '''            if (settings.Wool != null)
            {
                plushBoard = gameObject.AddComponent<PlushYarnBoard>();
                if (!plushBoard.Initialize(level, palette, settings.Wool)) { Destroy(plushBoard); plushBoard = null; }
            }''')
        start = text.index('        // Eight-neighbour traversal')
        end = text.index('        private void FillVisual', start)
        text = text[:start] + '''        public IReadOnlyList<int> ConnectedRoute(int seed, int color)
            => new DepthFirstRegionRoutePlanner().Build(state, seed, color);
        public bool TryFill(int index, int color) => playback.TryPlay(index, color);
        public int FindUnfilled(int color) => state.FindUnfilled(color);
        public int Remaining(int color) => state.Remaining(color);
        public Vector3 CellWorldPosition(int index) => cells[index].transform.position;
        public int[] GetFilledIndices() => state.GetFilledIndices();
        public void RestoreProgress(int[] indices)
        {
            foreach (var index in session.Restore(indices)) FillVisual(index, false);
        }
''' + text[end:]
        text = replace(text, 'plushBoard.Fill(index, GetColor(level.cells[index]), animate);', 'plushBoard.Fill(index, animate);')
        text = replace(text, 'cells[index].SetFilled(SpriteAt(artSprites,index),Color.white);', 'cells[index].SetFilled(SpriteAt(artSprites,index), artSprites != null ? Color.white : GetColor(level.cells[index]));')
        text = text.replace('level.fillAnimationSeconds', 'settings.CellPulseSeconds')
        text = replace(text, '''            if (filling != null) StopCoroutine(filling);
            filling = null; FilledCount = 0; Array.Clear(filled, 0, filled.Length);''', '''            playback.Cancel(); session.Reset();''')
        text = replace(text, 'public void SelectColor(int color) { selectedColor = color; RefreshUnfilled(); }', '''public void SelectColor(int color)
        {
            if (color < 0 || color >= level.ColorCount) throw new ArgumentOutOfRangeException(nameof(color));
            if (IsFilling) return;
            selectedColor = color; RefreshUnfilled();
        }''')
        text = replace(text, 'public Color GetColor(int index) => CozyYarnPalette.For(level,index);', 'public Color GetColor(int index) => palette[index];')
        text = re.sub(r'        private bool Paintable\(int i\).*?\n', '        private bool Paintable(int i) => level.IsPaintable(i);\n', text)
        text = re.sub(r'        private bool CanFill\(int i, int color\).*?\n', '', text)
        text = text.replace('if (filled[i])', 'if (state.IsFilled(i))')
        text = text.replace('level.width', 'level.Width').replace('level.height', 'level.Height').replace('level.cells.Length', 'level.Cells.Count').replace('level.cells', 'level.Cells')
        text = text.replace('new Vector2(BoardSize, BoardSize *', 'new Vector2(settings.Width, settings.Width *').replace('var size = BoardSize /', 'var size = settings.Width /')
        text = replace(text, '        private void OnDestroy()\n        {', '        private void OnDisable() { if (playback != null) playback.Cancel(); }\n        private void OnDestroy()\n        {\n            if (playback != null) playback.Cancel();')
    elif name == 'BoardViewportInput':
        text = replace(text, 'private PixelBoardRenderer board;', 'private IBoardViewportTarget board;\n        private ViewportSettings settings;\n        private Camera eventCamera;')
        text = replace(text, '''public void Initialize(PixelBoardRenderer target)
        { board = target; viewport = (RectTransform)transform; Fit(); }''', '''public void Initialize(IBoardViewportTarget target, ViewportSettings options = null, Camera uiCamera = null)
        {
            board = target ?? throw new System.ArgumentNullException(nameof(target));
            settings = options ?? new ViewportSettings(); eventCamera = uiCamera;
            viewport = (RectTransform)transform; Fit();
        }''')
        text = text.replace('a.position)', 'a.position, eventCamera)').replace('b.position)', 'b.position, eventCamera)')
        text = text.replace('lastSize.x - 28', 'lastSize.x - settings.Padding').replace('lastSize.y - 28', 'lastSize.y - settings.Padding')
        text = text.replace('WorldToScreenPoint(null, viewport.position)', 'WorldToScreenPoint(eventCamera, viewport.position)').replace('screenPoint, null, out local', 'screenPoint, eventCamera, out local')
        text = text.replace('Mathf.Clamp(value, 1, 8)', 'Mathf.Clamp(value, 1, Mathf.Max(1, settings.MaxZoom))')
        text = text.replace('Mathf.Max(zoom, 3.2f)', 'Mathf.Clamp(Mathf.Max(zoom, settings.FocusZoom), 1, Mathf.Max(1, settings.MaxZoom))')
        text = text.replace('zoom >= 1.7f', 'zoom >= settings.NumberZoom').replace('zoom < 2 ? 3.2f : 1', 'zoom < 2 ? settings.FocusZoom : 1').replace('Mathf.Pow(1.2f,', 'Mathf.Pow(settings.ScrollFactor,')
        # Guard event callbacks before setup, including destroyed/inactive targets.
        text = text.replace('if (board == null) return;', 'if (!HasTarget) return;')
        text = replace(text, 'public float Zoom => zoom;', 'private bool HasTarget => board != null && board.Rect != null;\n        public float Zoom => zoom;')
        text = replace(text, '            if (Input.touchCount > 1 || pinching) return;', '            if (!HasTarget || Input.touchCount > 1 || pinching) return;')
        text = replace(text, '            if (dragged || pinching', '            if (!HasTarget || dragged || pinching')
        text = replace(text, '        { ZoomAt(zoom * Mathf.Pow(settings.ScrollFactor, e.scrollDelta.y), e.position); }', '        { if (HasTarget) ZoomAt(zoom * Mathf.Pow(settings.ScrollFactor, e.scrollDelta.y), e.position); }')
    elif name == 'YarnThreadGraphic':
        text = replace(text, 'private PixelBoardRenderer board;', 'private ICellPositionSource board;\n        private ThreadStyle style;')
        text = replace(text, 'public void Initialize(PixelBoardRenderer target, Sprite sparkleSprite)', 'public void Initialize(ICellPositionSource target, Sprite sparkleSprite, Texture2D texture = null, ThreadStyle options = null)')
        text = replace(text, '            board = target; raycastTarget = false;', '''            if (sparkle != null) throw new System.InvalidOperationException("Thread is already initialized.");
            board = target ?? throw new System.ArgumentNullException(nameof(target));
            style = options ?? new ThreadStyle(); raycastTarget = false;''')
        text = replace(text, 'yarnTexture = Resources.Load<Texture2D>("YarnMaterialsV2/thread_strip") ?? Resources.Load<Texture2D>("YarnMaterialsV1/thread_strip");', 'yarnTexture = texture;')
        text = text.replace('new Vector2(32, 32)', 'Vector2.one * style.TipSize').replace('trail.Count > 16', 'trail.Count > Mathf.Max(1, style.TrailLength)')
        text = text.replace('Time.unscaledDeltaTime * 3)', 'Time.unscaledDeltaTime * style.FadeSpeed)').replace('Time.unscaledDeltaTime * 32)', 'Time.unscaledDeltaTime * style.FollowSpeed)')
        text = text.replace('for (var i = 1; i <= 48; i++)', 'var segments = Mathf.Max(2, style.CurveSegments);\n            for (var i = 1; i <= segments; i++)').replace('i / 48f', 'i / (float)segments')
        text = text.replace('textureDistance / 180', 'textureDistance / Mathf.Max(1, style.TextureRepeatLength)').replace('Vector2.down * 2.5f', 'Vector2.down * style.ShadowOffset').replace(', 9, new Color', ', style.ShadowWidth, new Color').replace(', 7.5f, shade', ', style.Width, shade')
        text = replace(text, '        private Vector2 Local', '        protected override void OnDisable() { Clear(); base.OnDisable(); }\n        private Vector2 Local')
    elif name == 'PlushYarnBoard':
        text = text.replace('using YarnScapes.Core;', 'using WeaveKit.Core;')
        text = replace(text, 'private const int RenderLayer = 30;', 'private int renderLayer;\n        private int geometrySeed;\n        private Color[] palette;')
        text = text.replace('private LevelDefinition level;', 'private GridDefinition level;')
        text = replace(text, 'public bool Initialize(LevelDefinition definition)', 'public bool Initialize(GridDefinition definition, Color[] colors, WoolRenderAssets assets)')
        text = replace(text, '            level = definition;', '''            if (Ready) throw new InvalidOperationException("Wool renderer is already initialized.");
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (colors == null || colors.Length != definition.ColorCount || colors.Length > 255)
                throw new ArgumentException("Wool requires 1..255 display colors matching the grid.", nameof(colors));
            if (assets == null || assets.Filaments == null) { Debug.LogError("Wool assets are unavailable."); return false; }
            if (assets.RenderLayer < 0 || assets.RenderLayer > 31) throw new ArgumentOutOfRangeException(nameof(assets.RenderLayer));
            level = definition; palette = (Color[])colors.Clone(); renderLayer = assets.RenderLayer; geometrySeed = assets.GeometrySeed;''')
        for field, shader in [('Strand','SoftWoolStrand'),('Fiber','BoundaryWoolFiber'),('Pile','SoftWoolPile'),('Bed','RecessedWoolBed'),('Height','WoolHeight')]:
            text = text.replace(f'Resources.Load<Shader>("YarnMaterialsV8/{shader}")', f'assets.{field}')
        text = text.replace('Resources.Load<Shader>("YarnMaterialsV2/PlushComposite")', 'assets.Composite').replace('Resources.Load<Texture2D>("YarnMaterialsV3/wool_filaments")', 'assets.Filaments')
        text = text.replace('CozyYarnPalette.For(level,k)', 'palette[k]').replace('CozyYarnPalette.For(level,level.cells[index])', 'palette[level.Cells[index]]')
        text = text.replace('level.width', 'level.Width').replace('level.height', 'level.Height').replace('level.palette.Length', 'level.ColorCount').replace('level.cells', 'level.Cells')
        text = text.replace('RenderLayer', 'renderLayer').replace('assets.renderLayer', 'assets.RenderLayer')
        text = text.replace('index*7919+326863', 'unchecked(index*7919+geometrySeed)').replace('19349663+326863', '19349663+geometrySeed')
        text = replace(text, 'public void Fill(int index, Color sourceColor, bool animate = true)', 'public void Fill(int index, bool animate = true)')
        text = replace(text, '            if (!filled.Add(index)) return;', '            if (!Ready || !level.IsPaintable(index) || !filled.Add(index)) return;')
        text = replace(text, '            Clear();if(stage!=null)Destroy(stage);', '            Clear();if(stage!=null)Destroy(stage);\n            if(display!=null)Destroy(display.gameObject);')
    target.write_text(text, encoding='utf-8')

# Runtime shaders and textures travel with the package; preserve existing import metadata/GUIDs.
move(ROOT/'Assets/Resources/YarnMaterialsV8', PACKAGE/'Runtime/Resources/WeaveKit/Wool')
move(ROOT/'Assets/Resources/YarnMaterialsV2/PlushComposite.shader', PACKAGE/'Runtime/Resources/WeaveKit/Wool/PlushComposite.shader')
move(ROOT/'Assets/Resources/YarnMaterialsV3/wool_filaments.png', PACKAGE/'Runtime/Resources/WeaveKit/Wool/wool_filaments.png')
move(ROOT/'Assets/Resources/YarnMaterialsV2/thread_strip.png', PACKAGE/'Runtime/Resources/WeaveKit/thread_strip.png')
for shader in (PACKAGE/'Runtime/Resources/WeaveKit/Wool').glob('*.shader'):
    shader.write_text(shader.read_text(encoding='utf-8-sig').replace('Shader "YarnScapes/', 'Shader "WeaveKit/'), encoding='utf-8')

runtime = ROOT/'Assets/Scripts/YarnScapesRuntime.cs'
text = runtime.read_text(encoding='utf-8-sig')
text = text.replace('using YarnScapes.Presentation;', 'using YarnScapes.Presentation;\nusing WeaveKit.Unity;')
text = replace(text, 'private YarnThreadGraphic thread;', 'private YarnThreadGraphic thread;\n    private CompletionBurst completionBurst;\n    private readonly System.Collections.Generic.List<Sprite> ownedSprites = new System.Collections.Generic.List<Sprite>();')
text = replace(text, 'board.Build(level, yarnSprite, artTexture, HandleCellClicked);', 'board.Build(YarnScapesComposition.Grid(level), YarnScapesComposition.BoardSettings(level, yarnSprite, artTexture), HandleCellClicked);')
text = replace(text, 'thread.Initialize(board, CreateSprite(Resources.Load<Texture2D>("Textures/pixel_sparkle_tintable")));', '''var sparkleSprite = CreateSprite(Resources.Load<Texture2D>("Textures/pixel_sparkle_tintable"));
        thread.Initialize(board, sparkleSprite, Resources.Load<Texture2D>("WeaveKit/thread_strip"));
        completionBurst = gameObject.AddComponent<CompletionBurst>();
        completionBurst.Initialize((RectTransform)canvasRoot, CreateSprite(Resources.Load<Texture2D>("Textures/pixel_style_mask_tintable")), sparkleSprite);''')
text = replace(text, 'StartCoroutine(PlayPlaceholderCompletionFx());', 'completionBurst.Play();')
start = text.index('    private IEnumerator PlayPlaceholderCompletionFx()')
end = text.index('    private void ShowCompletionOverlay()', start)
text = text[:start] + text[end:]
text = replace(text, '        StopAllCoroutines();', '        StopAllCoroutines();\n        completionBurst.Cancel();')
text = text.replace('private static Sprite CreateSprite(Texture2D texture)', 'private Sprite CreateSprite(Texture2D texture)')
text = replace(text, '        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);', '''        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        ownedSprites.Add(sprite);
        return sprite;''')
text = replace(text, '        background.sprite = sprite;', '        ownedSprites.Add(sprite);\n        background.sprite = sprite;')
text = replace(text, '            wrap.raycastTarget = false;', '            ownedSprites.Add(wrap.sprite);\n            wrap.raycastTarget = false;')
text = replace(text, '    private static Image CreatePanel', '''    private void OnDestroy()
    {
        foreach (var sprite in ownedSprites) if (sprite != null) Destroy(sprite);
        ownedSprites.Clear();
    }

    private static Image CreatePanel''')
runtime.write_text(text, encoding='utf-8')

capture = ROOT/'Assets/Scripts/Presentation/RuntimeAcceptanceCapture.cs'
text = capture.read_text(encoding='utf-8-sig')
text = 'using WeaveKit.Unity;\n' + text
text = replace(text, 'swatch.Initialize(fixture)', 'swatch.Initialize(YarnScapesComposition.Grid(fixture), YarnScapesComposition.Palette(fixture), YarnScapesComposition.WoolAssets())')
text = replace(text, 'swatch.Fill(j,fixture.palette[fixture.cells[j]].ToUnityColor(),false)', 'swatch.Fill(j,false)')
capture.write_text(text, encoding='utf-8')
print('Migrated components, their metadata and runtime assets into the embedded WeaveKit package.')
