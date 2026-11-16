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

        public LocalProgressStore(string path = null)
        {
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

            record.filledCells = filledCells;
            record.totalCells = level.cells?.Count(value => value >= 0 && value != level.emptyCellValue) ?? 0;
            record.completed = completed;
            record.rewardGems = completed ? level.rewardGems : 0;
            record.filledIndices = filledIndices ?? Array.Empty<int>();
            snapshot.levels = list.ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, JsonUtility.ToJson(snapshot, true));
        }

        private ProgressSnapshot LoadSnapshot()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var loaded = JsonUtility.FromJson<ProgressSnapshot>(File.ReadAllText(filePath));
                    if (loaded != null && loaded.levels != null)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Progress load failed: " + exception.Message);
            }

            return new ProgressSnapshot { levels = Array.Empty<LevelProgressRecord>() };
        }
    }
}
