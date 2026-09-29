using System;
using UnityEngine;
using static DemolitionBuster.Editor.Art.Shapes;

namespace DemolitionBuster.Editor.Art
{
    /// <summary>
    /// Draws the tower, the hazards and the construction-site backdrop in a flat, softly shaded style that
    /// matches the Kenney characters. Pure drawing code (no editor API): canvases are exported by <see cref="ArtGenerator"/>.
    /// Light comes from the top left, so shadows fall to the right and below.
    /// </summary>
    public static class EnvironmentArt
    {
        public const int FloorVariants = 4;
        public const int CloudVariants = 2;

        // ---- Palette ----------------------------------------------------------------------------

        private static readonly Color Wall = Rgb(238, 217, 186);
        private static readonly Color WallLight = Rgb(247, 232, 208);
        private static readonly Color WallShade = Rgb(214, 186, 152);
        private static readonly Color Slab = Rgb(206, 199, 190);
        private static readonly Color SlabLight = Rgb(230, 225, 218);
        private static readonly Color Reveal = Rgb(146, 116, 94);
        private static readonly Color WindowFrame = Rgb(250, 248, 242);
        private static readonly Color Sill = Rgb(250, 243, 232);
        private static readonly Color Shadow = Rgb(40, 28, 20, 0.18f);

        private static readonly Color Steel = Rgb(222, 76, 52);
        private static readonly Color SteelDark = Rgb(160, 46, 34);
        private static readonly Color SteelLight = Rgb(248, 124, 92);
        private static readonly Color Galvanized = Rgb(158, 166, 176);
        private static readonly Color HazardYellow = Rgb(255, 199, 36);
        private static readonly Color HazardBlack = Rgb(42, 42, 52);

        private static readonly Color SkyTop = Rgb(88, 160, 226);
        private static readonly Color SkyHorizon = Rgb(206, 232, 246);

        // ---- Tower ------------------------------------------------------------------------------

        /// <summary>
        /// One 2 x 1 unit floor (512 x 256 px): concrete facade, pilasters, slab band and three windows.
        /// Variants share the facade and only change what is behind or around the windows (curtains, blinds,
        /// lit rooms, AC units, plants), so the building reads as one tower but never looks copy-pasted.
        /// </summary>
        public static PixelCanvas Floor(int variant)
        {
            var canvas = new PixelCanvas(512, 256);
            var random = new System.Random(1009 + variant * 7919);

            canvas.Fill((x, y) => Wall);
            Speckle(canvas, random, 0, 8, 512, 218, count: 160);
            // Weathering: the facade darkens slightly towards the bottom of each floor.
            canvas.Fill((x, y) => y < 48 ? Rgb(90, 60, 30, 0.07f * (1f - y / 48f)) : Color.clear);

            // Pilasters: the left one catches the light and casts a thin shadow, the right one is lit on its edge.
            Rect(canvas, 0, 0, 30, 256, WallLight);
            Rect(canvas, 30, 0, 36, 256, WallShade);
            Rect(canvas, 482, 0, 512, 256, WallLight);
            Rect(canvas, 482, 0, 486, 256, Color.white.WithAlpha(0.6f));

            float[] windowCenters = { 108f, 256f, 404f };
            foreach (float center in windowCenters) Window(canvas, center, random);

            // Floor slab band, with the shadow it casts on the facade below it.
            Rect(canvas, 0, 214, 512, 224, Shadow);
            Rect(canvas, 0, 224, 512, 256, Slab);
            Rect(canvas, 0, 248, 512, 256, SlabLight);
            Rect(canvas, 0, 0, 512, 3, Rgb(0, 0, 0, 0.12f));
            return canvas;
        }

