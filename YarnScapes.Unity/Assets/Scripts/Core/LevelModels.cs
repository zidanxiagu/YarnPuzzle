using System;

namespace YarnScapes.Core
{
    [Serializable]
    public class RgbaColor
    {
        public float r;
        public float g;
        public float b;
        public float a = 1f;

    }

    [Serializable]
    public class LevelDefinition
    {
        public string levelId;
        public string displayName;
        public int width;
        public int height;
        public RgbaColor[] palette;
        public int[] cells;
        public int emptyCellValue = -1;
        public string artImageResource;
        public string finishedImageResource;
        public string unfilledImageResource;
        public float fillAnimationSeconds = 0.12f;
        public float completionDelaySeconds = 0.6f;
        public int rewardGems = 10;
        public int starThreshold = 100;
        public string sourceNote;
    }

    [Serializable]
    public class LevelProgressRecord
    {
        public string levelId;
        public int filledCells;
        public int totalCells;
        public bool completed;
        public int rewardGems;
        public int[] filledIndices = Array.Empty<int>();
    }

    [Serializable]
    public class ProgressSnapshot
    {
        public LevelProgressRecord[] levels = Array.Empty<LevelProgressRecord>();
    }
}
