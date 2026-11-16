using UnityEngine;

namespace WeaveKit.Unity
{
    /// <summary>Optional convenience loader for bundled materials. Renderers also accept explicitly supplied assets.</summary>
    public static class WeaveKitResources
    {
        public static Texture2D LoadThreadTexture() => Resources.Load<Texture2D>("WeaveKit/thread_strip");
        public static WoolRenderAssets LoadWool()
        {
            const string root = "WeaveKit/Wool/";
            return new WoolRenderAssets
            {
                Strand = Resources.Load<Shader>(root + "SoftWoolStrand"),
                Fiber = Resources.Load<Shader>(root + "BoundaryWoolFiber"),
                Pile = Resources.Load<Shader>(root + "SoftWoolPile"),
                Bed = Resources.Load<Shader>(root + "RecessedWoolBed"),
                Height = Resources.Load<Shader>(root + "WoolHeight"),
                Composite = Resources.Load<Shader>(root + "PlushComposite"),
                Filaments = Resources.Load<Texture2D>(root + "wool_filaments")
            };
        }
    }
}
