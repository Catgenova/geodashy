using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>A minimal CPU raster canvas used to draw placeholder art.</summary>
    public class Raster
    {
        public readonly int width;
        public readonly int height;
        public readonly Color32[] pixels;
        /// <summary>When true, x coordinates wrap around (used for seamless background tiles).</summary>
        public bool wrapX;
        /// <summary>When true, plotting a fully transparent colour punches the pixel out instead of being ignored.</summary>
        public bool punch;

        public Raster(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            pixels = new Color32[this.width * this.height];
        }

        public void Clear(Color c)
        {
            var c32 = (Color32)c;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c32;
        }

        /// <summary>Reads a pixel (transparent outside the canvas).</summary>
        public Color Get(int x, int y)
        {
            if (wrapX) x = ((x % width) + width) % width;
            if (x < 0 || y < 0 || x >= width || y >= height) return Color.clear;
            return pixels[y * width + x];
        }

        /// <summary>Writes a pixel without blending (so fully transparent pixels can be punched out).</summary>
        public void Set(int x, int y, Color32 c)
        {
            if (wrapX) x = ((x % width) + width) % width;
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            pixels[y * width + x] = c;
        }

        public void Plot(int x, int y, Color32 c)
        {
            if (wrapX) x = ((x % width) + width) % width;
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            if (c.a == 255)
            {
                pixels[y * width + x] = c;
                return;
            }
            if (c.a == 0)
            {
                if (punch) pixels[y * width + x] = new Color32(0, 0, 0, 0);
                return;
            }
            var dst = pixels[y * width + x];
            float a = c.a / 255f;
            float da = dst.a / 255f;
            float outA = a + da * (1f - a);
            if (outA <= 0f)
            {
                pixels[y * width + x] = new Color32(0, 0, 0, 0);
                return;
            }
            byte r = (byte)((c.r * a + dst.r * da * (1f - a)) / outA);
            byte g = (byte)((c.g * a + dst.g * da * (1f - a)) / outA);
            byte b = (byte)((c.b * a + dst.b * da * (1f - a)) / outA);
            pixels[y * width + x] = new Color32(r, g, b, (byte)(outA * 255f));
        }

        public void FillRect(float x0, float y0, float x1, float y1, Color c)
        {
            int ax = Mathf.RoundToInt(Mathf.Min(x0, x1));
            int bx = Mathf.RoundToInt(Mathf.Max(x0, x1));
            int ay = Mathf.RoundToInt(Mathf.Min(y0, y1));
            int by = Mathf.RoundToInt(Mathf.Max(y0, y1));
            var c32 = (Color32)c;
            for (int y = ay; y < by; y++)
            for (int x = ax; x < bx; x++)
                Plot(x, y, c32);
        }

        public void RectOutline(float x0, float y0, float x1, float y1, float thickness, Color c)
        {
            FillRect(x0, y0, x1, y0 + thickness, c);
            FillRect(x0, y1 - thickness, x1, y1, c);
            FillRect(x0, y0, x0 + thickness, y1, c);
            FillRect(x1 - thickness, y0, x1, y1, c);
        }

        public void Border(float thickness, Color c) => RectOutline(0, 0, width, height, thickness, c);

        public void FillCircle(float cx, float cy, float r, Color c)
        {
            FillEllipse(cx, cy, r, r, c);
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, Color c)
        {
            var c32 = (Color32)c;
            int x0 = Mathf.FloorToInt(cx - rx) - 1, x1 = Mathf.CeilToInt(cx + rx) + 1;
            int y0 = Mathf.FloorToInt(cy - ry) - 1, y1 = Mathf.CeilToInt(cy + ry) + 1;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Plot(x, y, c32);
            }
        }

        public void Ring(float cx, float cy, float rOuter, float rInner, Color c)
        {
            var c32 = (Color32)c;
            int x0 = Mathf.FloorToInt(cx - rOuter) - 1, x1 = Mathf.CeilToInt(cx + rOuter) + 1;
            int y0 = Mathf.FloorToInt(cy - rOuter) - 1, y1 = Mathf.CeilToInt(cy + rOuter) + 1;
            float ro2 = rOuter * rOuter, ri2 = rInner * rInner;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d2 = dx * dx + dy * dy;
                if (d2 <= ro2 && d2 >= ri2) Plot(x, y, c32);
            }
        }

        public void EllipseRing(float cx, float cy, float rx, float ry, float thickness, Color c)
        {
            var c32 = (Color32)c;
            int x0 = Mathf.FloorToInt(cx - rx) - 1, x1 = Mathf.CeilToInt(cx + rx) + 1;
            int y0 = Mathf.FloorToInt(cy - ry) - 1, y1 = Mathf.CeilToInt(cy + ry) + 1;
            float irx = Mathf.Max(0.5f, rx - thickness), iry = Mathf.Max(0.5f, ry - thickness);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f - cx, py = y + 0.5f - cy;
                float o = (px * px) / (rx * rx) + (py * py) / (ry * ry);
                float i = (px * px) / (irx * irx) + (py * py) / (iry * iry);
                if (o <= 1f && i >= 1f) Plot(x, y, c32);
            }
        }

        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            var c32 = (Color32)col;
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0;
                bool pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) Plot(x, y, c32);
            }
        }

        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public void FillPolygon(Vector2[] pts, Color col)
        {
            // fan triangulation (convex polygons only)
            for (int i = 1; i < pts.Length - 1; i++) FillTriangle(pts[0], pts[i], pts[i + 1], col);
        }

        public void Line(float x0, float y0, float x1, float y1, float thickness, Color c)
        {
            var c32 = (Color32)c;
            float dx = x1 - x0, dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
            float r = thickness * 0.5f;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float px = x0 + dx * t, py = y0 + dy * t;
                if (r <= 0.75f) Plot(Mathf.FloorToInt(px), Mathf.FloorToInt(py), c32);
                else FillCircle(px, py, r, c);
            }
        }

        public void FillRoundRect(float x0, float y0, float x1, float y1, float radius, Color c)
        {
            radius = Mathf.Min(radius, (x1 - x0) / 2f, (y1 - y0) / 2f);
            FillRect(x0 + radius, y0, x1 - radius, y1, c);
            FillRect(x0, y0 + radius, x1, y1 - radius, c);
            FillCircle(x0 + radius, y0 + radius, radius, c);
            FillCircle(x1 - radius, y0 + radius, radius, c);
            FillCircle(x0 + radius, y1 - radius, radius, c);
            FillCircle(x1 - radius, y1 - radius, radius, c);
        }

        public void RoundRectOutline(float x0, float y0, float x1, float y1, float radius, float thickness, Color c)
        {
            // draw outer then punch inner with transparent is not possible with blending; draw ring via two passes
            var tmp = new Raster(width, height);
            tmp.FillRoundRect(x0, y0, x1, y1, radius, Color.white);
            var inner = new Raster(width, height);
            inner.FillRoundRect(x0 + thickness, y0 + thickness, x1 - thickness, y1 - thickness, Mathf.Max(0, radius - thickness), Color.white);
            var c32 = (Color32)c;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (tmp.pixels[i].a > 0 && inner.pixels[i].a == 0) Plot(i % width, i / width, c32);
            }
        }

        /// <summary>Vertical gradient from bottom colour (y=0) to top colour.</summary>
        public void GradientV(Color bottom, Color top)
        {
            for (int y = 0; y < height; y++)
            {
                var c = (Color32)Color.Lerp(bottom, top, y / (float)(height - 1));
                for (int x = 0; x < width; x++) pixels[y * width + x] = c;
            }
        }

        /// <summary>Multiplies alpha across the whole raster.</summary>
        public void Fade(float alpha)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                p.a = (byte)(p.a * alpha);
                pixels[i] = p;
            }
        }

        public Texture2D ToTexture(FilterMode filter = FilterMode.Bilinear)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                name = "raster"
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        public Sprite ToSprite(float pixelsPerUnit, Vector2? pivot = null, Vector4? border = null, FilterMode filter = FilterMode.Bilinear)
        {
            var tex = ToTexture(filter);
            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), pivot ?? new Vector2(0.5f, 0.5f), pixelsPerUnit, 0,
                SpriteMeshType.FullRect, border ?? Vector4.zero);
            sprite.name = "placeholder";
            return sprite;
        }

        public static Color Darken(Color c, float amount) => new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
        public static Color Lighten(Color c, float amount) => Color.Lerp(c, new Color(1, 1, 1, c.a), amount);
        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
