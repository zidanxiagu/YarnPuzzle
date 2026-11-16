using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WeaveKit.Core;

namespace WeaveKit.Unity
{
    public sealed class PixelBoardRenderer : MonoBehaviour, IBoardViewportTarget
    {
        public const float BoardSize = 960f;
        private GridDefinition level;
        private BoardViewSettings settings;
        private BoardState state;
        private FillSession session;
        private IRegionRoutePlanner routePlanner;
        private RegionPlayback playback;
        private Color[] palette;
        private PixelCellView[] cells;
        private Sprite[] artSprites, unfilledSprites;
        private Sprite shadowSprite;
        private GameObject shadowObject;
        private Sprite fallback;
        private int selectedColor = -1;
        private bool showNumbers;
        private Action<int> cellClicked;
        private PlushYarnBoard plushBoard;
        public event Action<int> StitchStarted;
        public event Action<int> StitchReached;
        public event Action FillEnded;
        public event Action FillCancelled;
        public int FilledCount => state?.FilledCount ?? 0;
        public int TotalCount => state?.TotalCount ?? 0;
        public bool IsFilling => playback != null && playback.IsPlaying;
        public int Width => level.Width;
        public int Height => level.Height;
        public int SelectedColor => selectedColor;
        public RectTransform Rect => (RectTransform)transform;
        public bool UsesReferenceYarnMaterial { get; private set; }
        public bool UsesPlushGeometry => plushBoard != null && plushBoard.Ready;

