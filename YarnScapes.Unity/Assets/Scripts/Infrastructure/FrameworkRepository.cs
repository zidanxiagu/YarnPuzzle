using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YarnScapes.Core;

namespace YarnScapes.Infrastructure
{
    public sealed class FrameworkRepository
    {
        public FrameworkCatalog Catalog { get; }
        public AppConfig Config { get; }
        private readonly Dictionary<string, CatalogLevel> levels;
        public FrameworkRepository()
        {
            Catalog = Read<FrameworkCatalog>("Framework/catalog");
            Config = Read<AppConfig>("Framework/app_config");
            if (Catalog.categories == null || Catalog.levels == null || Catalog.daily == null)
                throw new InvalidOperationException("Incomplete framework catalog");
            levels = Catalog.levels.ToDictionary(x => x.levelId);
            Config.pageSize = Math.Max(2, Math.Min(30, Config.pageSize));
        }
        private static T Read<T>(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Missing resource " + path);
            return JsonUtility.FromJson<T>(asset.text);
        }
        public CatalogLevel Find(string id) => id != null && levels.TryGetValue(id, out var item) ? item : null;
        public IEnumerable<CatalogLevel> Category(string name, bool onlyAvailable)
        {
            var category = Catalog.categories.FirstOrDefault(x => x.name == name);
            var result = category == null ? Catalog.levels : category.levelIds.Select(Find).Where(x => x != null);
            return result.Where(x => !onlyAvailable || x.available);
        }
        public CatalogLevel NextAvailable(string id, string category)
        {
            var list = Category(category, true).ToArray();
            var index = Array.FindIndex(list, x => x.levelId == id);
            return index >= 0 && index + 1 < list.Length ? list[index + 1] : null;
        }
    }
}
