using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WeaveKit.Unity
{
    [Serializable]
    public sealed class CompletionBurstSettings
    {
        public float Duration = 1.15f;
        public float Size = 500f;
        public float Radius = 210f;
        public int ParticleCount = 20;
        public float ParticleSize = 42f;
        public Color ParticleColor = new Color(1f, .78f, .08f, 1f);
    }

    /// <summary>Reusable UI burst. Owns temporary objects; supplied sprites remain owned by the caller.</summary>
    public sealed class CompletionBurst : MonoBehaviour
    {
        private RectTransform parent;
        private Sprite maskSprite, sparkleSprite;
        private CompletionBurstSettings settings;
        private GameObject effectRoot;
        private Coroutine routine;
        public bool IsPlaying => routine != null;

        public void Initialize(RectTransform target, Sprite mask, Sprite sparkle, CompletionBurstSettings options = null)
        {
            Cancel();
            parent = target != null ? target : throw new ArgumentNullException(nameof(target));
            maskSprite = mask; sparkleSprite = sparkle; settings = options ?? new CompletionBurstSettings();
            if (settings.Duration <= 0 || float.IsNaN(settings.Duration) || float.IsInfinity(settings.Duration) || settings.ParticleCount < 0)
                throw new ArgumentOutOfRangeException(nameof(options));
        }
        public void Play()
        {
            Cancel();
            if (!isActiveAndEnabled || parent == null || maskSprite == null) return;
            routine = StartCoroutine(Animate());
        }
        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (effectRoot != null) { effectRoot.SetActive(false); Destroy(effectRoot); effectRoot = null; }
        }
        private IEnumerator Animate()
        {
            effectRoot = new GameObject("CompletionBurst", typeof(RectTransform));
            var effectRect = (RectTransform)effectRoot.transform;
            effectRect.SetParent(parent, false);
            effectRect.anchorMin = effectRect.anchorMax = new Vector2(.5f, .5f);
            effectRect.sizeDelta = Vector2.one * settings.Size;
            var mask = CreateImage("Mask", maskSprite, effectRect);
            mask.rectTransform.anchorMin = Vector2.zero; mask.rectTransform.anchorMax = Vector2.one;
            mask.rectTransform.offsetMin = mask.rectTransform.offsetMax = Vector2.zero;
            var particles = new Image[settings.ParticleCount];
            var directions = new Vector2[particles.Length];
            for (var i = 0; i < particles.Length; i++)
            {
                particles[i] = CreateImage("Sparkle_" + i, sparkleSprite, effectRect);
                particles[i].rectTransform.sizeDelta = Vector2.one * settings.ParticleSize;
                var angle = Mathf.PI * 2f * i / particles.Length;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            var elapsed = 0f;
            while (elapsed < settings.Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / settings.Duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                effectRect.localScale = Vector3.Lerp(Vector3.one * .72f, Vector3.one * 1.1f, Mathf.Clamp01(t * 1.5f));
                mask.color = new Color(1f, 1f, 1f, Mathf.Lerp(.92f, 0f, t));
                for (var i = 0; i < particles.Length; i++)
                {
                    var rect = particles[i].rectTransform;
                    rect.anchoredPosition = directions[i] * (settings.Radius * eased);
                    var color = settings.ParticleColor;
                    color.a *= 1f - Mathf.Clamp01((t - .18f) / .82f);
                    particles[i].color = color;
                    rect.localRotation = Quaternion.Euler(0f, 0f, t * 260f + i * (360f / particles.Length));
                }
                yield return null;
            }
            Destroy(effectRoot); effectRoot = null; routine = null;
        }
        private static Image CreateImage(string name, Sprite sprite, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            return image;
        }
        private void OnDisable() => Cancel();
    }
}
