using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WeaveKit.Unity
{
    /// <summary>Independent unscaled pulse/flash feedback for any UI transform and Graphic.</summary>
    public sealed class UiFeedbackAnimator : MonoBehaviour
    {
        private Coroutine pulse, flash;
        private Transform scaleTarget;
        private Graphic colorTarget;
        private Vector3 settledScale;
        private Color settledColor;

        public void Pulse(Transform target, float seconds, float multiplier = 1.12f)
        {
            StopPulse();
            if (!isActiveAndEnabled || target == null) return;
            ValidateDuration(seconds);
            scaleTarget = target; settledScale = target.localScale;
            pulse = StartCoroutine(PulseRoutine(Mathf.Max(.02f, seconds * .5f), multiplier));
        }
        public void Flash(Graphic target, Color finalColor, float seconds, Color? flashColor = null)
        {
            StopFlash();
            if (!isActiveAndEnabled || target == null) return;
            ValidateDuration(seconds);
            colorTarget = target; settledColor = finalColor;
            if (seconds == 0) { colorTarget.color = settledColor; return; }
            flash = StartCoroutine(FlashRoutine(seconds, flashColor ?? Color.white));
        }
        public void Cancel() { StopPulse(); StopFlash(); }
        private void StopPulse()
        {
            if (pulse != null) StopCoroutine(pulse);
            pulse = null;
            if (scaleTarget != null) scaleTarget.localScale = settledScale;
            scaleTarget = null;
        }
        private void StopFlash()
        {
            if (flash != null) StopCoroutine(flash);
            flash = null;
            if (colorTarget != null) colorTarget.color = settledColor;
            colorTarget = null;
        }
        private IEnumerator PulseRoutine(float half, float multiplier)
        {
            var enlarged = settledScale * multiplier;
            for (var part = 0; part < 2; part++)
            {
                var elapsed = 0f;
                while (elapsed < half && scaleTarget != null)
                {
                    elapsed += Time.unscaledDeltaTime;
                    scaleTarget.localScale = part == 0 ? Vector3.Lerp(settledScale, enlarged, elapsed / half) : Vector3.Lerp(enlarged, settledScale, elapsed / half);
                    yield return null;
                }
            }
            if (scaleTarget != null) scaleTarget.localScale = settledScale;
            scaleTarget = null; pulse = null;
        }
        private IEnumerator FlashRoutine(float seconds, Color from)
        {
            var elapsed = 0f;
            while (elapsed < seconds && colorTarget != null)
            {
                elapsed += Time.unscaledDeltaTime;
                colorTarget.color = Color.Lerp(from, settledColor, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }
            if (colorTarget != null) colorTarget.color = settledColor;
            colorTarget = null; flash = null;
        }
        private static void ValidateDuration(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        }
        private void OnDisable() => Cancel();
    }
}
