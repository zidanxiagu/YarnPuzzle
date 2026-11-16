using System;
using System.Collections.Generic;

namespace WeaveKit.Core
{
    public readonly struct FillStep
    {
        public int Index { get; }
        public bool NewlyFilled { get; }
        public FillStep(int index, bool newlyFilled) { Index = index; NewlyFilled = newlyFilled; }
    }

    /// <summary>Clock-free region playback. Advance once per presentation tick; cancel keeps completed cells.</summary>
    public sealed class FillSession
    {
        private readonly IRegionRoutePlanner planner;
        private IReadOnlyList<int> route;
        private int cursor;
        public BoardState State { get; }
        public bool IsPlaying => route != null;
        public FillSession(BoardState state, IRegionRoutePlanner planner = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            this.planner = planner ?? new DepthFirstRegionRoutePlanner();
        }
        public bool TryStart(int seed, int color)
        {
            if (IsPlaying || !State.CanFill(seed, color)) return false;
            var planned = planner.Build(State, seed, color);
            if (planned == null || planned.Count == 0) return false;
            // Own the route so a strategy cannot mutate a running session.
            var copy = new int[planned.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                if (!State.CanFill(planned[i], color)) throw new InvalidOperationException("Route contains an unavailable cell.");
                copy[i] = planned[i];
            }
            route = copy; cursor = 0; return true;
        }
        public bool Advance(out FillStep step)
        {
            step = default;
            if (route == null) return false;
            if (cursor == route.Count) { Cancel(); return false; }
            var index = route[cursor++];
            step = new FillStep(index, State.TryFill(index));
            return true;
        }
        public void Cancel() { route = null; cursor = 0; }
        public void Reset() { Cancel(); State.Reset(); }
        public int[] Restore(IEnumerable<int> indices)
        {
            if (IsPlaying) throw new InvalidOperationException("Cancel playback before restoring progress.");
            return State.Restore(indices);
        }
    }
}
