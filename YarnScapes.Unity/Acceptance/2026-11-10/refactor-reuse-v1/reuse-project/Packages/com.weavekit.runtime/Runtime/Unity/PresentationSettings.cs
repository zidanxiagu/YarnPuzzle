using System;
using UnityEngine;

namespace WeaveKit.Unity
{
    public interface ICellPositionSource { Vector3 CellWorldPosition(int index); }
    public interface IBoardViewportTarget : ICellPositionSource
    {
        RectTransform Rect { get; }
        int FindUnfilled(int color);
        void SetNumberVisibility(bool visible);
        void ClickScreenPoint(Vector2 point, Camera camera);
    }

    [Serializable]
    public sealed class BoardViewSettings
    {
        public float Width = 960f;
        public Color[] Palette;
        public Font Font;
        public Sprite FallbackSprite;
        public Texture2D FilledTexture, UnfilledTexture, ShadowTexture;
        public bool SmoothFilledTexture, SmoothUnfilledTexture;
        public float CellPulseSeconds = .12f;
        public float RouteStepSeconds = .045f;
        public WoolRenderAssets Wool;
    }

    [Serializable]
    public sealed class WoolRenderAssets
    {
        public Shader Strand, Fiber, Pile, Bed, Height, Composite;
        public Texture2D Filaments;
        public int RenderLayer = 30;
        public int GeometrySeed = 326863;
    }

    [Serializable]
    public sealed class ThreadStyle
    {
        public float Width = 7.5f;
        public float ShadowWidth = 9f;
        public float ShadowOffset = 2.5f;
        public float TextureRepeatLength = 180f;
        public float FollowSpeed = 32f;
        public float FadeSpeed = 3f;
        public int TrailLength = 16;
        public int CurveSegments = 48;
        public float TipSize = 32f;
    }

    [Serializable]
    public sealed class ViewportSettings
    {
        public float Padding = 28f;
        public float MaxZoom = 8f;
        public float FocusZoom = 3.2f;
        public float NumberZoom = 1.7f;
        public float ScrollFactor = 1.2f;
    }
}
