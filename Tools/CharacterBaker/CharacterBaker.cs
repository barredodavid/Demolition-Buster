using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class CharacterBaker
{
    public const int CanvasW = 448, CanvasH = 320, PoseX = 96, PoseY = 64;

    static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        p.AddArc(x, y, 2 * r, 2 * r, 180, 90);
        p.AddArc(x + w - 2 * r, y, 2 * r, 2 * r, 270, 90);
        p.AddArc(x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r, 0, 90);
        p.AddArc(x, y + h - 2 * r, 2 * r, 2 * r, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>Flat Kenney-style sledgehammer, drawn along +X from the grip (0,0).</summary>
    static void DrawHammer(Graphics g, float length)
    {
        using (var wood = new SolidBrush(Color.FromArgb(0x9c, 0x63, 0x33)))
        using (var woodDark = new SolidBrush(Color.FromArgb(0x7a, 0x4a, 0x24)))
        using (var steel = new SolidBrush(Color.FromArgb(0x8e, 0x98, 0xa8)))
        using (var steelLight = new SolidBrush(Color.FromArgb(0xc3, 0xca, 0xd6)))
        using (var steelDark = new SolidBrush(Color.FromArgb(0x6a, 0x73, 0x82)))
        {
            g.FillPath(wood, RoundRect(-14, -8, length + 14, 16, 7));
            g.FillRectangle(woodDark, -10, 2, length + 6, 5);
            float hx = length - 22;
            g.FillPath(steelDark, RoundRect(hx, -40, 46, 80, 10));
            g.FillPath(steel, RoundRect(hx, -40, 40, 76, 10));
            g.FillPath(steelLight, RoundRect(hx + 6, -34, 10, 62, 5));
        }
    }

    /// <summary>Pose placed on the big canvas, with an optional hammer behind it, gripped at (gripX, gripY) in pose pixels.</summary>
    public static Bitmap ComposeWithHammer(Bitmap pose, float gripX, float gripY, float angleDegrees, float length)
    {
        var canvas = new Bitmap(CanvasW, CanvasH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            var state = g.Save();
            g.TranslateTransform(PoseX + gripX, PoseY + gripY);
            g.RotateTransform(angleDegrees);
            DrawHammer(g, length);
            g.Restore(state);
            g.DrawImage(pose, PoseX, PoseY, pose.Width, pose.Height);
        }
        return canvas;
    }

    public static Bitmap Compose(Bitmap pose)
    {
        var canvas = new Bitmap(CanvasW, CanvasH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            g.Clear(Color.Transparent);
            g.DrawImage(pose, PoseX, PoseY, pose.Width, pose.Height);
        }
        return canvas;
    }

    /// <summary>Hue-based recolor: pixels whose hue is within [hueMin, hueMax] and saturation above satMin are mapped
    /// to the target color, keeping their relative lightness (so Kenney's shading survives).</summary>
    public static void Recolor(Bitmap b, float hueMin, float hueMax, float satMin, Color target, float referenceLightness, int yMin, int yMax)
    {
        Recolor(b, hueMin, hueMax, satMin, 1f, target, referenceLightness, yMin, yMax);
    }

    public static void Recolor(Bitmap b, float hueMin, float hueMax, float satMin, float lightnessMax, Color target, float referenceLightness, int yMin, int yMax)
    {
        float th = target.GetHue(), ts = target.GetSaturation(), tl = target.GetBrightness();
        for (int y = Math.Max(0, yMin); y < Math.Min(b.Height, yMax); y++)
        for (int x = 0; x < b.Width; x++)
        {
            Color c = b.GetPixel(x, y);
            if (c.A == 0) continue;
            float h = c.GetHue(), s = c.GetSaturation(), l = c.GetBrightness();
            bool inRange = hueMin <= hueMax ? (h >= hueMin && h <= hueMax) : (h >= hueMin || h <= hueMax);
            if (!inRange || s < satMin || l > lightnessMax) continue;
            float nl = Math.Max(0f, Math.Min(1f, tl + (l - referenceLightness)));
            b.SetPixel(x, y, FromHsl(c.A, th, ts, nl));
        }
    }

    public static Color FromHsl(int a, float h, float s, float l)
    {
        float c = (1 - Math.Abs(2 * l - 1)) * s;
        float hp = h / 60f;
        float x = c * (1 - Math.Abs(hp % 2 - 1));
        float r = 0, g = 0, bl = 0;
        if (hp < 1) { r = c; g = x; } else if (hp < 2) { r = x; g = c; } else if (hp < 3) { g = c; bl = x; }
        else if (hp < 4) { g = x; bl = c; } else if (hp < 5) { r = x; bl = c; } else { r = c; bl = x; }
        float m = l - c / 2;
        return Color.FromArgb(a, Clamp(r + m), Clamp(g + m), Clamp(bl + m));
    }

    static int Clamp(float v) { return Math.Max(0, Math.Min(255, (int)Math.Round(v * 255))); }

    /// <summary>Side-by-side preview on a sky background, with the ground line.</summary>
    public static Bitmap Sheet(Bitmap[] frames)
    {
        var sheet = new Bitmap(CanvasW * frames.Length, CanvasH + 16, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sheet))
        {
            g.Clear(Color.FromArgb(140, 201, 237));
            g.FillRectangle(new SolidBrush(Color.FromArgb(69, 71, 84)), 0, CanvasH, sheet.Width, 16);
            for (int i = 0; i < frames.Length; i++)
            {
                g.DrawImage(frames[i], i * CanvasW, 0, CanvasW, CanvasH);
                g.DrawRectangle(Pens.White, i * CanvasW, 0, CanvasW - 1, CanvasH - 1);
            }
        }
        return sheet;
    }
}
