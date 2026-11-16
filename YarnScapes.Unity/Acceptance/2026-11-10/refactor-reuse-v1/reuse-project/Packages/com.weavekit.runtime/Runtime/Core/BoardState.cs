using System;
using System.Collections.Generic;

namespace WeaveKit.Core
{
    /// <summary>Progress only. No clock, rendering, persistence format or project-specific rewards.</summary>
    public sealed class BoardState
    {
        private readonly bool[] filled;
        private readonly int[] remaining;
        public GridDefinition Definition { get; }
        public int FilledCount { get; private set; }
        public int TotalCount => Definition.TotalCount;
        public bool IsComplete => TotalCount > 0 && FilledCount == TotalCount;

        public BoardState(GridDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            filled = new bool[definition.Cells.Count];
            remaining = new int[definition.ColorCount];
            Reset();
        }

        public bool IsFilled(int index) => index >= 0 && index < filled.Length && filled[index];
        public bool CanFill(int index, int color) => Definition.IsPaintable(index) && !filled[index] && Definition.Cells[index] == color;
        public bool TryFill(int index)
        {
            if (!Definition.IsPaintable(index) || filled[index]) return false;
            filled[index] = true; FilledCount++; remaining[Definition.Cells[index]]--;
            return true;
        }
        public int FindUnfilled(int color)
        {
            for (var i = 0; i < filled.Length; i++) if (CanFill(i, color)) return i;
            return -1;
        }
        public int Remaining(int color) => color >= 0 && color < remaining.Length ? remaining[color] : 0;
        public int[] GetFilledIndices()
        {
            var result = new int[FilledCount];
            var cursor = 0;
            for (var i = 0; i < filled.Length; i++) if (filled[i]) result[cursor++] = i;
            return result;
        }
        /// <summary>Add valid saved cells, ignoring duplicates, empty and out-of-range indices. Returns newly restored cells.</summary>
        public int[] Restore(IEnumerable<int> indices)
        {
            var restored = new List<int>();
            if (indices != null) foreach (var index in indices) if (TryFill(index)) restored.Add(index);
            return restored.ToArray();
        }
        public void Reset()
        {
            Array.Clear(filled, 0, filled.Length); Array.Clear(remaining, 0, remaining.Length);
            FilledCount = 0;
            for (var i = 0; i < filled.Length; i++) if (Definition.IsPaintable(i)) remaining[Definition.Cells[i]]++;
        }
    }
}
