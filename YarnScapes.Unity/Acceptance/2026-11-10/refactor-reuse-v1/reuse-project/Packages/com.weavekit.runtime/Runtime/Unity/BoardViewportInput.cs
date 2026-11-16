using UnityEngine;
using UnityEngine.EventSystems;

namespace WeaveKit.Unity
{
    // One input surface owns the board; artwork never intercepts gestures.
    public sealed class BoardViewportInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        private IBoardViewportTarget board;
        private ViewportSettings settings;
        private Camera eventCamera;
        private RectTransform viewport;
        private float fitScale = 1, zoom = 1;
        private bool dragged, pinching;
        private Vector2 lastSize;
        private bool HasTarget => board != null && !(board is Object native && native == null) && board.Rect != null;
        public float Zoom => zoom;
        public Vector2 Pan => HasTarget ? board.Rect.anchoredPosition : Vector2.zero;

        public void Initialize(IBoardViewportTarget target, ViewportSettings options = null, Camera uiCamera = null)
        {
            board = target ?? throw new System.ArgumentNullException(nameof(target));
            settings = options ?? new ViewportSettings(); eventCamera = uiCamera;
            viewport = (RectTransform)transform; Fit();
        }

        private void LateUpdate()
        {
            if (!HasTarget) return;
            if (viewport.rect.size != lastSize) RecalculateFit();
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, a.position, eventCamera) ||
                    !RectTransformUtility.RectangleContainsScreenPoint(viewport, b.position, eventCamera)) return;
                var previous = (a.position - a.deltaPosition - b.position + b.deltaPosition).magnitude;
                var current = (a.position - b.position).magnitude;
                if (previous > 1) ZoomAt(zoom * current / previous, (a.position + b.position) * .5f);
                pinching = dragged = true;
            }
            else if (Input.touchCount == 0) pinching = false;
        }

        private void RecalculateFit()
        {
            lastSize = viewport.rect.size;
            fitScale = Mathf.Max(.01f, Mathf.Min((lastSize.x - settings.Padding) / board.Rect.rect.width, (lastSize.y - settings.Padding) / board.Rect.rect.height));
            ApplyScale();
        }

        public void Fit()
        {
            if (!HasTarget) return;
            zoom = 1; board.Rect.anchoredPosition = Vector2.zero;
            RecalculateFit();
        }
        public void ZoomBy(float multiplier)
        { if (HasTarget) ZoomAt(zoom * multiplier, RectTransformUtility.WorldToScreenPoint(eventCamera, viewport.position)); }
        public void ZoomAt(float value, Vector2 screenPoint)
        {
            if (!HasTarget) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, eventCamera, out local);
            var old = zoom; zoom = Mathf.Clamp(value, 1, Mathf.Max(1, settings.MaxZoom));
            board.Rect.anchoredPosition = local - (local - board.Rect.anchoredPosition) * (zoom / old);
            ApplyScale();
        }
        public void FocusColor(int color)
        {
            if (!HasTarget) return;
            var index = board.FindUnfilled(color);
            if (index < 0) return;
            zoom = Mathf.Clamp(Mathf.Max(zoom, settings.FocusZoom), 1, Mathf.Max(1, settings.MaxZoom));
            board.Rect.localScale = Vector3.one * (fitScale * zoom);
            var target = (Vector2)viewport.InverseTransformPoint(board.CellWorldPosition(index));
            board.Rect.anchoredPosition -= target;
            ApplyScale();
        }
        private void ApplyScale()
        {
            board.Rect.localScale = Vector3.one * (fitScale * zoom);
            var half = (board.Rect.rect.size * (fitScale * zoom) - viewport.rect.size) * .5f;
            var p = board.Rect.anchoredPosition;
            p.x = Mathf.Clamp(p.x, -Mathf.Max(0, half.x), Mathf.Max(0, half.x));
            p.y = Mathf.Clamp(p.y, -Mathf.Max(0, half.y), Mathf.Max(0, half.y));
            board.Rect.anchoredPosition = p;
            board.SetNumberVisibility(zoom >= settings.NumberZoom);
        }
        public void OnPointerDown(PointerEventData e) { dragged = pinching; }
        public void OnBeginDrag(PointerEventData e) { dragged = true; }
        public void OnDrag(PointerEventData e)
        {
            if (!HasTarget || Input.touchCount > 1 || pinching) return;
            Vector2 now, before;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position - e.delta, e.pressEventCamera, out before);
            board.Rect.anchoredPosition += now - before; ApplyScale();
        }
        public void OnEndDrag(PointerEventData e) { }
        public void OnPointerClick(PointerEventData e)
        {
            if (!HasTarget || dragged || pinching || e.button != PointerEventData.InputButton.Left) return;
            if (e.clickCount == 2) ZoomAt(zoom < 2 ? settings.FocusZoom : 1, e.position);
            else board.ClickScreenPoint(e.position, e.pressEventCamera);
        }
        public void OnScroll(PointerEventData e)
        { if (HasTarget) ZoomAt(zoom * Mathf.Pow(settings.ScrollFactor, e.scrollDelta.y), e.position); }
    }
}
