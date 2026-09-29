using UnityEditor;
using UnityEngine;

namespace DemolitionBuster.Editor.Art
{
    /// <summary>
    /// Import settings by folder, applied automatically to any texture dropped in the project:
    /// no manual Inspector setup, and re-baked art keeps the right settings.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        public const string CharactersFolder = "Assets/_Project/Art/Characters/";
        public const string EffectsFolder = "Assets/_Project/Art/Effects/";
        public const string UIFolder = "Assets/_Project/UI/Kenney/";

        /// <summary>
        /// Character frames are baked on a 448 x 320 canvas (Tools/CharacterBaker), feet at the bottom and
        /// body center at x = 192. At 180 px per unit, a character stands about one unit tall.
        /// </summary>
        public const float CharacterPixelsPerUnit = 180f;
        public static readonly Vector2 CharacterPivot = new Vector2(192f / 448f, 0f);

        /// <summary>Effect sprites are 512 px: one world unit at scale 1.</summary>
        public const float EffectPixelsPerUnit = 512f;

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(CharactersFolder)) ConfigureSprite(CharacterPixelsPerUnit, CharacterPivot);
            else if (assetPath.StartsWith(EffectsFolder)) ConfigureSprite(EffectPixelsPerUnit, new Vector2(0.5f, 0.5f));
            else if (assetPath.StartsWith(UIFolder)) ConfigureUITexture();
        }

        private void ConfigureSprite(float pixelsPerUnit, Vector2 pivot)
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        private void ConfigureUITexture()
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
