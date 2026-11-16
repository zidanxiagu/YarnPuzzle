using System;
using System.Collections;
using UnityEngine;
using WeaveKit.Core;

namespace WeaveKit.Unity
{
    /// <summary>Unscaled Unity clock adapter. Disabling or resetting cancels without firing Completed.</summary>
    public sealed class RegionPlayback : MonoBehaviour
    {
        private FillSession session;
        private Coroutine routine;
        private int generation;
        private float stepSeconds;
        public bool IsPlaying => routine != null;
        public event Action<int> Started;
        public event Action<int> Reached;
        public event Action<int> CellFilled;
        public event Action Completed;
        public event Action Cancelled;

        public void Initialize(FillSession target, float secondsPerStep = .045f)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (float.IsNaN(secondsPerStep) || float.IsInfinity(secondsPerStep) || secondsPerStep < 0)
                throw new ArgumentOutOfRangeException(nameof(secondsPerStep));
            Cancel(); session = target; stepSeconds = secondsPerStep;
        }
        public bool TryPlay(int index, int color)
        {
            if (!isActiveAndEnabled || session == null || IsPlaying || !session.TryStart(index, color)) return false;
            routine = StartCoroutine(Play(index, ++generation)); return true;
        }
        private IEnumerator Play(int seed, int token)
        {
            yield return null;
            Started?.Invoke(seed);
            if (token != generation) yield break;
            while (session.Advance(out var step))
            {
                // Notify visual state before external listeners can cancel/reset this playback.
                if (step.NewlyFilled) CellFilled?.Invoke(step.Index);
                if (token != generation) yield break;
                Reached?.Invoke(step.Index);
                if (token != generation) yield break;
                yield return new WaitForSecondsRealtime(stepSeconds);
            }
            routine = null; Completed?.Invoke();
        }
        public void Cancel()
        {
            var wasPlaying = IsPlaying;
            generation++;
            if (routine != null) StopCoroutine(routine);
            routine = null; session?.Cancel();
            if (wasPlaying) Cancelled?.Invoke();
        }
        private void OnDisable() => Cancel();
    }
}