        private static void Window(PixelCanvas canvas, float cx, System.Random random)
        {
            const float bottom = 70f;
            const float top = 196f;
            const float half = 46f;
            float gx0 = cx - half + 8f, gx1 = cx + half - 8f, gy0 = bottom + 8f, gy1 = top - 8f;
            int kind = random.Next(0, 5); // 0-1 plain, 2 lit, 3 curtains, 4 blinds

            // Rain streak under the sill, then the recess, the frame and the glass.
            canvas.Draw(Box(new Vector2(cx - 40, 8), new Vector2(cx + 40, bottom - 18)),
                p => Rgb(90, 64, 40, 0.1f * Mathf.Clamp01((p.y - 8f) / 50f)), new Vector2(cx - 40, 8), new Vector2(cx + 40, bottom));
            Rect(canvas, cx - half - 5, bottom - 4, cx + half + 7, top + 5, Reveal, 3);
            Rect(canvas, cx - half, bottom, cx + half, top, WindowFrame, 3);

            bool lit = kind == 2;
            Color glassTop = lit ? Rgb(255, 238, 168) : Rgb(156, 210, 242);
            Color glassBottom = lit ? Rgb(250, 186, 88) : Rgb(74, 126, 184);
            Sdf glass = Box(new Vector2(gx0, gy0), new Vector2(gx1, gy1), 2f);
            canvas.Draw(glass, p => Color.Lerp(glassBottom, glassTop, (p.y - gy0) / (gy1 - gy0)), new Vector2(gx0, gy0), new Vector2(gx1, gy1));

            if (kind == 3)
            {
                Color[] fabrics = { Rgb(214, 82, 72), Rgb(72, 160, 150), Rgb(142, 102, 186), Rgb(236, 170, 60) };
                Color fabric = fabrics[random.Next(fabrics.Length)];
                Color fold = Color.Lerp(fabric, Color.black, 0.2f);
                foreach (float edge in new[] { gx0, gx1 - 18f })
                {
                    Rect(canvas, edge, gy0, edge + 18, gy1, fabric);
                    Rect(canvas, edge + 8, gy0, edge + 10, gy1, fold);
                }
            }
            else if (kind == 4)
            {
                float blindBottom = gy1 - (gy1 - gy0) * Mathf.Lerp(0.3f, 0.75f, (float)random.NextDouble());
                Rect(canvas, gx0, blindBottom, gx1, gy1, Rgb(242, 236, 222));
                for (float y = gy1 - 7f; y > blindBottom; y -= 8f) Rect(canvas, gx0, y, gx1, y + 2, Rgb(210, 200, 182));
                Rect(canvas, gx0, blindBottom, gx1, blindBottom + 3, Rgb(180, 170, 152));
            }

            if (!lit)
            {
                // Sky reflections: two diagonal bands clipped to the glass.
                Sdf wide = Intersect(glass, Segment(new Vector2(gx0 - 20, gy0 + 4), new Vector2(gx0 + 34, gy1 + 30), 11f));
                Sdf thin = Intersect(glass, Segment(new Vector2(gx0 + 14, gy0 - 10), new Vector2(gx0 + 68, gy1 + 16), 3.5f));
                canvas.Draw(wide, Color.white.WithAlpha(0.28f), new Vector2(gx0, gy0), new Vector2(gx1, gy1));
                canvas.Draw(thin, Color.white.WithAlpha(0.35f), new Vector2(gx0, gy0), new Vector2(gx1, gy1));
            }

            // Mullion and transom.
            Rect(canvas, cx - 3, gy0, cx + 3, gy1, WindowFrame);
            Rect(canvas, gx0, gy1 - 36, gx1, gy1 - 30, WindowFrame);

            // Sill.
            Rect(canvas, cx - half - 10, bottom - 17, cx + half + 12, bottom - 13, Shadow);
            Rect(canvas, cx - half - 10, bottom - 13, cx + half + 12, bottom - 1, Sill, 2);

            switch (random.Next(0, 9))
            {
                case 0: AirConditioner(canvas, cx + (random.Next(2) == 0 ? -40f : 40f)); break;
                case 1: Plant(canvas, cx + (random.Next(2) == 0 ? -24f : 24f), bottom - 1); break;
            }
        }

        private static void AirConditioner(PixelCanvas canvas, float cx)
        {
            Rect(canvas, cx - 18, 10, cx + 24, 44, Shadow, 4);
            Rect(canvas, cx - 22, 14, cx + 20, 48, Rgb(226, 230, 234), 4);
            for (float x = cx - 16; x < cx - 2; x += 5) Rect(canvas, x, 20, x + 2, 42, Rgb(176, 182, 190));
            Disc(canvas, cx + 8, 31, 10, Rgb(176, 182, 190));
            Disc(canvas, cx + 8, 31, 3.5f, Rgb(226, 230, 234));
            Rect(canvas, cx - 22, 44, cx + 20, 48, Color.white.WithAlpha(0.7f), 2);
        }

