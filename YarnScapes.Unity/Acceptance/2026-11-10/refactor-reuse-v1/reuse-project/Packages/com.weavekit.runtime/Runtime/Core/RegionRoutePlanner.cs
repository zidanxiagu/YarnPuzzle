using System;
using System.Collections.Generic;

namespace WeaveKit.Core
{
    public enum GridConnectivity { Four, Eight }

    public interface IRegionRoutePlanner
    {
        IReadOnlyList<int> Build(BoardState state, int seed, int color);
    }

    /// <summary>Deterministic DFS with explicit backtracking so the thread never jumps across a gap.</summary>
    public sealed class DepthFirstRegionRoutePlanner : IRegionRoutePlanner
    {
        public GridConnectivity Connectivity { get; }
        public DepthFirstRegionRoutePlanner(GridConnectivity connectivity = GridConnectivity.Eight)
        {
            if (connectivity != GridConnectivity.Four && connectivity != GridConnectivity.Eight)
                throw new ArgumentOutOfRangeException(nameof(connectivity));
            Connectivity = connectivity;
        }

        public IReadOnlyList<int> Build(BoardState state, int seed, int color)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var route = new List<int>();
            if (!state.CanFill(seed, color)) return route.AsReadOnly();
            var definition = state.Definition;
            var visited = new HashSet<int> { seed };
            var stack = new Stack<int>(); stack.Push(seed); route.Add(seed);
            while (stack.Count > 0)
            {
                var current = stack.Peek(); var next = -1;
                for (var dy = -1; dy <= 1 && next < 0; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (Connectivity == GridConnectivity.Four && Math.Abs(dx) + Math.Abs(dy) != 1) continue;
                    var x = current % definition.Width + dx; var y = current / definition.Width + dy;
                    if (x < 0 || y < 0 || x >= definition.Width || y >= definition.Height) continue;
                    var candidate = y * definition.Width + x;
                    if (state.CanFill(candidate, color) && visited.Add(candidate)) { next = candidate; break; }
                }
                if (next >= 0) { stack.Push(next); route.Add(next); }
                else { stack.Pop(); if (stack.Count > 0) route.Add(stack.Peek()); }
            }
            return route.AsReadOnly();
        }
    }
}
