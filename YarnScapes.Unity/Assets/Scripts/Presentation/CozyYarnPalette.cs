using UnityEngine;
using YarnScapes.Core;

namespace YarnScapes.Presentation
{
    // Display-only yarn dyes. Keep the recovered palette IDs and cell map in
    // LevelDefinition intact; spools, thread, loops and bed use this one lookup.
    public static class CozyYarnPalette
    {
        private static readonly Color[] StarColors = {
            Rgb(232,173,120), // apricot
            Rgb(217,155,165), // dusty rose
            Rgb(243,220,166), // butter cream
            Rgb(245,239,225), // warm ivory
            Rgb(72,90,101),   // blue charcoal
            Rgb(170,128,103), // cocoa
            Rgb(182,201,178), // sage
            Rgb(128,156,139), // moss
            Rgb(169,203,220), // powder blue
            Rgb(108,149,176), // denim
            Rgb(128,120,143), // smoky mauve
            Rgb(171,155,185), // lilac
            Rgb(203,185,207)  // lavender
        };
        private static Color Rgb(int r,int g,int b) => new Color(r/255f,g/255f,b/255f,1);
        public static Color For(LevelDefinition level,int index)
        {
            if(level.levelId=="level_326863" && level.palette.Length==StarColors.Length)
                return StarColors[index];
            var source=level.palette[index];
            return new Color(.065f+source.r*.90f,.065f+source.g*.90f,.065f+source.b*.90f,1);
        }
    }
}
