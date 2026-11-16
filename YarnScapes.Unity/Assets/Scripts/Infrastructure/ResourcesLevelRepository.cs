using UnityEngine;
using YarnScapes.Core;

namespace YarnScapes.Infrastructure
{
    public sealed class ResourcesLevelRepository
    {
        public LevelCatalogEntry[] LoadCatalog()
        {
            var asset = Resources.Load<TextAsset>("Levels/level_catalog");
            if (asset == null)
            {
                Debug.LogError("Missing level catalog resource");
                return System.Array.Empty<LevelCatalogEntry>();
            }

            var catalog = JsonUtility.FromJson<LevelCatalogFile>(asset.text);
            if (catalog == null || catalog.levels == null)
            {
                Debug.LogError("Invalid level catalog resource");
                return System.Array.Empty<LevelCatalogEntry>();
            }

            return catalog.levels;
        }

        public LevelDefinition Load(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            var asset = Resources.Load<TextAsset>("Levels/" + levelId);
            if (asset == null)
            {
                Debug.LogError("Missing level resource: " + levelId);
                return null;
            }

            LevelDefinition level;
            try { level = JsonUtility.FromJson<LevelDefinition>(asset.text); }
            catch (System.Exception ex) { Debug.LogError("Invalid JSON " + levelId + ": " + ex.Message); return null; }
            var error = Validate(level);
            if (error != null || level.levelId != levelId)
            {
                Debug.LogError("Invalid level resource: " + levelId + ": " + error);
                return null;
            }

            return level;
        }

        public static string Validate(LevelDefinition level)
        {
            if (level == null || string.IsNullOrEmpty(level.levelId)) return "Missing level ID";
            if (level.width <= 0 || level.height <= 0 || level.cells == null || (long)level.width * level.height != level.cells.Length) return "Invalid dimensions";
            if (level.palette == null || level.palette.Length == 0 || level.emptyCellValue >= 0) return "Invalid palette";
            foreach (var color in level.palette)
                if (color == null || !ValidChannel(color.r) || !ValidChannel(color.g) || !ValidChannel(color.b) || !ValidChannel(color.a)) return "Invalid RGBA";
            var count = 0;
            foreach (var value in level.cells)
            {
                if (value == level.emptyCellValue) continue;
                if (value < 0 || value >= level.palette.Length) return "Invalid color index";
                count++;
            }
            if (count == 0) return "No paintable cells";
            if (float.IsNaN(level.routeStepSeconds) || float.IsInfinity(level.routeStepSeconds) || level.routeStepSeconds < 0) return "Invalid route timing";
            if (float.IsNaN(level.fillAnimationSeconds) || float.IsInfinity(level.fillAnimationSeconds) || level.fillAnimationSeconds <= 0 || float.IsNaN(level.completionDelaySeconds) || float.IsInfinity(level.completionDelaySeconds) || level.completionDelaySeconds < 0) return "Invalid animation durations";
            return null;
        }
        private static bool ValidChannel(float x) => !float.IsNaN(x) && x >= 0 && x <= 1;
    }
}