        private static void Plant(PixelCanvas canvas, float cx, float sillTop)
        {
            Color leaf = Rgb(92, 176, 84);
            Color leafDark = Rgb(62, 138, 66);
            Disc(canvas, cx - 9, sillTop + 22, 10, leafDark);
            Disc(canvas, cx + 9, sillTop + 24, 10, leafDark);
            Disc(canvas, cx, sillTop + 32, 11, leaf);
            Disc(canvas, cx - 6, sillTop + 22, 8, leaf);
            Poly(canvas, Rgb(206, 104, 64),
                new Vector2(cx - 13, sillTop + 16), new Vector2(cx + 13, sillTop + 16), new Vector2(cx + 9, sillTop), new Vector2(cx - 9, sillTop));
            Rect(canvas, cx - 14, sillTop + 13, cx + 14, sillTop + 18, Rgb(226, 128, 84), 1.5f);
        }

        /// <summary>
        /// Steel I-beam sticking out of the facade (352 x 96 px, pivot on its left end). It must read as
        /// "danger" at a glance: saturated red paint and a yellow/black band on its tip.
        /// </summary>
        public static PixelCanvas Beam()
        {
            var canvas = new PixelCanvas(352, 96);

            Rect(canvas, 14, 4, 348, 62, Rgb(20, 16, 24, 0.22f), 4); // drop shadow

            // Profile: web between two flanges, lit from above.
            Rect(canvas, 0, 28, 336, 70, SteelDark);
            Rect(canvas, 0, 14, 340, 30, Steel, 3);
            Rect(canvas, 0, 66, 340, 84, Steel, 3);
            Rect(canvas, 0, 79, 338, 84, SteelLight, 2);
            Rect(canvas, 0, 25, 338, 30, SteelLight.WithAlpha(0.6f), 2);
            for (float x = 80; x < 280; x += 44)
            {
                Disc(canvas, x, 48, 6, SteelLight);
                Disc(canvas, x + 1, 47, 3, SteelDark);
            }

            // Hazard band on the tip.
            Sdf beam = Union(Box(new Vector2(0, 14), new Vector2(340, 84), 3), Box(new Vector2(0, 28), new Vector2(336, 70)));
            Sdf band = Intersect(beam, Box(new Vector2(282, 0), new Vector2(352, 96)));
            canvas.Draw(band, p => HazardStripes(p, 22f), new Vector2(282, 14), new Vector2(340, 84));
            Rect(canvas, 282, 79, 340, 84, Color.white.WithAlpha(0.25f), 2);

            // Bolted splice plate and broken concrete where the beam leaves the wall.
            Rect(canvas, 0, 8, 46, 90, Galvanized, 4);
            Rect(canvas, 0, 84, 46, 90, Color.white.WithAlpha(0.4f), 3);
            foreach (Vector2 bolt in new[] { new Vector2(30, 24), new Vector2(30, 74), new Vector2(30, 49) })
            {
                Disc(canvas, bolt.x, bolt.y, 5, Rgb(104, 110, 122));
                Disc(canvas, bolt.x - 1, bolt.y + 1, 2.5f, Rgb(200, 206, 214));
            }
            Poly(canvas, WallShade, new Vector2(0, 0), new Vector2(18, 4), new Vector2(12, 22), new Vector2(0, 26));
            Poly(canvas, Wall, new Vector2(0, 70), new Vector2(14, 78), new Vector2(20, 94), new Vector2(0, 96));
            Poly(canvas, WallLight, new Vector2(0, 80), new Vector2(10, 84), new Vector2(12, 96), new Vector2(0, 96));
            return canvas;
        }

