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
        public float routeStepSeconds = 0.045f;
        public float completionDelaySeconds = 0.6f;
        public int rewardGems;
        public int starThreshold;
        public string sourceNote;
        public string paintId, categories, sourceUrl, pixelMapUrl, gifUrl, timeZone;
        public string dataEvidenceStatus, animationEvidenceStatus;
        public int unlockType, cornerMarker, distributeFlag;
        public long onlineDate;
    }

    [Serializable]
    public class LevelCatalogEntry
    {
        public string levelId;
        public string displayName;
        public string groupCategory;
        public string categories;
        public string sourceResource;
        public string finishedResource;
        public string unfilledResource;
    }

    [Serializable]
    public class LevelCatalogFile
    {
        public int schemaVersion = 1;
        public LevelCatalogEntry[] levels = Array.Empty<LevelCatalogEntry>();
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
        public int schemaVersion = 2;
        public string lastLevelId;
        public AppSettings settings = new AppSettings();
        public LevelProgressRecord[] levels = Array.Empty<LevelProgressRecord>();
    }
}
