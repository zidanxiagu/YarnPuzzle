using UnityEngine;
using YarnScapes.Core;

namespace YarnScapes.Infrastructure
{
    public sealed class ResourcesLevelRepository
    {
        public LevelDefinition Load(string levelId)
        {
            var asset = Resources.Load<TextAsset>("Levels/" + levelId);
            if (asset == null)
            {
                Debug.LogError("Missing level resource: " + levelId);
                return null;
            }

            var level = JsonUtility.FromJson<LevelDefinition>(asset.text);
            if (level == null || level.cells == null || level.cells.Length != level.width * level.height)
            {
                Debug.LogError("Invalid level resource: " + levelId);
                return null;
            }

            return level;
        }
    }
}
