using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YarnScapes.Core;

namespace YarnScapes.Infrastructure
{
    public sealed class LocalProgressStore
    {
        private readonly string filePath;
        private ProgressSnapshot snapshot;
        private readonly AppSettings defaults;
        public AppSettings Settings => snapshot.settings;
        public string LastLevelId => snapshot.lastLevelId;
        public string LastError { get; private set; }
        public LevelProgressRecord[] Records => snapshot.levels.ToArray();

        public LocalProgressStore(string path = null, AppSettings defaultSettings = null)
        {
            defaults = defaultSettings ?? new AppSettings();
            filePath = Path.GetFullPath(path ?? Path.Combine(Application.persistentDataPath, "yarnscapes_progress.json"));
            snapshot = LoadSnapshot();
        }

        public LevelProgressRecord Get(string levelId)
        {
            return snapshot.levels.FirstOrDefault(x => x.levelId == levelId);
        }

        public void SaveLevel(LevelDefinition level, int filledCells, bool completed, int[] filledIndices = null)
        {
            var list = new List<LevelProgressRecord>(snapshot.levels ?? Array.Empty<LevelProgressRecord>());
            var record = list.FirstOrDefault(x => x.levelId == level.levelId);
            if (record == null)
            {
                record = new LevelProgressRecord { levelId = level.levelId };
                list.Add(record);
            }

            record.filledIndices = (filledIndices ?? Array.Empty<int>()).Where(i => i >= 0 && i < level.cells.Length && level.cells[i] >= 0 && level.cells[i] != level.emptyCellValue).Distinct().OrderBy(i => i).ToArray();
            record.filledCells = record.filledIndices.Length;
            record.totalCells = level.cells?.Count(value => value >= 0 && value != level.emptyCellValue) ?? 0;
            record.completed = record.totalCells > 0 && record.filledCells == record.totalCells;
            record.rewardGems = 0;
            snapshot.levels = list.ToArray();
            snapshot.lastLevelId = level.levelId;
            Persist();
        }

        public void SaveSettings() => Persist();
        public void DeleteLevel(string levelId)
        {
            snapshot.levels = snapshot.levels.Where(x => x.levelId != levelId).ToArray();
            if (snapshot.lastLevelId == levelId) snapshot.lastLevelId = null;
            Persist();
        }
        private void Persist()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var temporary = filePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(snapshot, true));
                if (File.Exists(filePath)) File.Replace(temporary, filePath, filePath + ".bak");
                else File.Move(temporary, filePath);
                LastError = null;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                Debug.LogWarning("Progress save failed: " + LastError);
            }
        }

        private ProgressSnapshot LoadSnapshot()
        {
            foreach (var candidate in new[] { filePath, filePath + ".bak" }) try
            {
                if (File.Exists(candidate))
                {
                    var loaded = JsonUtility.FromJson<ProgressSnapshot>(File.ReadAllText(candidate));
                    if (loaded != null && loaded.levels != null)
                    {
                        loaded.levels = loaded.levels.Where(x => x != null && !string.IsNullOrEmpty(x.levelId)).GroupBy(x => x.levelId).Select(x => x.Last()).ToArray();
                        if (loaded.settings == null) loaded.settings = CopyDefaults();
                        return loaded;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Progress load failed: " + exception.Message);
            }

            return new ProgressSnapshot { levels = Array.Empty<LevelProgressRecord>(), settings = CopyDefaults() };
        }
        private AppSettings CopyDefaults() => JsonUtility.FromJson<AppSettings>(JsonUtility.ToJson(defaults));
    }
}
