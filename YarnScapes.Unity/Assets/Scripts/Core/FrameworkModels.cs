using System;

namespace YarnScapes.Core
{
    public enum HomeTab { Library, Daily, My }
    public enum AppScreen { Home, Playing, Complete, Settings }

    [Serializable] public sealed class CatalogCategory
    {
        public string id, uid, name, label;
        public bool newTag, forYouTag;
        public string[] levelIds = Array.Empty<string>();
    }
    [Serializable] public sealed class CatalogLevel : LevelCatalogEntry
    {
        public string paintId, sourceUrl, pixelMapUrl, gifUrl, timeZone, evidenceStatus;
        public int unlockType, cornerMarker, distributeFlag;
        public long onlineDate;
        public bool available;
        public int width, height, paletteCount, paintableCells;
    }
    [Serializable] public sealed class DailyEntry
    {
        public string levelId, timeZone;
        public long onlineDate;
        public int sort;
        public DateTime LocalDate => DateTimeOffset.FromUnixTimeSeconds(onlineDate).LocalDateTime.Date;
    }
    [Serializable] public sealed class FrameworkCatalog
    {
        public int schemaVersion;
        public string sourceApkSha256;
        public CatalogCategory[] categories = Array.Empty<CatalogCategory>();
        public CatalogLevel[] levels = Array.Empty<CatalogLevel>();
        public DailyEntry[] daily = Array.Empty<DailyEntry>();
    }
    [Serializable] public sealed class AppSettings
    {
        public bool musicEnabled = true, soundEnabled = true, autoSwitchColor = true, showCompleted = true;
    }
    [Serializable] public sealed class AppConfig
    {
        public int schemaVersion = 1, pageSize = 12;
        public string defaultCategory = "For You";
        public string[] tabs = { "Library", "Daily", "My" };
        public bool hideUnavailableByDefault = true;
        public AppSettings settings = new AppSettings();
    }
}