        public void Build(GridDefinition definition, BoardViewSettings viewSettings, Action<int> clicked,
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
            this.routePlanner = routePlanner ?? new DepthFirstRegionRoutePlanner();
            state = new BoardState(level); session = new FillSession(state, this.routePlanner);
            playback = gameObject.AddComponent<RegionPlayback>();
            playback.Initialize(session, settings.RouteStepSeconds);
            playback.Started += index => StitchStarted?.Invoke(index);
            playback.Reached += index => StitchReached?.Invoke(index);
            playback.CellFilled += index => FillVisual(index, true);
            playback.Completed += () => FillEnded?.Invoke();
            playback.Cancelled += () => FillCancelled?.Invoke();
            cells = new PixelCellView[level.Cells.Count];
            UsesReferenceYarnMaterial = settings.SmoothFilledTexture;
            artSprites = BuildSprites(settings.FilledTexture, settings.SmoothFilledTexture);
            unfilledSprites = BuildSprites(settings.UnfilledTexture, settings.SmoothUnfilledTexture);
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(.5f, .5f);
            Rect.sizeDelta = new Vector2(settings.Width, settings.Width * level.Height / level.Width);
            Rect.anchoredPosition = Vector2.zero;
            var shadowTexture = settings.ShadowTexture;
            if (shadowTexture != null)
            {
                shadowObject = new GameObject("SoftYarnShadow", typeof(RectTransform), typeof(Image));
                shadowObject.transform.SetParent(transform, false);
                var shadowRect = (RectTransform)shadowObject.transform;
                shadowRect.anchorMin = Vector2.zero; shadowRect.anchorMax = Vector2.one;
                shadowRect.offsetMin = shadowRect.offsetMax = new Vector2(.5f, -1.8f);
                var image = shadowObject.GetComponent<Image>();
                shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, shadowTexture.width, shadowTexture.height), new Vector2(.5f, .5f), 100);
                image.sprite = shadowSprite; image.raycastTarget = false;
            }
            var size = settings.Width / level.Width;
            var font = settings.Font;
            for (var i = 0; i < cells.Length; i++)
            {
                var go = new GameObject("Cell_" + i, typeof(RectTransform), typeof(Image), typeof(PixelCellView));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.sizeDelta = Vector2.one * size;
                rect.anchoredPosition = new Vector2((i % level.Width + .5f) * size, -(i / level.Width + .5f) * size);
                var image = go.GetComponent<Image>(); image.raycastTarget = false;
                var labelObject = new GameObject("Number", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(go.transform, false);
                var label = labelObject.GetComponent<Text>();
                // Rasterize glyphs at sufficient resolution before scaling with the board.
                const float glyphScale = 6;
                label.font = font; label.fontSize = Mathf.RoundToInt(size * .64f * glyphScale);
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
                label.rectTransform.sizeDelta = Vector2.one * size * glyphScale;
                label.rectTransform.localScale = Vector3.one / glyphScale;
                cells[i] = go.GetComponent<PixelCellView>();
                cells[i].Initialize(i, image, label, null, null);
            }
            RefreshUnfilled();
            if (settings.Wool != null)
            {
                plushBoard = gameObject.AddComponent<PlushYarnBoard>();
                if (!plushBoard.Initialize(level, palette, settings.Wool)) { Destroy(plushBoard); plushBoard = null; }
            }
        }
        public void ClickScreenPoint(Vector2 point, Camera camera)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, point, camera, out local)) return;
            var x = Mathf.FloorToInt((local.x - Rect.rect.xMin) / Rect.rect.width * level.Width);
            var y = Mathf.FloorToInt((Rect.rect.yMax - local.y) / Rect.rect.height * level.Height);
            if (x >= 0 && y >= 0 && x < level.Width && y < level.Height) cellClicked?.Invoke(y * level.Width + x);
        }
        public IReadOnlyList<int> ConnectedRoute(int seed, int color)
            => routePlanner.Build(state, seed, color);
        public bool TryFill(int index, int color) => playback.TryPlay(index, color);
        public int FindUnfilled(int color) => state.FindUnfilled(color);
        public int Remaining(int color) => state.Remaining(color);
        public Vector3 CellWorldPosition(int index) => cells[index].transform.position;
        public int[] GetFilledIndices() => state.GetFilledIndices();
        public void RestoreProgress(int[] indices)
        {
            foreach (var index in session.Restore(indices)) FillVisual(index, false);
        }
        private void FillVisual(int index, bool animate)
        {
            if (UsesPlushGeometry)
            {
                cells[index].SetFilledGeometry();
                plushBoard.Fill(index, animate);
            }
            else
            {
                cells[index].SetFilled(SpriteAt(artSprites,index), artSprites != null ? Color.white : GetColor(level.Cells[index]));
                if (animate) { cells[index].Pulse(settings.CellPulseSeconds);cells[index].Flash(.12f); }
            }
        }
        public void ResetBoard()
        {
            playback.Cancel(); session.Reset();
            if (UsesPlushGeometry) plushBoard.Clear();
            foreach (var cell in cells) cell.ResetAnimation();
            RefreshUnfilled();
        }
        public void SelectColor(int color)
        {
            if (color < 0 || color >= level.ColorCount) throw new ArgumentOutOfRangeException(nameof(color));
            if (IsFilling) return;
            selectedColor = color; RefreshUnfilled();
        }
        public void SetNumberVisibility(bool visible)
        { if (showNumbers == visible) return; showNumbers = visible; RefreshUnfilled(); }
        public Color GetColor(int index) => palette[index];
        private bool Paintable(int i) => level.IsPaintable(i);
        private void RefreshUnfilled()
        {
            for (var i = 0; i < cells.Length; i++)
            {
                if (state.IsFilled(i)) continue;
                if (!Paintable(i)) { cells[i].SetEmpty(null); continue; }
                var selected = level.Cells[i] == selectedColor;
                cells[i].SetUnfilled(SpriteAt(unfilledSprites, i), selected ? new Color(.67f, .67f, .67f) : Color.white, showNumbers ? (level.Cells[i] + 1).ToString() : "");
                cells[i].SetLabelColor(selected ? new Color(.08f, .09f, .1f) : new Color(.58f, .59f, .6f));
            }
        }
        private Sprite SpriteAt(Sprite[] sprites, int i) => sprites == null ? fallback : sprites[i];
        private Sprite[] BuildSprites(Texture2D texture, bool smooth = false)
        {
            if (texture == null) return null;
            texture.filterMode = smooth ? FilterMode.Trilinear : FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var result = new Sprite[level.Cells.Count];
            for (var y = 0; y < level.Height; y++)
            for (var x = 0; x < level.Width; x++)
            {
                var left = Mathf.RoundToInt(x * texture.width / (float)level.Width);
                var right = Mathf.RoundToInt((x + 1) * texture.width / (float)level.Width);
                var top = Mathf.RoundToInt(y * texture.height / (float)level.Height);
                var bottom = Mathf.RoundToInt((y + 1) * texture.height / (float)level.Height);
                result[y * level.Width + x] = Sprite.Create(texture, new Rect(left, texture.height - bottom, right - left, bottom - top), new Vector2(.5f, .5f), 100);
            }
            return result;
        }
        private void OnDisable() { if (playback != null) playback.Cancel(); }
        private void OnDestroy()
        {
            if (playback != null) { playback.Cancel(); Destroy(playback); }
            if (plushBoard != null) Destroy(plushBoard);
            if (cells != null) foreach (var cell in cells) if (cell != null) Destroy(cell.gameObject);
            if (shadowObject != null) Destroy(shadowObject);
            if (artSprites != null) foreach (var sprite in artSprites) Destroy(sprite);
            if (unfilledSprites != null) foreach (var sprite in unfilledSprites) Destroy(sprite);
            if (shadowSprite != null) Destroy(shadowSprite);
        }
    }
}