        /// <summary>Concrete chunks and a bent rebar piled against the tower base (256 x 96 px, pivot bottom center).</summary>
        public static PixelCanvas Rubble()
        {
            var canvas = new PixelCanvas(256, 96);
            Line(canvas, new Vector2(150, 18), new Vector2(206, 70), 5, Rgb(150, 86, 60));
            Line(canvas, new Vector2(206, 70), new Vector2(228, 64), 5, Rgb(150, 86, 60));
            Poly(canvas, WallShade, new Vector2(20, 0), new Vector2(236, 0), new Vector2(200, 36), new Vector2(128, 52), new Vector2(60, 30));
            Poly(canvas, Wall, new Vector2(48, 0), new Vector2(110, 0), new Vector2(118, 46), new Vector2(72, 58), new Vector2(40, 30));
            Poly(canvas, WallLight, new Vector2(72, 58), new Vector2(118, 46), new Vector2(110, 38), new Vector2(66, 50));
            Poly(canvas, Slab, new Vector2(116, 0), new Vector2(190, 0), new Vector2(182, 34), new Vector2(140, 42), new Vector2(122, 24));
            Poly(canvas, SlabLight, new Vector2(140, 42), new Vector2(182, 34), new Vector2(178, 28), new Vector2(138, 35));
            Poly(canvas, Wall, new Vector2(176, 0), new Vector2(226, 0), new Vector2(214, 20), new Vector2(186, 22));
            Poly(canvas, WallShade, new Vector2(14, 0), new Vector2(52, 0), new Vector2(42, 16), new Vector2(22, 14));
            return canvas;
        }

        // ---- Street -----------------------------------------------------------------------------

        /// <summary>Sidewalk, curb and road (1536 x 256 px = 12 x 2 units, pivot top center: the top edge is the ground line).</summary>
        public static PixelCanvas Ground()
        {
            var canvas = new PixelCanvas(1536, 256);
            var random = new System.Random(5);
            Color asphalt = Rgb(72, 76, 88);

            Rect(canvas, 0, 0, 1536, 190, asphalt);
            Speckle(canvas, random, 0, 0, 1536, 186, count: 900, Rgb(255, 255, 255, 0.05f), Rgb(0, 0, 0, 0.08f));
            for (float x = 20; x < 1536; x += 160) Rect(canvas, x, 88, x + 90, 100, Rgb(236, 232, 220), 3);

            Rect(canvas, 0, 176, 1536, 190, Rgb(20, 20, 28, 0.25f));      // curb shadow on the road
            Rect(canvas, 0, 190, 1536, 206, Rgb(150, 150, 156));          // curb face
            Rect(canvas, 0, 206, 1536, 256, Rgb(204, 202, 198));          // sidewalk
            for (float x = 0; x < 1536; x += 96) Rect(canvas, x, 206, x + 3, 250, Rgb(170, 168, 164));
            Rect(canvas, 0, 250, 1536, 256, Rgb(226, 224, 220));
            return canvas;
        }

        /// <summary>Plywood site hoarding with a hazard-striped top rail (1536 x 176 px, pivot bottom center).</summary>
        public static PixelCanvas Hoarding()
        {
            var canvas = new PixelCanvas(1536, 176);
            var random = new System.Random(11);
            Color wood = Rgb(214, 172, 116);

            Rect(canvas, 0, 0, 1536, 150, wood);
            for (int i = 0; i < 260; i++)
            {
                float x = random.Next(0, 1536), y = random.Next(6, 146);
                Rect(canvas, x, y, x + random.Next(20, 70), y + 2, Rgb(170, 120, 70, 0.25f));
            }
            for (float x = 0; x < 1536; x += 128)
            {
                Rect(canvas, x, 0, x + 4, 150, Rgb(150, 108, 64));
                Rect(canvas, x + 4, 0, x + 7, 150, Rgb(255, 255, 255, 0.2f));
            }

            // Signs sit near the screen edges (x = ±2.6 units), clear of the tower and mostly clear of the player.
            foreach (float x in new[] { 250f, 1101f })
            {
                Rect(canvas, x - 38, 44, x + 38, 120, Rgb(250, 250, 246), 6);
                Poly(canvas, HazardBlack, new Vector2(x, 112), new Vector2(x + 32, 54), new Vector2(x - 32, 54));
                Poly(canvas, HazardYellow, new Vector2(x, 102), new Vector2(x + 23, 60), new Vector2(x - 23, 60));
                Rect(canvas, x - 3, 74, x + 3, 94, HazardBlack, 2);
                Disc(canvas, x, 67, 3.5f, HazardBlack);
            }
            foreach (float x in new[] { 435f, 1300f })
            {
                Disc(canvas, x, 82, 34, Rgb(250, 250, 246));
                Disc(canvas, x, 82, 29, Rgb(214, 56, 50));
                Rect(canvas, x - 19, 77, x + 19, 87, Rgb(250, 250, 246), 2);
            }

            Rect(canvas, 0, 146, 1536, 150, Rgb(0, 0, 0, 0.18f));
            canvas.Draw(Box(new Vector2(0, 150), new Vector2(1536, 176)), p => HazardStripes(p, 28f), new Vector2(0, 150), new Vector2(1536, 176));
            Rect(canvas, 0, 172, 1536, 176, Color.white.WithAlpha(0.3f));
            return canvas;
        }

