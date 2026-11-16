using UnityEngine;
using WeaveKit.Core;
using WeaveKit.Unity;
using YarnScapes.Core;

namespace YarnScapes.Presentation
{
    /// <summary>The project boundary: recovered level DTOs, display dyes and resource paths stay here.</summary>
    public static class YarnScapesComposition
    {
        public static GridDefinition Grid(LevelDefinition level)
            => new GridDefinition(level.width, level.height, level.palette.Length, level.cells, level.emptyCellValue);

        public static Color[] Palette(LevelDefinition level)
        {
            var colors = new Color[level.palette.Length];
            for (var i = 0; i < colors.Length; i++) colors[i] = CozyYarnPalette.For(level, i);
            return colors;
        }

        public static WoolRenderAssets WoolAssets() => WeaveKitResources.LoadWool();

        public static BoardViewSettings BoardSettings(LevelDefinition level, Sprite fallback, Texture2D art)
        {
            var filled = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_filled");
            var unfilled = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_unfilled");
            return new BoardViewSettings
            {
                Palette = Palette(level), FallbackSprite = fallback,
                Font = Resources.Load<Font>("Fonts/Rubik-Medium") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                FilledTexture = filled != null ? filled : art, SmoothFilledTexture = filled != null,
                UnfilledTexture = unfilled != null ? unfilled : Resources.Load<Texture2D>(level.unfilledImageResource ?? ""),
                SmoothUnfilledTexture = unfilled != null,
                ShadowTexture = Resources.Load<Texture2D>("YarnMaterialsV1/" + level.levelId + "_shadow"),
                CellPulseSeconds = level.fillAnimationSeconds, RouteStepSeconds = level.routeStepSeconds,
                Wool = WoolAssets()
            };
        }
    }
}
