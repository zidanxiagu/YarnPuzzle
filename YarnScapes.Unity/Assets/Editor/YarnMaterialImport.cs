using UnityEditor;
using UnityEngine;

namespace YarnScapes.Editor
{
    public sealed class YarnMaterialImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/YarnMaterialsV1/") && !assetPath.StartsWith("Assets/Resources/YarnMaterialsV2/") && !assetPath.StartsWith("Assets/Resources/YarnMaterialsV3/")) return;
            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Default;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.maxTextureSize = 4096;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.mipmapEnabled = true;
            texture.mipMapsPreserveCoverage = true;
            texture.alphaIsTransparency = true;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 2;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (assetPath.EndsWith("thread_strip.png") || assetPath.EndsWith("knit_tile.png"))
                texture.wrapModeU = TextureWrapMode.Repeat;
            if (assetPath.EndsWith("knit_tile.png")) texture.wrapModeV = TextureWrapMode.Repeat;
            if (assetPath.EndsWith("fiber_roughness.png")) texture.wrapMode = TextureWrapMode.Repeat;
            if (assetPath.EndsWith("wool_filaments.png"))
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.sRGBTexture = false;
                texture.alphaIsTransparency = false;
            }
        }
    }
}