        /// <summary>Traffic cone (96 x 128 px, pivot bottom center).</summary>
        public static PixelCanvas Cone()
        {
            var canvas = new PixelCanvas(96, 128);
            Color orange = Rgb(250, 110, 40);
            Rect(canvas, 4, 0, 92, 14, Rgb(60, 60, 70), 4);
            Poly(canvas, orange, new Vector2(18, 12), new Vector2(78, 12), new Vector2(54, 124), new Vector2(42, 124));
            Sdf body = Polygon(new Vector2(18, 12), new Vector2(78, 12), new Vector2(54, 124), new Vector2(42, 124));
            canvas.Draw(Intersect(body, Box(new Vector2(0, 40), new Vector2(96, 58))), Rgb(250, 250, 246), new Vector2(0, 40), new Vector2(96, 58));
            canvas.Draw(Intersect(body, Box(new Vector2(0, 80), new Vector2(96, 94))), Rgb(250, 250, 246), new Vector2(0, 80), new Vector2(96, 94));
            canvas.Draw(Intersect(body, Box(new Vector2(0, 0), new Vector2(40, 128))), Color.white.WithAlpha(0.18f), new Vector2(0, 0), new Vector2(48, 128));
            return canvas;
        }

        /// <summary>Soft contact shadow, stretched under the tower and the player (128 x 32 px).</summary>
        public static PixelCanvas ContactShadow()
        {
            var canvas = new PixelCanvas(128, 32);
            return canvas.Fill((x, y) =>
            {
                float dx = (x + 0.5f - 64f) / 64f;
                float dy = (y + 0.5f - 16f) / 16f;
                float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                return Rgb(20, 16, 30, falloff * falloff * 0.6f);
            });
        }

        // ---- Sky and skyline ----------------------------------------------------------------------

        /// <summary>Vertical sky gradient (4 x 256 px), stretched to fill the view.</summary>
        public static PixelCanvas Sky()
        {
            return new PixelCanvas(4, 256).Fill((x, y) => Color.Lerp(SkyHorizon, SkyTop, Mathf.Pow(y / 255f, 0.8f)));
        }

        public static PixelCanvas Sun()
        {
            var canvas = new PixelCanvas(256, 256);
            canvas.Fill((x, y) =>
            {
                float d = new Vector2(x + 0.5f - 128f, y + 0.5f - 128f).magnitude / 128f;
                float halo = Mathf.Clamp01(1f - d);
                return Rgb(255, 246, 200, halo * halo * 0.55f);
            });
            Disc(canvas, 128, 128, 46, Rgb(255, 232, 140));
            Disc(canvas, 122, 134, 36, Rgb(255, 244, 190));
            return canvas;
        }

