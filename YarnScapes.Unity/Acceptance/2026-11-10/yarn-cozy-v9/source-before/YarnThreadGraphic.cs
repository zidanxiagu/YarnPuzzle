using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YarnScapes.Presentation
{
    // Presentation approximation using a shaded UI thread and the original tip sprite.
    // This is not a port of the original GLES thread shader.
    public sealed class YarnThreadGraphic : MaskableGraphic
    {
        private RectTransform source;
        private PixelBoardRenderer board;
        private int targetIndex;
        private Vector2 tip;
        private float opacity;
        private bool weaving;
        private Image sparkle;
        private readonly List<int> trail = new List<int>();
        private Texture2D yarnTexture;
        private float textureDistance;
        public override Texture mainTexture => yarnTexture != null ? yarnTexture : base.mainTexture;
        public bool IsVisible => opacity > .05f;

        public void Initialize(PixelBoardRenderer target, Sprite sparkleSprite)
        {
            board = target; raycastTarget = false;
            yarnTexture = Resources.Load<Texture2D>("YarnMaterialsV2/thread_strip") ?? Resources.Load<Texture2D>("YarnMaterialsV1/thread_strip");
            var go = new GameObject("ThreadTip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            sparkle = go.GetComponent<Image>(); sparkle.sprite = sparkleSprite;
            sparkle.raycastTarget = false;
            sparkle.rectTransform.sizeDelta = new Vector2(32, 32);
            sparkle.enabled = false;
        }
        public void Begin(RectTransform spool, int index, Color threadColor)
        {
            source = spool; targetIndex = index;
            color = new Color(.067f + threadColor.r * .89f, .067f + threadColor.g * .89f, .067f + threadColor.b * .89f);
            opacity = 1; weaving = true; trail.Clear();
            tip = Local(source.TransformPoint(new Vector3(0, source.rect.height * .3f, 0)));
        }
        public void Reach(int index)
        {
            if (trail.Count == 0 || trail[trail.Count - 1] != index) trail.Add(index);
            if (trail.Count > 16) trail.RemoveAt(0);
            targetIndex = index;
        }
        public void End() { weaving = false; }
        public void Clear()
        { opacity = 0; weaving = false; trail.Clear(); if (sparkle != null) sparkle.enabled = false; SetVerticesDirty(); }
        private Vector2 Local(Vector3 world) => rectTransform.InverseTransformPoint(world);
        private void LateUpdate()
        {
            if (board == null || source == null) return;
            if (!weaving) opacity = Mathf.MoveTowards(opacity, 0, Time.unscaledDeltaTime * 3);
            var destination = Local(board.CellWorldPosition(targetIndex));
            tip = Vector2.Lerp(tip, destination, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 32));
            sparkle.enabled = opacity > .01f;
            sparkle.rectTransform.anchoredPosition = tip;
            sparkle.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 180);
            sparkle.rectTransform.localScale = Vector3.one * (1 + .2f * Mathf.Sin(Time.unscaledTime * 24));
            sparkle.color = new Color(1, .9f, .55f, opacity);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (source == null || opacity <= 0 || board == null) return;
            var start = Local(source.TransformPoint(new Vector3(0, source.rect.height * .3f, 0)));
            var control = (start + tip) * .5f + Vector2.right * Mathf.Min(90, Mathf.Abs(tip.y - start.y) * .15f);
            var previous = start;
            textureDistance = 0;
            for (var i = 1; i <= 48; i++)
            {
                var t = i / 48f; var u = 1 - t;
                var point = u * u * start + 2 * u * t * control + t * t * tip;
                ThreadSegment(vh, previous, point); previous = point;
            }
            for (var i = 1; i < trail.Count; i++)
                ThreadSegment(vh, Local(board.CellWorldPosition(trail[i - 1])), Local(board.CellWorldPosition(trail[i])));
        }
        private void ThreadSegment(VertexHelper vh, Vector2 a, Vector2 b)
        {
            var from = textureDistance / 180;
            textureDistance += Vector2.Distance(a, b);
            var to = textureDistance / 180;
            Segment(vh, a + Vector2.down * 2.5f, b + Vector2.down * 2.5f, 9, new Color(.12f, .08f, .05f, .22f * opacity), from, to);
            var shade = color; shade.a = opacity;
            Segment(vh, a, b, 7.5f, shade, from, to);
        }
        private static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint, float from, float to)
        {
            if ((a - b).sqrMagnitude < .001f) return;
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            var v = vh.currentVertCount;
            vh.AddVert(a - n, tint, new Vector2(from, 0)); vh.AddVert(a + n, tint, new Vector2(from, 1));
            vh.AddVert(b + n, tint, new Vector2(to, 1)); vh.AddVert(b - n, tint, new Vector2(to, 0));
            vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v, v + 2, v + 3);
        }
    }
}
