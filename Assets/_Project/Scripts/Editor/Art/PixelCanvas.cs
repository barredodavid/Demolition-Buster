using System;
using UnityEngine;

namespace DemolitionBuster.Editor.Art
{
    /// <summary>A signed distance function in pixel space: negative inside the shape, positive outside.</summary>
    public delegate float Sdf(Vector2 p);

    /// <summary>
    /// Minimal CPU rasterizer used to generate the environment art from code.
    /// Shapes are signed distance fields, which gives anti-aliased edges for free.
    /// </summary>
    public sealed class PixelCanvas
    {
        private readonly Color[] _pixels;

        public int Width { get; }
        public int Height { get; }

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width * height];
        }

        /// <summary>Composites <paramref name="color"/> over the canvas wherever the shape covers a pixel.</summary>
        public PixelCanvas Draw(Sdf shape, Color color) => Draw(shape, color, Vector2.zero, new Vector2(Width, Height));

        /// <summary>
        /// Same as <see cref="Draw(Sdf, Color)"/>, but only visits the pixels of the bounding box [min, max]:
        /// big canvases with hundreds of small shapes (skylines, windows) stay fast to generate.
        /// </summary>
        public PixelCanvas Draw(Sdf shape, Color color, Vector2 min, Vector2 max) => Draw(shape, _ => color, min, max);

        /// <summary>Draws a shape whose color varies per pixel (gradients, stripes clipped to the shape).</summary>
        public PixelCanvas Draw(Sdf shape, Func<Vector2, Color> shader, Vector2 min, Vector2 max)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(min.x) - 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(min.y) - 1);
            int x1 = Mathf.Min(Width, Mathf.CeilToInt(max.x) + 1);
            int y1 = Mathf.Min(Height, Mathf.CeilToInt(max.y) + 1);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float coverage = Mathf.Clamp01(0.5f - shape(p));
                    if (coverage <= 0f) continue;
                    Color color = shader(p);
                    BlendOver(y * Width + x, color, coverage * color.a);
                }
            }
            return this;
        }

        /// <summary>Per-pixel fill, for patterns that are not shapes (stripes, gradients).</summary>
        public PixelCanvas Fill(Func<int, int, Color> shader)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                Color color = shader(x, y);
                BlendOver(y * Width + x, color, color.a);
            }
            return this;
        }

        /// <summary>Transforms every pixel, e.g. to fade a finished drawing into the distance.</summary>
        public PixelCanvas Map(Func<Color, Color> transform)
        {
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = transform(_pixels[i]);
            return this;
        }

        /// <summary>Draws the shape twice: grown by <paramref name="thickness"/> in the outline color, then filled.</summary>
        public PixelCanvas DrawOutlined(Sdf shape, Color fill, Color outline, float thickness)
        {
            Draw(p => shape(p) - thickness, outline);
            return Draw(shape, fill);
        }

        public Texture2D ToTexture()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels(_pixels);
            texture.Apply();
            return texture;
        }

        private void BlendOver(int index, Color source, float alpha)
        {
            if (alpha <= 0f) return;
            Color destination = _pixels[index];
            float outAlpha = alpha + destination.a * (1f - alpha);
            Color rgb = (source * alpha + destination * destination.a * (1f - alpha)) / outAlpha;
            rgb.a = outAlpha;
            _pixels[index] = rgb;
        }
    }

    /// <summary>Signed distance primitives (see Inigo Quilez's reference formulas).</summary>
    public static class Shapes
    {
        public static Sdf Circle(Vector2 center, float radius) => p => (p - center).magnitude - radius;

        public static Sdf Box(Vector2 min, Vector2 max, float cornerRadius = 0f)
        {
            Vector2 center = (min + max) * 0.5f;
            Vector2 half = (max - min) * 0.5f;
            return p =>
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + Vector2.one * cornerRadius;
                Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - cornerRadius;
            };
        }

        /// <summary>A capsule between two points.</summary>
        public static Sdf Segment(Vector2 a, Vector2 b, float radius)
        {
            return p =>
            {
                Vector2 pa = p - a;
                Vector2 ba = b - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
                return (pa - ba * h).magnitude - radius;
            };
        }

        /// <summary>Everything above the horizontal line y.</summary>
        public static Sdf Above(float y) => p => y - p.y;

        /// <summary>A closed polygon (any winding, may be concave).</summary>
        public static Sdf Polygon(params Vector2[] points)
        {
            return p =>
            {
                float distance = Vector2.Dot(p - points[0], p - points[0]);
                float sign = 1f;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i, i++)
                {
                    Vector2 edge = points[j] - points[i];
                    Vector2 w = p - points[i];
                    Vector2 b = w - edge * Mathf.Clamp01(Vector2.Dot(w, edge) / Vector2.Dot(edge, edge));
                    distance = Mathf.Min(distance, Vector2.Dot(b, b));

                    // Crossing number test, to know whether p is inside.
                    bool above = p.y >= points[i].y;
                    bool below = p.y < points[j].y;
                    bool left = edge.x * w.y > edge.y * w.x;
                    if ((above && below && left) || (!above && !below && !left)) sign = -sign;
                }
                return sign * Mathf.Sqrt(distance);
            };
        }

        public static Sdf Intersect(Sdf a, Sdf b) => p => Mathf.Max(a(p), b(p));

        public static Sdf Subtract(Sdf a, Sdf b) => p => Mathf.Max(a(p), -b(p));

        public static Sdf Union(Sdf a, Sdf b) => p => Mathf.Min(a(p), b(p));
    }
}