        /// <summary>A flat-bottomed cartoon cloud with a shaded underside (384 x 160 px).</summary>
        public static PixelCanvas Cloud(int variant)
        {
            var canvas = new PixelCanvas(384, 160);
            Vector3[] puffs = variant == 0
                ? new[] { new Vector3(96, 60, 40), new Vector3(168, 84, 58), new Vector3(248, 74, 48), new Vector3(306, 52, 30) }
                : new[] { new Vector3(78, 52, 30), new Vector3(140, 76, 48), new Vector3(218, 92, 60), new Vector3(296, 62, 38) };

            Sdf shape = Box(new Vector2(40, 20), new Vector2(344, 50), 15);
            foreach (Vector3 puff in puffs) shape = Union(shape, Circle(new Vector2(puff.x, puff.y), puff.z));
            Color white = Rgb(255, 255, 255);
            Color shade = Rgb(214, 232, 246);
            canvas.Draw(shape, p => p.y < 40f ? shade : white, Vector2.zero, new Vector2(384, 160));
            return canvas;
        }

        /// <summary>Distant hazy skyline silhouettes (1536 x 640 px = 12 x 5 units, pivot bottom center).</summary>
        public static PixelCanvas SkylineFar()
        {
            var canvas = new PixelCanvas(1536, 640);
            var random = new System.Random(21);
            Color body = Rgb(164, 200, 228);
            Color window = Rgb(186, 216, 238);
            float x = -20f;
            while (x < 1536f)
            {
                float width = Mathf.Lerp(90f, 190f, (float)random.NextDouble());
                float height = Mathf.Lerp(220f, 560f, (float)random.NextDouble());
                Building(canvas, random, x, width, height, body, window, window, 10f, 12f, 20f, 26f, 0f);
                x += width + Mathf.Lerp(-30f, 10f, (float)random.NextDouble());
            }
            return canvas;
        }

        /// <summary>Closer skyline with lit windows and rooftop details (1536 x 448 px = 12 x 3.5 units, pivot bottom center).</summary>
        public static PixelCanvas SkylineNear()
        {
            var canvas = new PixelCanvas(1536, 448);
            var random = new System.Random(34);
            Color body = Rgb(116, 150, 192);
            Color window = Rgb(146, 180, 216);
            Color lit = Rgb(255, 226, 150);
            float x = -10f;
            while (x < 1536f)
            {
                float width = Mathf.Lerp(110f, 220f, (float)random.NextDouble());
                float height = Mathf.Lerp(150f, 380f, (float)random.NextDouble());
                Color tone = Color.Lerp(body, Rgb(100, 134, 178), (float)random.NextDouble());
                Building(canvas, random, x, width, height, tone, window, lit, 14f, 18f, 28f, 34f, 0.08f);
                x += width + Mathf.Lerp(6f, 30f, (float)random.NextDouble());
            }
            return canvas;
        }

        private static void Building(PixelCanvas canvas, System.Random random, float x0, float width, float height,
            Color body, Color window, Color lit, float windowWidth, float windowHeight, float stepX, float stepY, float litChance)
        {
            float x1 = x0 + width;
            float mid = x0 + width * 0.5f;
            Rect(canvas, x0, 0, x1, height, body);

            switch (random.Next(0, 6))
            {
                case 1: // stepped crown
                    Rect(canvas, x0 + width * 0.18f, height - 1, x1 - width * 0.18f, height + 26, body);
                    Rect(canvas, x0 + width * 0.34f, height + 25, x1 - width * 0.34f, height + 46, body);
                    break;
                case 2: // antenna
                    Rect(canvas, mid - 14, height - 1, mid + 14, height + 18, body);
                    Line(canvas, new Vector2(mid, height + 18), new Vector2(mid, height + 90), 4, body);
                    Disc(canvas, mid, height + 92, 4, body);
                    break;
                case 3: // pitched roof
                    Poly(canvas, body, new Vector2(x0, height - 1), new Vector2(x1, height - 1), new Vector2(mid, height + width * 0.32f));
                    break;
                case 4: // water tank
                    float tx = x0 + width * 0.62f;
                    Line(canvas, new Vector2(tx - 12, height), new Vector2(tx - 12, height + 22), 3, body);
                    Line(canvas, new Vector2(tx + 12, height), new Vector2(tx + 12, height + 22), 3, body);
                    Rect(canvas, tx - 20, height + 20, tx + 20, height + 58, body, 5);
                    Poly(canvas, body, new Vector2(tx - 22, height + 56), new Vector2(tx + 22, height + 56), new Vector2(tx, height + 72));
                    break;
                case 5: // rooftop boxes
                    Rect(canvas, x0 + 12, height - 1, x0 + 46, height + 20, body, 2);
                    Rect(canvas, x1 - 40, height - 1, x1 - 14, height + 14, body, 2);
                    break;
            }

            int columns = Mathf.FloorToInt((width - 24f) / stepX);
            if (columns <= 0) return;
            float startX = mid - (columns * stepX - (stepX - windowWidth)) * 0.5f;
            for (float y = 24f; y + windowHeight < height - 18f; y += stepY)
            {
                for (int c = 0; c < columns; c++)
                {
                    float wx = startX + c * stepX;
                    Color color = random.NextDouble() < litChance ? lit : window;
                    Rect(canvas, wx, y, wx + windowWidth, y + windowHeight, color, 1.5f);
                }
            }
        }

