using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YarnScapes.Core;

namespace YarnScapes.Presentation
{
    public sealed class PixelBoardRenderer : MonoBehaviour
    {
        public const float BoardSize = 960f;
        private LevelDefinition level;
        private PixelCellView[] cells;
        private bool[] filled;
        private Sprite[] artSprites, unfilledSprites;
        private Sprite shadowSprite;
        private Sprite fallback;
        private int selectedColor = -1;
        private bool showNumbers;
        private Action<int> cellClicked;
        private Coroutine filling;
        private PlushYarnBoard plushBoard;
        public event Action<int> StitchStarted;
        public event Action<int> StitchReached;
        public event Action FillEnded;
        public int FilledCount { get; private set; }
        public int TotalCount { get; private set; }
        public bool IsFilling => filling != null;
        public int Width => level.width;
        public int Height => level.height;
        public int SelectedColor => selectedColor;
        public RectTransform Rect => (RectTransform)transform;
        public bool UsesReferenceYarnMaterial { get; private set; }
        public bool UsesPlushGeometry => plushBoard != null && plushBoard.Ready;

        public void Build(LevelDefinition definition, Sprite yarn, Action<int> clicked)
        { Build(definition, yarn, null, clicked); }
        public void Build(LevelDefinition definition, Sprite yarn, Texture2D art, Action<int> clicked)
        {
            level = definition; fallback = yarn; cellClicked = clicked;
            filled = new bool[level.cells.Length];
            cells = new PixelCellView[filled.Length];
            var materialArt = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_filled");
            UsesReferenceYarnMaterial = materialArt != null;
            artSprites = BuildSprites(materialArt != null ? materialArt : art, UsesReferenceYarnMaterial);
            var materialBlank = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_unfilled");
            unfilledSprites = BuildSprites(materialBlank != null ? materialBlank : Resources.Load<Texture2D>(level.unfilledImageResource ?? ""), materialBlank != null);
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(.5f, .5f);
            Rect.sizeDelta = new Vector2(BoardSize, BoardSize * level.height / level.width);
            Rect.anchoredPosition = Vector2.zero;
            var shadowTexture = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_shadow");
            if (shadowTexture != null)
            {
                var shadowObject = new GameObject("SoftYarnShadow", typeof(RectTransform), typeof(Image));
                shadowObject.transform.SetParent(transform, false);
                var shadowRect = (RectTransform)shadowObject.transform;
                shadowRect.anchorMin = Vector2.zero; shadowRect.anchorMax = Vector2.one;
                shadowRect.offsetMin = shadowRect.offsetMax = new Vector2(.5f, -1.8f);
                var image = shadowObject.GetComponent<Image>();
                shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, shadowTexture.width, shadowTexture.height), new Vector2(.5f, .5f), 100);
                image.sprite = shadowSprite; image.raycastTarget = false;
            }
            var size = BoardSize / level.width;
            var font = Resources.Load<Font>("Fonts/Rubik-Medium") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var i = 0; i < cells.Length; i++)
            {
                var go = new GameObject("Cell_" + i, typeof(RectTransform), typeof(Image), typeof(PixelCellView));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.sizeDelta = Vector2.one * size;
                rect.anchoredPosition = new Vector2((i % level.width + .5f) * size, -(i / level.width + .5f) * size);
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
                if (Paintable(i)) TotalCount++;
            }
            RefreshUnfilled();
            plushBoard = gameObject.AddComponent<PlushYarnBoard>();
            if (!plushBoard.Initialize(level)) { Destroy(plushBoard); plushBoard = null; }
        }
        public void ClickScreenPoint(Vector2 point, Camera camera)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, point, camera, out local)) return;
            var x = Mathf.FloorToInt((local.x - Rect.rect.xMin) / Rect.rect.width * level.width);
            var y = Mathf.FloorToInt((Rect.rect.yMax - local.y) / Rect.rect.height * level.height);
            if (x >= 0 && y >= 0 && x < level.width && y < level.height) cellClicked?.Invoke(y * level.width + x);
        }
        // Eight-neighbour traversal with backtracking keeps every thread segment adjacent.
        public List<int> ConnectedRoute(int seed, int color)
        {
            var route = new List<int>();
            if (!CanFill(seed, color)) return route;
            var visited = new HashSet<int> { seed };
            var stack = new Stack<int>(); stack.Push(seed); route.Add(seed);
            while (stack.Count > 0)
            {
                var current = stack.Peek(); var next = -1;
                for (var dy = -1; dy <= 1 && next < 0; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var x = current % level.width + dx; var y = current / level.width + dy;
                    if (x < 0 || y < 0 || x >= level.width || y >= level.height) continue;
                    var candidate = y * level.width + x;
                    if (CanFill(candidate, color) && !visited.Contains(candidate)) { next = candidate; break; }
                }
                if (next >= 0) { visited.Add(next); stack.Push(next); route.Add(next); }
                else { stack.Pop(); if (stack.Count > 0) route.Add(stack.Peek()); }
            }
            return route;
        }
        public bool TryFill(int index, int color)
        {
            if (IsFilling || !CanFill(index, color)) return false;
            filling = StartCoroutine(FillRegion(ConnectedRoute(index, color)));
            return true;
        }
        private IEnumerator FillRegion(List<int> route)
        {
            yield return null; // assign the coroutine handle before any completion callback
            StitchStarted?.Invoke(route[0]);
            for (var step = 0; step < route.Count; step++)
            {
                var index = route[step]; StitchReached?.Invoke(index);
                if (!filled[index])
                {
                    filled[index] = true; FilledCount++;
                    FillVisual(index, true);
                }
                // Provisional presentation timing; original timing parity still needs comparison.
                yield return new WaitForSecondsRealtime(.045f);
            }
            filling = null; FillEnded?.Invoke();
        }
        public int FindUnfilled(int color)
        { for (var i = 0; i < filled.Length; i++) if (CanFill(i, color)) return i; return -1; }
        public int Remaining(int color)
        { var count = 0; for (var i = 0; i < filled.Length; i++) if (CanFill(i, color)) count++; return count; }
        public Vector3 CellWorldPosition(int index) => cells[index].transform.position;
        public int[] GetFilledIndices()
        {
            var result = new List<int>();
            for (var i = 0; i < filled.Length; i++) if (filled[i]) result.Add(i);
            return result.ToArray();
        }
        public void RestoreProgress(int[] indices)
        {
            if (indices == null) return;
            foreach (var index in indices)
            {
                if (index < 0 || index >= filled.Length || filled[index] || !Paintable(index)) continue;
                filled[index] = true; FilledCount++;
                FillVisual(index, false);
            }
        }
        private void FillVisual(int index, bool animate)
        {
            if (UsesPlushGeometry)
            {
                cells[index].SetFilledGeometry();
                plushBoard.Fill(index, GetColor(level.cells[index]), animate);
            }
            else
            {
                cells[index].SetFilled(SpriteAt(artSprites,index),Color.white);
                if (animate) { cells[index].Pulse(level.fillAnimationSeconds);cells[index].Flash(.12f); }
            }
        }
        public void ResetBoard()
        {
            if (filling != null) StopCoroutine(filling);
            filling = null; FilledCount = 0; Array.Clear(filled, 0, filled.Length);
            if (UsesPlushGeometry) plushBoard.Clear();
            foreach (var cell in cells) cell.ResetAnimation();
            RefreshUnfilled();
        }
        public void SelectColor(int color) { selectedColor = color; RefreshUnfilled(); }
        public void SetNumberVisibility(bool visible)
        { if (showNumbers == visible) return; showNumbers = visible; RefreshUnfilled(); }
        public Color GetColor(int index) => level.palette[index].ToUnityColor();
        private bool Paintable(int i) => level.cells[i] >= 0 && level.cells[i] != level.emptyCellValue && level.cells[i] < level.palette.Length;
        private bool CanFill(int i, int color) => i >= 0 && i < filled.Length && !filled[i] && Paintable(i) && level.cells[i] == color;
        private void RefreshUnfilled()
        {
            for (var i = 0; i < cells.Length; i++)
            {
                if (filled[i]) continue;
                if (!Paintable(i)) { cells[i].SetEmpty(null); continue; }
                var selected = level.cells[i] == selectedColor;
                cells[i].SetUnfilled(SpriteAt(unfilledSprites, i), selected ? new Color(.67f, .67f, .67f) : Color.white, showNumbers ? (level.cells[i] + 1).ToString() : "");
                cells[i].SetLabelColor(selected ? new Color(.08f, .09f, .1f) : new Color(.58f, .59f, .6f));
            }
        }
        private Sprite SpriteAt(Sprite[] sprites, int i) => sprites == null ? fallback : sprites[i];
        private Sprite[] BuildSprites(Texture2D texture, bool smooth = false)
        {
            if (texture == null) return null;
            texture.filterMode = smooth ? FilterMode.Trilinear : FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var result = new Sprite[level.cells.Length];
            for (var y = 0; y < level.height; y++)
            for (var x = 0; x < level.width; x++)
            {
                var left = Mathf.RoundToInt(x * texture.width / (float)level.width);
                var right = Mathf.RoundToInt((x + 1) * texture.width / (float)level.width);
                var top = Mathf.RoundToInt(y * texture.height / (float)level.height);
                var bottom = Mathf.RoundToInt((y + 1) * texture.height / (float)level.height);
                result[y * level.width + x] = Sprite.Create(texture, new Rect(left, texture.height - bottom, right - left, bottom - top), new Vector2(.5f, .5f), 100);
            }
            return result;
        }
        private void OnDestroy()
        {
            if (artSprites != null) foreach (var sprite in artSprites) Destroy(sprite);
            if (unfilledSprites != null) foreach (var sprite in unfilledSprites) Destroy(sprite);
            if (shadowSprite != null) Destroy(shadowSprite);
        }
    }
}
