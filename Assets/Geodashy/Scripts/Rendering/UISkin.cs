using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Procedural nine-slice sprites for the interface: riveted iron frames, parchment cards, bevelled stone
    /// buttons, inset fields, ribbon banners and wax seals. Everything is greyscale-ish so the widget tint
    /// (button colour, accent) multiplies through.
    /// </summary>
    public static class UISkin
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static uint seed = 7;
        static float Noise()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return (seed & 0xFFFF) / 65535f;
        }

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            s.name = "skin_" + key;
            cache[key] = s;
            return s;
        }

        /// <summary>Dark iron plate with a bevelled lip and a rivet in each corner. Border 12 of 48.</summary>
        public static Sprite Iron() => Cached("iron", () =>
        {
            const int n = 48;
            var r = new Raster(n, n);
            var body = new Color(0.16f, 0.14f, 0.19f, 1f);
            r.Clear(body);
            // subtle brushed-metal grain
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float g = (Noise() - 0.5f) * 0.05f;
                    var c = r.Get(x, y);
                    r.Plot(x, y, new Color(c.r + g, c.g + g, c.b + g, 1f));
                }
            // bevel: light top/left, dark bottom/right
            r.FillRect(0, n - 3, n, n, new Color(0.34f, 0.31f, 0.38f, 1f));
            r.FillRect(0, 0, 3, n, new Color(0.30f, 0.27f, 0.34f, 1f));
            r.FillRect(0, 0, n, 3, new Color(0.07f, 0.06f, 0.09f, 1f));
            r.FillRect(n - 3, 0, n, n, new Color(0.08f, 0.07f, 0.1f, 1f));
            // inner line
            r.RectOutline(5, 5, n - 5, n - 5, 1, new Color(0.24f, 0.21f, 0.28f, 1f));
            // corner rivets
            foreach (var p in new[] { new Vector2(8, 8), new Vector2(n - 8, 8), new Vector2(8, n - 8), new Vector2(n - 8, n - 8) })
            {
                r.FillCircle(p.x, p.y, 2.6f, new Color(0.45f, 0.42f, 0.48f, 1f));
                r.FillCircle(p.x - 0.7f, p.y + 0.7f, 1.2f, new Color(0.7f, 0.68f, 0.72f, 1f));
                r.FillCircle(p.x + 0.8f, p.y - 0.8f, 1f, new Color(0.2f, 0.18f, 0.24f, 1f));
            }
            return r.ToSprite(64f, null, new Vector4(12, 12, 12, 12), FilterMode.Point);
        });

        /// <summary>Aged parchment with fibre noise and a darker, slightly burnt edge. Border 10 of 48.</summary>
        public static Sprite Parchment() => Cached("parchment", () =>
        {
            const int n = 48;
            var r = new Raster(n, n);
            var baseC = new Color(0.93f, 0.86f, 0.68f, 1f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float g = (Noise() - 0.5f) * 0.07f;
                    // darker toward every edge
                    float ex = Mathf.Min(x, n - 1 - x), ey = Mathf.Min(y, n - 1 - y);
                    float e = Mathf.Clamp01(Mathf.Min(ex, ey) / 9f);
                    float dark = Mathf.Lerp(0.72f, 1f, e);
                    r.Plot(x, y, new Color(baseC.r * dark + g, baseC.g * dark + g, baseC.b * dark + g * 0.6f, 1f));
                }
            r.RectOutline(0, 0, n, n, 1, new Color(0.45f, 0.32f, 0.16f, 1f));
            r.RectOutline(2, 2, n - 2, n - 2, 1, new Color(0.62f, 0.48f, 0.26f, 0.7f));
            return r.ToSprite(64f, null, new Vector4(10, 10, 10, 10), FilterMode.Point);
        });

        /// <summary>Bevelled stone button plate in light grey so button colours tint it. Border 6 of 24.</summary>
        public static Sprite Plate() => Cached("plate", () =>
        {
            const int n = 24;
            var r = new Raster(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float g = 0.92f + (Noise() - 0.5f) * 0.06f + (y / (float)n) * 0.1f;
                    r.Plot(x, y, new Color(g, g, g, 1f));
                }
            r.FillRect(0, n - 2, n, n, new Color(1.0f, 1.0f, 1.0f, 1f));
            r.FillRect(0, 0, 2, n, new Color(0.98f, 0.98f, 0.98f, 1f));
            r.FillRect(0, 0, n, 2, new Color(0.55f, 0.55f, 0.55f, 1f));
            r.FillRect(n - 2, 0, n, n, new Color(0.6f, 0.6f, 0.6f, 1f));
            // rounded corners cut out
            r.Set(0, 0, Color.clear); r.Set(n - 1, 0, Color.clear); r.Set(0, n - 1, Color.clear); r.Set(n - 1, n - 1, Color.clear);
            return r.ToSprite(64f, null, new Vector4(6, 6, 6, 6), FilterMode.Point);
        });

        /// <summary>Dark inset field with a thin brass rim. Border 4 of 16.</summary>
        public static Sprite Inset() => Cached("inset", () =>
        {
            const int n = 16;
            var r = new Raster(n, n);
            r.Clear(new Color(0.06f, 0.05f, 0.08f, 1f));
            r.RectOutline(0, 0, n, n, 1, new Color(0.5f, 0.4f, 0.2f, 0.9f));
            r.FillRect(1, n - 2, n - 1, n - 1, new Color(0.02f, 0.02f, 0.03f, 1f));   // shadow under the top rim
            return r.ToSprite(64f, null, new Vector4(4, 4, 4, 4), FilterMode.Point);
        });

        /// <summary>Ribbon banner for section headers: a swallow-tailed strip. Border 12 left/right of 64.</summary>
        public static Sprite Ribbon() => Cached("ribbon", () =>
        {
            const int w = 64, h = 20;
            var r = new Raster(w, h) { punch = true };
            var red = new Color(0.55f, 0.13f, 0.14f, 1f);
            var dark = new Color(0.36f, 0.08f, 0.1f, 1f);
            r.FillRect(8, 2, w - 8, h - 2, red);
            r.FillRect(8, h - 4, w - 8, h - 2, new Color(0.68f, 0.2f, 0.2f, 1f));
            r.FillRect(8, 2, w - 8, 4, dark);
            // swallow tails
            r.FillRect(0, 1, 9, h - 1, dark);
            r.FillRect(w - 9, 1, w, h - 1, dark);
            r.FillTriangle(new Vector2(-0.5f, 0.5f), new Vector2(-0.5f, h - 0.5f), new Vector2(5, h / 2f), Color.clear);
            r.FillTriangle(new Vector2(w + 0.5f, 0.5f), new Vector2(w + 0.5f, h - 0.5f), new Vector2(w - 5, h / 2f), Color.clear);
            return r.ToSprite(64f, null, new Vector4(12, 2, 12, 2), FilterMode.Point);
        });

        /// <summary>Wax seal for toggles: red blob with a darker rim.</summary>
        public static Sprite Seal() => Cached("seal", () =>
        {
            const int n = 32;
            var r = new Raster(n, n);
            float c = n / 2f;
            r.FillCircle(c + 0.5f, c - 0.7f, 14f, new Color(0.35f, 0.06f, 0.08f, 1f));
            r.FillCircle(c, c, 13.5f, new Color(0.62f, 0.12f, 0.12f, 1f));
            r.FillCircle(c - 3f, c + 3f, 5f, new Color(0.74f, 0.2f, 0.2f, 1f));
            r.Ring(c, c, 10f, 8.5f, new Color(0.45f, 0.08f, 0.1f, 1f));
            // the crest pressed into the wax
            r.FillCircle(c, c, 5f, new Color(0.85f, 0.68f, 0.28f, 1f));
            r.Ring(c, c, 4.2f, 3f, new Color(0.55f, 0.4f, 0.14f, 1f));
            r.FillRect(c - 0.8f, c - 3.2f, c + 0.8f, c + 3.2f, new Color(0.55f, 0.4f, 0.14f, 1f));
            return r.ToSprite(64f);
        });

        /// <summary>Round brass knob for slider handles.</summary>
        public static Sprite Knob() => Cached("knob", () =>
        {
            const int n = 24;
            var r = new Raster(n, n);
            float c = n / 2f;
            r.FillCircle(c, c - 0.5f, 10.5f, new Color(0.45f, 0.33f, 0.12f, 1f));
            r.FillCircle(c, c, 9.5f, new Color(0.85f, 0.68f, 0.28f, 1f));
            r.FillCircle(c - 2.5f, c + 2.5f, 3.5f, new Color(1f, 0.9f, 0.6f, 1f));
            return r.ToSprite(64f);
        });

        /// <summary>Banner-shaped bar with iron end caps for the play HUD progress bar. Border 14 of 64 wide.</summary>
        public static Sprite BannerBar() => Cached("bannerbar", () =>
        {
            const int w = 64, h = 24;
            var r = new Raster(w, h);
            var cloth = new Color(0.2f, 0.12f, 0.1f, 0.85f);
            r.FillRect(10, 3, w - 10, h - 3, cloth);
            r.FillRect(10, h - 5, w - 10, h - 3, new Color(0.35f, 0.22f, 0.15f, 0.9f));
            // iron end caps
            r.FillRoundRect(0, 0, 12, h, 3, new Color(0.3f, 0.28f, 0.34f, 1f));
            r.FillRoundRect(w - 12, 0, w, h, 3, new Color(0.3f, 0.28f, 0.34f, 1f));
            r.FillRect(2, 2, 10, h - 2, new Color(0.18f, 0.16f, 0.22f, 1f));
            r.FillRect(w - 10, 2, w - 2, h - 2, new Color(0.18f, 0.16f, 0.22f, 1f));
            r.FillCircle(6, h / 2f, 2f, new Color(0.6f, 0.58f, 0.62f, 1f));
            r.FillCircle(w - 6, h / 2f, 2f, new Color(0.6f, 0.58f, 0.62f, 1f));
            return r.ToSprite(64f, null, new Vector4(14, 4, 14, 4), FilterMode.Point);
        });
    }
}