        /// <summary>
        /// Tower crane holding a steel beam (1024 x 1216 px = 8 x 9.5 units at 128 px/unit).
        /// Pivot at the foot of the mast: see <see cref="CraneMastCenter"/>.
        /// </summary>
        public static PixelCanvas Crane()
        {
            var canvas = new PixelCanvas(1024, 1216);
            Color paint = Rgb(242, 184, 60);
            Color paintDark = Rgb(204, 146, 44);
            Color cable = Rgb(84, 88, 100);
            const float mastLeft = 104f, mastRight = 152f, jibY = 1040f;

            // Mast: two rails braced by a zigzag lattice.
            Line(canvas, new Vector2(mastLeft, 0), new Vector2(mastLeft, jibY), 7, paint);
            Line(canvas, new Vector2(mastRight, 0), new Vector2(mastRight, jibY), 7, paint);
            for (float y = 0; y < jibY - 10; y += 48f)
            {
                Line(canvas, new Vector2(mastLeft, y), new Vector2(mastRight, y), 4, paintDark);
                bool even = Mathf.RoundToInt(y / 48f) % 2 == 0;
                Line(canvas, new Vector2(even ? mastLeft : mastRight, y), new Vector2(even ? mastRight : mastLeft, y + 48f), 4, paintDark);
            }

            // Jib: bottom chord, top chord sloping to the tip, lattice in between.
            const float tipX = 1012f;
            float TopChord(float x) => Mathf.Lerp(jibY + 52f, jibY + 12f, (x - mastRight) / (tipX - mastRight));
            for (float x = mastRight; x < tipX - 40f; x += 44f)
            {
                Line(canvas, new Vector2(x, jibY), new Vector2(x + 22f, TopChord(x + 22f)), 3.5f, paintDark);
                Line(canvas, new Vector2(x + 22f, TopChord(x + 22f)), new Vector2(x + 44f, jibY), 3.5f, paintDark);
            }
            Line(canvas, new Vector2(mastLeft, jibY), new Vector2(tipX, jibY), 7, paint);
            Line(canvas, new Vector2(mastRight, jibY + 52f), new Vector2(tipX, jibY + 12f), 6, paint);
            Line(canvas, new Vector2(tipX, jibY), new Vector2(tipX, jibY + 12f), 6, paint);

            // Counter-jib with concrete counterweights.
            Line(canvas, new Vector2(0, jibY), new Vector2(mastLeft, jibY), 7, paint);
            Line(canvas, new Vector2(0, jibY + 28f), new Vector2(mastLeft, jibY + 40f), 5, paint);
            for (float x = 6; x < 80; x += 24f)
            {
                Rect(canvas, x, jibY - 66, x + 22, jibY - 4, Rgb(170, 170, 176), 2);
                Rect(canvas, x, jibY - 12, x + 22, jibY - 4, Rgb(140, 140, 148), 2);
            }

            // Tower head and tie bars.
            Vector2 apex = new Vector2((mastLeft + mastRight) * 0.5f, jibY + 172f);
            Line(canvas, new Vector2(mastLeft, jibY), apex, 6, paint);
            Line(canvas, new Vector2(mastRight, jibY), apex, 6, paint);
            Line(canvas, apex, new Vector2(640, TopChord(640)), 3, cable);
            Line(canvas, apex, new Vector2(4, jibY + 30f), 3, cable);
            Disc(canvas, apex.x, apex.y, 7, HazardBlack);

            // Operator cab under the jib.
            Rect(canvas, mastRight - 6, jibY - 84, mastRight + 62, jibY - 6, paint, 8);
            Rect(canvas, mastRight + 18, jibY - 70, mastRight + 54, jibY - 38, Rgb(150, 206, 238), 4);
            Rect(canvas, mastLeft - 10, jibY - 14, mastRight + 10, jibY + 2, paintDark, 3);

            // Trolley and a short hook with a concrete bucket. Nothing red: beams are reserved for hazards.
            const float trolleyX = 600f, hookY = 860f;
            Rect(canvas, trolleyX - 22, jibY - 16, trolleyX + 22, jibY + 2, paintDark, 3);
            Line(canvas, new Vector2(trolleyX - 6, jibY - 16), new Vector2(trolleyX - 6, hookY), 2.5f, cable);
            Line(canvas, new Vector2(trolleyX + 6, jibY - 16), new Vector2(trolleyX + 6, hookY), 2.5f, cable);
            Rect(canvas, trolleyX - 18, hookY - 34, trolleyX + 18, hookY + 4, paint, 5);
            Line(canvas, new Vector2(trolleyX, hookY - 34), new Vector2(trolleyX - 34, hookY - 70), 2.5f, cable);
            Line(canvas, new Vector2(trolleyX, hookY - 34), new Vector2(trolleyX + 34, hookY - 70), 2.5f, cable);
            Poly(canvas, Rgb(150, 154, 162), new Vector2(trolleyX - 44, hookY - 70), new Vector2(trolleyX + 44, hookY - 70),
                new Vector2(trolleyX + 24, hookY - 130), new Vector2(trolleyX - 24, hookY - 130));

            // Aerial perspective: the crane stands far behind the tower, so it fades towards the sky.
            return canvas.Map(color => Color.Lerp(color, Rgb(196, 222, 240), 0.45f).WithAlpha(color.a));
        }

