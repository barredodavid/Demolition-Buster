using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static DemolitionBuster.Editor.Art.Shapes;

namespace DemolitionBuster.Editor.Art
{
    /// <summary>All generated sprites, ready to be referenced by prefabs and assets.</summary>
    public sealed class ArtSet
    {
        public Sprite Square;
        public Sprite Coin;
        public Sprite HazardStripe;

        public Sprite[] Floors;
        public Sprite Beam;
        public Sprite Rubble;

        public Sprite Sky;
        public Sprite Sun;
        public Sprite[] Clouds;
        public Sprite SkylineFar;
        public Sprite Crane;
        public Sprite SkylineNear;
        public Sprite Hoarding;
        public Sprite Ground;
        public Sprite Cone;
        public Sprite ContactShadow;
    }

    /// <summary>
    /// Exports the code-drawn art (<see cref="EnvironmentArt"/> plus a few UI sprites) as imported sprites.
    /// Characters and effects come from third-party art (see Art/Characters and Art/Effects).
    /// </summary>
    public static class ArtGenerator
    {
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        /// <summary>Draws every sprite and (re)imports it into <paramref name="folder"/>.</summary>
        public static void Generate(string folder)
        {
            Directory.CreateDirectory(folder);

            // Pixels per unit: 256 for gameplay art seen up close (the view is ~180 screen px per unit on a
            // 1080 px wide phone), 128 for the soft backdrop.
            Save(new PixelCanvas(4, 4).Fill((x, y) => Color.white), folder, "Square", 4f, Center);
            Save(DrawCoin(), folder, "Coin", 128f, Center);
            Save(DrawHazardStripe(), folder, "HazardStripe", 32f, Center);

            for (int i = 0; i < EnvironmentArt.FloorVariants; i++) Save(EnvironmentArt.Floor(i), folder, FloorName(i), 256f, Center);
            Save(EnvironmentArt.Beam(), folder, "Beam", 256f, new Vector2(0f, 0.5f)); // pivot on the wall, so it grows outwards
            Save(EnvironmentArt.Rubble(), folder, "Rubble", 256f, BottomCenter);

            Save(EnvironmentArt.Sky(), folder, "Sky", 256f, Center);
            Save(EnvironmentArt.Sun(), folder, "Sun", 128f, Center);
            for (int i = 0; i < EnvironmentArt.CloudVariants; i++) Save(EnvironmentArt.Cloud(i), folder, CloudName(i), 128f, Center);
            Save(EnvironmentArt.SkylineFar(), folder, "SkylineFar", 128f, BottomCenter);
            Save(EnvironmentArt.Crane(), folder, "Crane", 128f, new Vector2(EnvironmentArt.CraneMastCenter, 0f));
            Save(EnvironmentArt.SkylineNear(), folder, "SkylineNear", 128f, BottomCenter);
            Save(EnvironmentArt.Hoarding(), folder, "Hoarding", 128f, BottomCenter);
            Save(EnvironmentArt.Ground(), folder, "Ground", 128f, new Vector2(0.5f, 1f)); // top edge = ground line
            Save(EnvironmentArt.Cone(), folder, "Cone", 256f, BottomCenter);
            Save(EnvironmentArt.ContactShadow(), folder, "ContactShadow", 128f, Center);
        }

        /// <summary>Loads the generated sprites. Always reload after imports: earlier references may be stale.</summary>
        public static ArtSet Load(string folder)
        {
            Sprite Get(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}.png")
                ?? throw new FileNotFoundException($"Generated sprite '{name}' is missing. Run the generator first.");

            return new ArtSet
            {
                Square = Get("Square"),
                Coin = Get("Coin"),
                HazardStripe = Get("HazardStripe"),
                Floors = Enumerable.Range(0, EnvironmentArt.FloorVariants).Select(i => Get(FloorName(i))).ToArray(),
                Beam = Get("Beam"),
                Rubble = Get("Rubble"),
                Sky = Get("Sky"),
                Sun = Get("Sun"),
                Clouds = Enumerable.Range(0, EnvironmentArt.CloudVariants).Select(i => Get(CloudName(i))).ToArray(),
                SkylineFar = Get("SkylineFar"),
                Crane = Get("Crane"),
                SkylineNear = Get("SkylineNear"),
                Hoarding = Get("Hoarding"),
                Ground = Get("Ground"),
                Cone = Get("Cone"),
                ContactShadow = Get("ContactShadow"),
            };
        }

        private static string FloorName(int variant) => $"Floor_{variant}";

        private static string CloudName(int variant) => $"Cloud_{variant}";

        private static PixelCanvas DrawCoin()
        {
            var canvas = new PixelCanvas(64, 64);
            var center = new Vector2(32, 32);
            canvas.Draw(Circle(center, 31), new Color(0.55f, 0.36f, 0.04f));
            canvas.Draw(Circle(center, 27), new Color(1f, 0.76f, 0.12f));
            canvas.Draw(Circle(center, 19), new Color(1f, 0.85f, 0.32f));
            canvas.Draw(Box(new Vector2(28, 19), new Vector2(36, 45), 3), new Color(1f, 0.96f, 0.72f));
            return canvas;
        }

        private static PixelCanvas DrawHazardStripe()
        {
            var yellow = new Color(1f, 0.76f, 0.1f);
            var black = new Color(0.12f, 0.13f, 0.18f);
            return new PixelCanvas(608, 32).Fill((x, y) => ((x + y) / 24) % 2 == 0 ? yellow : black);
        }

        private static void Save(PixelCanvas canvas, string folder, string name, float pixelsPerUnit, Vector2 pivot)
        {
            string assetPath = $"{folder}/{name}.png";
            Texture2D texture = canvas.ToTexture();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
