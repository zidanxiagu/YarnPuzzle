using System;
using System.Collections.Generic;

namespace WeaveKit.Core
{
    /// <summary>Immutable row-major grid. Color IDs are zero based; empty cells use a separate sentinel.</summary>
    public sealed class GridDefinition
    {
        public int Width { get; }
        public int Height { get; }
        public int ColorCount { get; }
        public int EmptyCellValue { get; }
        public IReadOnlyList<int> Cells { get; }
        public int TotalCount { get; }

        public GridDefinition(int width, int height, int colorCount, int[] cells, int emptyCellValue = -1)
        {
            if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive and fit in an array.");
            if (colorCount <= 0) throw new ArgumentOutOfRangeException(nameof(colorCount));
            if (emptyCellValue >= 0 && emptyCellValue < colorCount)
                throw new ArgumentException("Empty-cell sentinel must not be a valid color.", nameof(emptyCellValue));
            if (cells == null || cells.Length != width * height)
                throw new ArgumentException("Cell count must match width * height.", nameof(cells));
            var copy = (int[])cells.Clone();
            var count = 0;
            foreach (var value in copy)
            {
                if (value == emptyCellValue) continue;
                if (value < 0 || value >= colorCount)
                    throw new ArgumentException("Cell color is outside the palette.", nameof(cells));
                count++;
            }
            Width = width; Height = height; ColorCount = colorCount;
            EmptyCellValue = emptyCellValue; TotalCount = count;
            Cells = Array.AsReadOnly(copy);
        }

        public bool IsPaintable(int index) => index >= 0 && index < Cells.Count && Cells[index] != EmptyCellValue;
    }
}