        /// <summary>Horizontal pivot of the crane sprite, at the center of its mast.</summary>
        public const float CraneMastCenter = 128f / 1024f;

        // ---- Helpers ----------------------------------------------------------------------------

        private static Color HazardStripes(Vector2 p, float period)
        {
            float band = Mathf.Repeat(p.x + p.y, period * 2f);
            return band < period ? HazardYellow : HazardBlack;
        }

        private static void Speckle(PixelCanvas canvas, System.Random random, float x0, float y0, float x1, float y1, int count,
            Color? light = null, Color? dark = null)
        {
            Color lightColor = light ?? Rgb(255, 255, 255, 0.18f);
            Color darkColor = dark ?? Rgb(120, 80, 40, 0.08f);
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(x0, x1, (float)random.NextDouble());
                float y = Mathf.Lerp(y0, y1, (float)random.NextDouble());
                Disc(canvas, x, y, Mathf.Lerp(1.2f, 3.2f, (float)random.NextDouble()), i % 2 == 0 ? lightColor : darkColor);
            }
        }

        private static void Rect(PixelCanvas canvas, float x0, float y0, float x1, float y1, Color color, float radius = 0f)
        {
            var min = new Vector2(x0, y0);
            var max = new Vector2(x1, y1);
            canvas.Draw(Box(min, max, radius), color, min, max);
        }

        private static void Disc(PixelCanvas canvas, float x, float y, float radius, Color color)
        {
            canvas.Draw(Circle(new Vector2(x, y), radius), color, new Vector2(x - radius, y - radius), new Vector2(x + radius, y + radius));
        }

        private static void Line(PixelCanvas canvas, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 margin = Vector2.one * width;
            canvas.Draw(Segment(a, b, width * 0.5f), color, Vector2.Min(a, b) - margin, Vector2.Max(a, b) + margin);
        }

        private static void Poly(PixelCanvas canvas, Color color, params Vector2[] points)
        {
            Vector2 min = points[0], max = points[0];
            foreach (Vector2 point in points)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            canvas.Draw(Polygon(points), color, min, max);
        }

        private static Color Rgb(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);

        private static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
