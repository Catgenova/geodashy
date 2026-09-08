using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Procedural, seamlessly tiling silhouettes for the three parallax layers.</summary>
    public static class BackgroundArt
    {
        public const float PPU = 16f; // background tiles are low-res on purpose
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite ForLayer(BackgroundLayerDefinition layer)
        {
            if (cache.TryGetValue(layer.id, out var s) && s != null) return s;
            int w = Mathf.RoundToInt(layer.widthBlocks * PPU);
            int h = Mathf.RoundToInt(layer.heightBlocks * PPU);
            var r = new Raster(w, h) { wrapX = true };
            r.Clear(new Color(0, 0, 0, 0));
            var rng = new System.Random(layer.seed);
            var col = layer.silhouette;
            var col2 = Raster.Lighten(col, 0.12f);
            switch (layer.style)
            {
                case "mountains": Mountains(r, rng, col, col2); break;
                case "hills": Hills(r, rng, col, col2); break;
                case "castle": Castle(r, rng, col, col2); break;
                case "forest": Forest(r, rng, col, col2); break;
                case "pillars": Pillars(r, rng, col, col2); break;
                case "cave": Cave(r, rng, col, col2); break;
                case "crystals": Crystals(r, rng, col, col2); break;
                case "clouds": Clouds(r, rng, col, col2); break;
                default: Hills(r, rng, col, col2); break;
            }
            s = r.ToSprite(PPU, new Vector2(0.5f, 0f));
            s.name = "bg_" + layer.id;
            cache[layer.id] = s;
            return s;
        }

        static float Rand(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static void Mountains(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            int peaks = 6;
            float step = w / (float)peaks;
            // back ridge
            for (int i = 0; i < peaks; i++)
            {
                float x = i * step + Rand(rng, 0, step * 0.5f);
                float ph = Rand(rng, h * 0.5f, h * 0.95f);
                float pw = Rand(rng, step * 0.8f, step * 1.6f);
                r.FillTriangle(new Vector2(x - pw, 0), new Vector2(x + pw, 0), new Vector2(x, ph), Raster.Alpha(col2, 0.9f));
            }
            for (int i = 0; i < peaks; i++)
            {
                float x = i * step + step * 0.5f + Rand(rng, -step * 0.3f, step * 0.3f);
                float ph = Rand(rng, h * 0.35f, h * 0.7f);
                float pw = Rand(rng, step * 0.7f, step * 1.3f);
                r.FillTriangle(new Vector2(x - pw, 0), new Vector2(x + pw, 0), new Vector2(x, ph), col);
                // snow cap
                r.FillTriangle(new Vector2(x - pw * 0.18f, ph * 0.82f), new Vector2(x + pw * 0.18f, ph * 0.82f), new Vector2(x, ph), Raster.Lighten(col, 0.45f));
            }
        }

        static void Hills(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            float f1 = Rand(rng, 1.5f, 3f), f2 = Rand(rng, 3f, 6f);
            float p1 = Rand(rng, 0, 6.28f), p2 = Rand(rng, 0, 6.28f);
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)w * Mathf.PI * 2f;
                float y = h * (0.45f + 0.18f * Mathf.Sin(t * Mathf.Round(f1) + p1) + 0.08f * Mathf.Sin(t * Mathf.Round(f2) + p2));
                r.FillRect(x, 0, x + 1, y, col2);
                float y2 = h * (0.3f + 0.12f * Mathf.Sin(t * Mathf.Round(f1) + p1 + 1.5f) + 0.06f * Mathf.Cos(t * Mathf.Round(f2) + p2));
                r.FillRect(x, 0, x + 1, y2, col);
            }
        }

        static void Castle(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            // wall
            float wallH = h * 0.3f;
            r.FillRect(0, 0, w, wallH, col);
            for (int x = 0; x < w; x += 12) r.FillRect(x, wallH, x + 6, wallH + 5, col);
            int towers = 5;
            float step = w / (float)towers;
            for (int i = 0; i < towers; i++)
            {
                float x = i * step + Rand(rng, 0, step * 0.4f);
                float tw = Rand(rng, 10, 22);
                float th = Rand(rng, h * 0.45f, h * 0.9f);
                var c = (i % 2 == 0) ? col : col2;
                r.FillRect(x, 0, x + tw, th, c);
                for (float bx = x - 2; bx < x + tw + 2; bx += 6) r.FillRect(bx, th, bx + 3, th + 4, c);
                if (rng.NextDouble() > 0.4)
                {
                    r.FillTriangle(new Vector2(x - 2, th + 4), new Vector2(x + tw + 2, th + 4), new Vector2(x + tw / 2f, th + tw * 1.2f), c);
                    r.FillRect(x + tw / 2f, th + tw * 1.2f, x + tw / 2f + 1, th + tw * 1.6f, c);
                    r.FillTriangle(new Vector2(x + tw / 2f + 1, th + tw * 1.6f), new Vector2(x + tw / 2f + 1, th + tw * 1.4f), new Vector2(x + tw / 2f + 7, th + tw * 1.5f), Raster.Lighten(c, 0.3f));
                }
                // windows
                for (float wy = th * 0.3f; wy < th - 8; wy += 10)
                {
                    r.FillRect(x + tw / 2f - 1.5f, wy, x + tw / 2f + 1.5f, wy + 4, Raster.Lighten(c, 0.5f));
                }
            }
        }

        static void Forest(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            r.FillRect(0, 0, w, h * 0.12f, col);
            int trees = 14;
            for (int i = 0; i < trees; i++)
            {
                float x = i * (w / (float)trees) + Rand(rng, -4, 4);
                float th = Rand(rng, h * 0.4f, h * 0.95f);
                float tw = Rand(rng, 6, 14);
                var c = (i % 3 == 0) ? col2 : col;
                r.FillRect(x - 1.5f, 0, x + 1.5f, th * 0.5f, c);
                if (rng.NextDouble() > 0.5)
                {
                    r.FillTriangle(new Vector2(x - tw, th * 0.25f), new Vector2(x + tw, th * 0.25f), new Vector2(x, th * 0.7f), c);
                    r.FillTriangle(new Vector2(x - tw * 0.8f, th * 0.5f), new Vector2(x + tw * 0.8f, th * 0.5f), new Vector2(x, th), c);
                }
                else
                {
                    r.FillCircle(x, th * 0.65f, tw, c);
                    r.FillCircle(x - tw * 0.6f, th * 0.5f, tw * 0.75f, c);
                    r.FillCircle(x + tw * 0.6f, th * 0.5f, tw * 0.75f, c);
                }
            }
        }

        static void Pillars(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            r.FillRect(0, h * 0.9f, w, h, col);
            int n = 6;
            float step = w / (float)n;
            for (int i = 0; i < n; i++)
            {
                float x = i * step + step * 0.3f;
                float pw = Rand(rng, 8, 14);
                var c = i % 2 == 0 ? col : col2;
                r.FillRect(x, 0, x + pw, h, c);
                r.FillRect(x - 3, 0, x + pw + 3, 6, c);
                r.FillRect(x - 3, h - 8, x + pw + 3, h, c);
                r.FillRect(x + pw * 0.4f, 6, x + pw * 0.4f + 1, h - 8, Raster.Lighten(c, 0.15f));
                if (rng.NextDouble() > 0.5)
                {
                    // torch glow between pillars
                    float gx = x + pw + step * 0.35f;
                    r.FillCircle(gx, h * 0.55f, 5, new Color(1f, 0.7f, 0.3f, 0.35f));
                    r.FillCircle(gx, h * 0.55f, 2, new Color(1f, 0.85f, 0.4f, 0.9f));
                    r.FillRect(gx - 1, h * 0.35f, gx + 1, h * 0.53f, Raster.Darken(c, 0.2f));
                }
            }
        }

        static void Cave(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            r.FillRect(0, h * 0.85f, w, h, col);
            for (int i = 0; i < 18; i++)
            {
                float x = Rand(rng, 0, w);
                float len = Rand(rng, h * 0.1f, h * 0.45f);
                float sw = Rand(rng, 3, 9);
                r.FillTriangle(new Vector2(x - sw, h * 0.86f), new Vector2(x + sw, h * 0.86f), new Vector2(x, h * 0.86f - len), i % 2 == 0 ? col : col2);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = Rand(rng, 0, w);
                float len = Rand(rng, h * 0.08f, h * 0.35f);
                float sw = Rand(rng, 4, 10);
                r.FillTriangle(new Vector2(x - sw, 0), new Vector2(x + sw, 0), new Vector2(x, len), i % 2 == 0 ? col : col2);
            }
        }

        static void Crystals(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            for (int i = 0; i < 16; i++)
            {
                float x = Rand(rng, 0, w);
                float ch = Rand(rng, h * 0.2f, h * 0.7f);
                float cw = Rand(rng, 3, 8);
                float lean = Rand(rng, -6, 6);
                var c = i % 2 == 0 ? col : col2;
                r.FillPolygon(new[] { new Vector2(x - cw, 0), new Vector2(x + cw, 0), new Vector2(x + lean + cw * 0.5f, ch * 0.85f), new Vector2(x + lean, ch) }, c);
                r.FillPolygon(new[] { new Vector2(x - cw * 0.3f, 0), new Vector2(x + cw * 0.1f, 0), new Vector2(x + lean, ch * 0.9f) }, Raster.Lighten(c, 0.4f));
            }
        }

        static void Clouds(Raster r, System.Random rng, Color col, Color col2)
        {
            int w = r.width, h = r.height;
            for (int i = 0; i < 9; i++)
            {
                float x = Rand(rng, 0, w);
                float y = Rand(rng, h * 0.2f, h * 0.9f);
                float cw = Rand(rng, 14, 30);
                var c = Raster.Alpha(i % 2 == 0 ? col : col2, 0.85f);
                r.FillEllipse(x, y, cw, cw * 0.35f, c);
                r.FillEllipse(x - cw * 0.4f, y + cw * 0.15f, cw * 0.45f, cw * 0.3f, c);
                r.FillEllipse(x + cw * 0.3f, y + cw * 0.2f, cw * 0.5f, cw * 0.35f, c);
            }
        }
    }
}
