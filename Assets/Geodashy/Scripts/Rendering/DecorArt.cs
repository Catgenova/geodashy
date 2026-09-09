using System;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Procedural art for the castle decoration families (stonework, ivy and plants, roofs and windows,
    /// furnishings, lights, yard and siege gear). Every piece is built from a small set of material painters
    /// (marble veins, slate layers, granite speckle, cobbles, planks…) masked into silhouettes, so hundreds of
    /// palette entries share a few dozen routines and still read as different objects.
    /// </summary>
    public static class DecorArt
    {
        // ---- deterministic randomness --------------------------------------------------

        struct Rng
        {
            uint s;
            public Rng(int seed)
            {
                s = (uint)seed * 2654435761u + 0x9E3779B9u;
                if (s == 0) s = 1;
            }
            public float Next()
            {
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                return (s & 0xFFFFFF) / 16777216f;
            }
            public float Range(float a, float b) => a + (b - a) * Next();
            public int Int(int n) => n <= 0 ? 0 : Mathf.Min(n - 1, (int)(Next() * n));
        }

        static int Hash(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                foreach (var ch in s) h = (h ^ ch) * 16777619;
                return h;
            }
        }

        static readonly Color Transparent = new Color(0, 0, 0, 0);
        static readonly Color White = Color.white;
        static Color Hex(string h) => ObjectCatalog.Hex(h);
        static Color A(Color c, float a) => Raster.Alpha(c, a);
        static Color D(Color c, float a) => Raster.Darken(c, a);
        static Color L(Color c, float a) => Raster.Lighten(c, a);
        static Vector2 V(float x, float y) => new Vector2(x, y);

        static readonly Color Moss = Hex("5f8a3a");
        static readonly Color Flame = Hex("ffb03a");
        static readonly Color FlameCore = Hex("fff2a0");
        static readonly Color IronC = Hex("4a4c56");
        static readonly Color GoldC = Hex("e0b64a");
        static readonly Color WoodC = Hex("a9743d");
        static readonly Color WoodD = Hex("5a3a1a");

        // ---- materials ------------------------------------------------------------------

        public static Color BaseColor(DecoMaterial m)
        {
            switch (m)
            {
                case DecoMaterial.Marble: return Hex("e9e4dc");
                case DecoMaterial.Slate: return Hex("5a6470");
                case DecoMaterial.Granite: return Hex("8c8a86");
                case DecoMaterial.Sandstone: return Hex("d2b184");
                case DecoMaterial.Limestone: return Hex("d9d2bd");
                case DecoMaterial.Basalt: return Hex("3a3b40");
                case DecoMaterial.Cobble: return Hex("8a847c");
                case DecoMaterial.MossyStone: return Hex("7f8a7a");
                case DecoMaterial.Oak: return Hex("a9743d");
                case DecoMaterial.DarkWood: return Hex("5a3a1a");
                case DecoMaterial.Iron: return Hex("4a4c56");
                case DecoMaterial.Gold: return Hex("e0b64a");
                case DecoMaterial.Bronze: return Hex("a8702e");
                case DecoMaterial.Copper: return Hex("6fae8f");
                case DecoMaterial.Plaster: return Hex("efe7d6");
                case DecoMaterial.Terracotta: return Hex("c2643a");
                case DecoMaterial.Thatch: return Hex("c9a458");
                case DecoMaterial.Lead: return Hex("6c6f78");
                case DecoMaterial.Ivy: return Hex("3f8a3a");
                case DecoMaterial.Brick: return Hex("9a5a48");
            }
            return Color.gray;
        }

        /// <summary>Paints a w×h raster full of a material; seed varies the pattern.</summary>
        public static Raster Material(int w, int h, DecoMaterial m, int seed, Color? baseOverride = null)
        {
            var r = new Raster(w, h);
            var b = baseOverride ?? BaseColor(m);
            var d = D(b, 0.3f);
            var l = L(b, 0.28f);
            var rng = new Rng(seed);
            r.Clear(b);
            switch (m)
            {
                case DecoMaterial.Marble:
                    for (int i = 0; i < 2 + w * h / 1500; i++)
                    {
                        float x0 = rng.Range(0, w), y0 = rng.Range(0, h);
                        float x1 = x0 + rng.Range(-w * 0.6f, w * 0.6f), y1 = y0 + rng.Range(-h * 0.5f, h * 0.5f);
                        float xm = (x0 + x1) / 2f + rng.Range(-6, 6), ym = (y0 + y1) / 2f + rng.Range(-6, 6);
                        r.Line(x0, y0, xm, ym, 1.2f, A(Hex("9a938c"), 0.55f));
                        r.Line(xm, ym, x1, y1, 1.2f, A(Hex("9a938c"), 0.55f));
                        if (i % 2 == 0) r.Line(x0 + 3, y0 + 2, xm + 3, ym + 2, 0.8f, A(Hex("c9b27a"), 0.35f));
                    }
                    r.FillRect(0, 0, w, h, A(White, 0.05f));
                    break;
                case DecoMaterial.Slate:
                    for (int y = 3; y < h; y += 6)
                    {
                        float off = rng.Range(0, 10);
                        r.FillRect(0, y, w, y + 1, A(d, 0.6f));
                        r.FillRect(off, y + 2, off + w * 0.6f, y + 3, A(l, 0.25f));
                    }
                    break;
                case DecoMaterial.Granite:
                    for (int i = 0; i < w * h / 12; i++)
                    {
                        float x = rng.Range(0, w), y = rng.Range(0, h);
                        var c = rng.Next() < 0.5f ? A(d, 0.7f) : A(l, 0.7f);
                        if (rng.Next() < 0.2f) c = A(Hex("c9a0a0"), 0.5f);
                        r.FillRect(x, y, x + rng.Range(1, 2.5f), y + rng.Range(1, 2f), c);
                    }
                    break;
                case DecoMaterial.Sandstone:
                    for (int y = 4; y < h; y += 10)
                    {
                        r.FillRect(0, y, w, y + 2, A(l, 0.35f));
                        r.FillRect(0, y + 5, w, y + 6, A(d, 0.25f));
                    }
                    for (int i = 0; i < w * h / 200; i++) r.FillCircle(rng.Range(0, w), rng.Range(0, h), 1.2f, A(d, 0.35f));
                    break;
                case DecoMaterial.Limestone:
                case DecoMaterial.MossyStone:
                {
                    int rowH = 16;
                    for (int row = 0; row * rowH < h; row++)
                    {
                        float y = row * rowH;
                        r.FillRect(0, y, w, y + 1.5f, A(d, 0.55f));
                        float off = (row % 2) * 12f;
                        for (float x = off; x < w; x += 24f) r.FillRect(x, y, x + 1.5f, y + rowH, A(d, 0.55f));
                        r.FillRect(0, y + rowH - 2, w, y + rowH - 1, A(l, 0.3f));
                    }
                    if (m == DecoMaterial.MossyStone)
                    {
                        for (int i = 0; i < 3 + w * h / 700; i++)
                        {
                            float x = rng.Range(0, w), y = rng.Next() < 0.6f ? rng.Range(0, h * 0.4f) : rng.Range(h * 0.7f, h);
                            r.FillEllipse(x, y, rng.Range(4, 9), rng.Range(2, 5), A(Moss, 0.75f));
                            r.FillEllipse(x + 2, y + 1, rng.Range(2, 4), rng.Range(1, 2.5f), A(L(Moss, 0.3f), 0.6f));
                        }
                    }
                    break;
                }
                case DecoMaterial.Basalt:
                    for (float x = 5; x < w; x += 11) r.FillRect(x, 0, x + 1.5f, h, A(D(b, 0.6f), 0.8f));
                    for (int i = 0; i < w * h / 300; i++)
                    {
                        float x = rng.Range(0, w), y = rng.Range(0, h);
                        r.FillRect(x, y, x + 6, y + 1, A(L(b, 0.2f), 0.6f));
                    }
                    break;
                case DecoMaterial.Cobble:
                {
                    r.Clear(D(b, 0.45f));
                    float cell = 13f;
                    for (int row = 0; row * cell * 0.85f < h + cell; row++)
                    for (float x = (row % 2) * cell / 2f - cell; x < w + cell; x += cell)
                    {
                        float cx = x + rng.Range(-1.5f, 1.5f), cy = row * cell * 0.85f + rng.Range(-1.5f, 1.5f);
                        var c = Color.Lerp(d, l, rng.Next());
                        r.FillEllipse(cx, cy, cell * 0.45f, cell * 0.36f, c);
                        r.FillEllipse(cx - 1, cy + 1.5f, cell * 0.2f, cell * 0.12f, A(White, 0.18f));
                    }
                    break;
                }
                case DecoMaterial.Oak:
                case DecoMaterial.DarkWood:
                {
                    float pw = 14f;
                    for (float x = 0; x < w; x += pw)
                    {
                        r.FillRect(x, 0, x + 1.5f, h, A(D(b, 0.5f), 0.9f));
                        for (int k = 0; k < 3; k++)
                        {
                            float gx = x + rng.Range(3, pw - 3);
                            r.Line(gx, 0, gx + rng.Range(-2, 2), h, 0.8f, A(d, 0.35f));
                        }
                        r.FillRect(x + 2, 0, x + 3, h, A(l, 0.25f));
                    }
                    break;
                }
                case DecoMaterial.Iron:
                case DecoMaterial.Lead:
                    r.FillRect(0, h * 0.55f, w, h * 0.62f, A(l, 0.18f));
                    for (float x = 8; x < w; x += 16)
                    for (float y = 8; y < h; y += 16)
                    {
                        if (m == DecoMaterial.Iron)
                        {
                            r.FillCircle(x, y, 2.2f, D(b, 0.5f));
                            r.FillCircle(x - 0.5f, y + 0.5f, 1.2f, L(b, 0.3f));
                        }
                    }
                    if (m == DecoMaterial.Lead) for (float x = 12; x < w; x += 12) r.FillRect(x, 0, x + 1.2f, h, A(d, 0.6f));
                    break;
                case DecoMaterial.Gold:
                case DecoMaterial.Bronze:
                    for (int y = 0; y < h; y++) r.FillRect(0, y, w, y + 1, A(Color.Lerp(d, l, Mathf.PingPong(y / 18f, 1f)), 0.35f));
                    r.Line(0, h * 0.2f, w, h * 0.8f, 3f, A(White, 0.2f));
                    break;
                case DecoMaterial.Copper:
                    for (int i = 0; i < w * h / 250; i++) r.FillEllipse(rng.Range(0, w), rng.Range(0, h), rng.Range(3, 7), rng.Range(2, 4), A(Hex("8fc7a3"), 0.5f));
                    for (float y = 10; y < h; y += 14) r.FillRect(0, y, w, y + 1, A(d, 0.5f));
                    break;
                case DecoMaterial.Plaster:
                    for (int i = 0; i < 2 + w * h / 2500; i++)
                    {
                        float x = rng.Range(0, w), y = rng.Range(0, h);
                        r.Line(x, y, x + rng.Range(-10, 10), y + rng.Range(-14, 14), 0.8f, A(d, 0.45f));
                    }
                    r.FillRect(0, 0, w, h, A(d, 0.06f));
                    break;
                case DecoMaterial.Terracotta:
                    for (int row = 0; row * 10 < h + 10; row++)
                    for (float x = (row % 2) * 8f - 8f; x < w + 8; x += 16f)
                    {
                        float y = row * 10f;
                        r.FillEllipse(x + 8, y, 8, 5, A(d, 0.7f));
                        r.FillEllipse(x + 8, y + 1.5f, 6.5f, 3.5f, b);
                    }
                    break;
                case DecoMaterial.Thatch:
                    for (int i = 0; i < w * h / 25; i++)
                    {
                        float x = rng.Range(0, w), y = rng.Range(0, h);
                        r.Line(x, y, x + rng.Range(-2, 2), y - rng.Range(4, 9), 0.9f, rng.Next() < 0.5f ? A(d, 0.5f) : A(l, 0.5f));
                    }
                    break;
                case DecoMaterial.Ivy:
                    r.Clear(D(b, 0.35f));
                    for (int i = 0; i < w * h / 40; i++)
                    {
                        float x = rng.Range(0, w), y = rng.Range(0, h);
                        Leaf(r, x, y, rng.Range(3, 5), Color.Lerp(d, l, rng.Next()), rng.Range(-0.5f, 0.5f));
                    }
                    break;
                case DecoMaterial.Brick:
                {
                    var mortar = Hex("b9aea0");
                    r.Clear(mortar);
                    float bh = 8f, bw = 18f;
                    for (int row = 0; row * bh < h; row++)
                    for (float x = (row % 2) * bw / 2f - bw; x < w; x += bw)
                    {
                        float y = row * bh;
                        r.FillRect(x + 1, y + 1, x + bw - 1, y + bh - 1, Color.Lerp(b, d, rng.Next() * 0.5f));
                    }
                    break;
                }
            }
            return r;
        }

        /// <summary>A leaf: a small circle with a pointed tip, tilted by 'tilt' (-1..1).</summary>
        static void Leaf(Raster r, float x, float y, float size, Color c, float tilt)
        {
            r.FillCircle(x, y, size * 0.6f, c);
            r.FillTriangle(V(x - size * 0.6f, y), V(x + size * 0.6f, y), V(x + tilt * size, y - size * 1.1f), c);
        }

        static void Blit(Raster dst, Raster src, int ox, int oy)
        {
            for (int y = 0; y < src.height; y++)
            for (int x = 0; x < src.width; x++)
            {
                var c = src.pixels[y * src.width + x];
                if (c.a == 0) continue;
                dst.Plot(x + ox, y + oy, c);
            }
        }

        /// <summary>Copies src into dst where the mask (same size as dst) is opaque.</summary>
        static void BlitMasked(Raster dst, Raster src, Raster mask)
        {
            for (int y = 0; y < dst.height && y < src.height; y++)
            for (int x = 0; x < dst.width && x < src.width; x++)
            {
                var m = mask.pixels[y * mask.width + x];
                if (m.a == 0) continue;
                var c = src.pixels[y * src.width + x];
                if (m.a < 255) c.a = (byte)(c.a * m.a / 255);
                dst.Plot(x, y, c);
            }
        }

        static Raster NewMask(int w, int h)
        {
            var m = new Raster(w, h);
            m.Clear(Transparent);
            return m;
        }

        /// <summary>Removes (makes transparent) every pixel of 'mask' that is opaque in 'cut'.</summary>
        static void Subtract(Raster mask, Raster cut)
        {
            for (int i = 0; i < mask.pixels.Length && i < cut.pixels.Length; i++) if (cut.pixels[i].a > 0) mask.pixels[i] = new Color32(0, 0, 0, 0);
        }

        static void ClearRect(Raster mask, float x0, float y0, float x1, float y1)
        {
            int ax = Mathf.Max(0, Mathf.FloorToInt(x0)), bx = Mathf.Min(mask.width, Mathf.CeilToInt(x1));
            int ay = Mathf.Max(0, Mathf.FloorToInt(y0)), by = Mathf.Min(mask.height, Mathf.CeilToInt(y1));
            for (int y = ay; y < by; y++)
            for (int x = ax; x < bx; x++) mask.pixels[y * mask.width + x] = new Color32(0, 0, 0, 0);
        }

        /// <summary>Fills a rectangle of the target with material.</summary>
        static void MatRect(Raster r, DecoMaterial m, float x0, float y0, float x1, float y1, int seed, Color? tint = null)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(x1 - x0)), h = Mathf.Max(1, Mathf.RoundToInt(y1 - y0));
            var src = Material(w, h, m, seed, tint);
            Blit(r, src, Mathf.RoundToInt(x0), Mathf.RoundToInt(y0));
        }

        /// <summary>Fills the opaque part of a full-size mask with material.</summary>
        static void MatMask(Raster r, DecoMaterial m, Raster mask, int seed, Color? tint = null)
        {
            var src = Material(r.width, r.height, m, seed, tint);
            BlitMasked(r, src, mask);
        }

        /// <summary>Darkens the outer edge of every opaque region so pieces read against any background.</summary>
        static void Outline(Raster r, float strength)
        {
            int w = r.width, h = r.height;
            var src = (Color32[])r.pixels.Clone();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = src[y * w + x];
                if (c.a < 40) continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 ||
                            src[y * w + x - 1].a < 40 || src[y * w + x + 1].a < 40 || src[(y - 1) * w + x].a < 40 || src[(y + 1) * w + x].a < 40;
                if (!edge) continue;
                r.pixels[y * w + x] = (Color32)Color.Lerp(c, new Color(0.05f, 0.03f, 0.06f, c.a / 255f), strength);
            }
        }

        static void Shadow(Raster r, float x0, float y0, float x1, float y1, float a) => r.FillRect(x0, y0, x1, y1, A(Color.black, a));
        static void Highlight(Raster r, float x0, float y0, float x1, float y1, float a) => r.FillRect(x0, y0, x1, y1, A(White, a));

        static DecoMaterial Mat(ObjectDefinition def, DecoMaterial fallback) => def.material == DecoMaterial.None ? fallback : def.material;

        // ---- dispatcher ------------------------------------------------------------------

        /// <summary>Draws the definition when it belongs to a decoration family; false leaves it to the classic placeholders.</summary>
        public static bool TryDraw(Raster r, ObjectDefinition def)
        {
            int seed = Hash(def.id);
            r.Clear(Transparent);
            switch (def.shape)
            {
                case PlaceholderShape.Facade: DrawFacade(r, def, seed); break;
                case PlaceholderShape.Column: DrawColumn(r, def, seed); break;
                case PlaceholderShape.ArchDeco: DrawArch(r, def, seed); break;
                case PlaceholderShape.Battlement: DrawBattlement(r, def, seed); break;
                case PlaceholderShape.Relief: DrawRelief(r, def, seed); break;
                case PlaceholderShape.Monument: DrawMonument(r, def, seed); break;
                case PlaceholderShape.Ivy: DrawIvy(r, def, seed); break;
                case PlaceholderShape.Foliage: DrawFoliage(r, def, seed); break;
                case PlaceholderShape.TreeDeco: DrawTree(r, def, seed); break;
                case PlaceholderShape.Roof: DrawRoof(r, def, seed); break;
                case PlaceholderShape.TowerCap: DrawTowerCap(r, def, seed); break;
                case PlaceholderShape.WindowDeco: DrawWindow(r, def, seed); break;
                case PlaceholderShape.Door: DrawDoor(r, def, seed); break;
                case PlaceholderShape.Furniture: DrawFurniture(r, def, seed); break;
                case PlaceholderShape.Tapestry: DrawTapestry(r, def, seed); break;
                case PlaceholderShape.Armoury: DrawArmoury(r, def, seed); break;
                case PlaceholderShape.Lantern: DrawLantern(r, def, seed); break;
                case PlaceholderShape.Yard: DrawYard(r, def, seed); break;
                case PlaceholderShape.Fence: DrawFence(r, def, seed); break;
                case PlaceholderShape.Siege: DrawSiege(r, def, seed); break;
                default: return false;
            }
            Outline(r, 0.45f);
            return true;
        }

        // ---- stonework ---------------------------------------------------------------------

        static void DrawFacade(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            MatRect(r, Mat(def, DecoMaterial.Granite), 0, 0, w, h, seed);
            Shadow(r, 0, 0, w, 2, 0.25f); Shadow(r, w - 2, 0, w, h, 0.2f);
            Highlight(r, 0, h - 2, w, h, 0.18f); Highlight(r, 0, 0, 2, h, 0.12f);
        }

        static void DrawColumn(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var m = Mat(def, DecoMaterial.Marble);
            var b = BaseColor(m);
            int v = def.variant;                       // 0 plain, 1 doric, 2 ionic, 3 corinthian, 4 twisted, 5 broken, 6 pilaster
            float sx0 = v == 6 ? w * 0.12f : w * 0.3f, sx1 = v == 6 ? w * 0.88f : w * 0.7f;
            float top = v == 5 ? h * 0.7f : h * 0.9f;
            // plinth
            MatRect(r, m, w * 0.08f, 0, w * 0.92f, h * 0.07f, seed + 1);
            MatRect(r, m, w * 0.16f, h * 0.07f, w * 0.84f, h * 0.11f, seed + 2);
            // shaft
            if (v == 5)
            {
                var mask = NewMask(w, h);
                mask.FillRect(sx0, h * 0.11f, sx1, top, White);
                var rng = new Rng(seed);
                for (float x = sx0; x < sx1; x += 6) mask.FillTriangle(V(x, top), V(x + 6, top), V(x + 3, top - rng.Range(4, 12)), White);
                mask.FillRect(sx0, top - 2, sx1, top, White);
                var cut = NewMask(w, h);
                for (float x = sx0; x < sx1; x += 6) cut.FillTriangle(V(x - 3, top + 1), V(x + 3, top + 1), V(x, top - rng.Range(3, 10)), White);
                Subtract(mask, cut);
                MatMask(r, m, mask, seed + 3);
            }
            else MatRect(r, m, sx0, h * 0.11f, sx1, top, seed + 3);
            // shading down the shaft
            Shadow(r, sx1 - (sx1 - sx0) * 0.22f, h * 0.11f, sx1, top, 0.22f);
            Highlight(r, sx0 + 2, h * 0.11f, sx0 + (sx1 - sx0) * 0.18f, top, 0.16f);
            if (v == 1 || v == 3 || v == 6)   // fluting
                for (float x = sx0 + 6; x < sx1 - 3; x += 7) r.FillRect(x, h * 0.12f, x + 1.5f, top - 1, A(D(b, 0.45f), 0.55f));
            if (v == 4)   // twisted: diagonal bands
            {
                for (float y = h * 0.11f - 10; y < top; y += 12)
                {
                    r.Line(sx0, y, sx1, y + 10, 3f, A(D(b, 0.45f), 0.5f));
                    r.Line(sx0, y + 4, sx1, y + 14, 1.5f, A(L(b, 0.4f), 0.5f));
                }
            }
            if (v == 5)
            {
                // fallen drum on the plinth
                r.FillEllipse(w * 0.72f, h * 0.15f, w * 0.2f, h * 0.05f, D(b, 0.25f));
                return;
            }
            // capital
            MatRect(r, m, w * 0.2f, top, w * 0.8f, h * 0.95f, seed + 4);
            MatRect(r, m, w * 0.1f, h * 0.95f, w * 0.9f, h, seed + 5);
            if (v == 2)
            {
                r.Ring(w * 0.2f, h * 0.92f, w * 0.13f, w * 0.06f, D(b, 0.4f));
                r.Ring(w * 0.8f, h * 0.92f, w * 0.13f, w * 0.06f, D(b, 0.4f));
                r.FillCircle(w * 0.2f, h * 0.92f, w * 0.05f, L(b, 0.2f));
                r.FillCircle(w * 0.8f, h * 0.92f, w * 0.05f, L(b, 0.2f));
            }
            if (v == 3)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = w * (0.26f + i * 0.16f);
                    Leaf(r, x, h * 0.89f, w * 0.07f, D(b, 0.35f), (i % 2 == 0) ? -0.4f : 0.4f);
                    Leaf(r, x + 3, h * 0.93f, w * 0.05f, L(b, 0.1f), 0.2f);
                }
            }
            if (v == 0) Shadow(r, w * 0.2f, top, w * 0.8f, top + 2, 0.3f);
        }

        static void DrawArch(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var m = Mat(def, DecoMaterial.Granite);
            var b = BaseColor(m);
            int v = def.variant;   // 0 round, 1 pointed, 2 keystone, 3 broken
            float pier = w * 0.2f, spring = h * 0.5f;
            var mask = NewMask(w, h);
            mask.FillRect(0, 0, pier, spring + 2, White);
            mask.FillRect(w - pier, 0, w, spring + 2, White);
            if (v == 1)
            {
                mask.FillPolygon(new[] { V(0, spring), V(0, spring + h * 0.12f), V(cx, h), V(w, spring + h * 0.12f), V(w, spring) }, White);
                var cut = NewMask(w, h);
                cut.FillPolygon(new[] { V(pier, spring - 1), V(pier, spring + h * 0.1f), V(cx, h - pier * 0.95f), V(w - pier, spring + h * 0.1f), V(w - pier, spring - 1) }, White);
                Subtract(mask, cut);
            }
            else
            {
                mask.FillEllipse(cx, spring, w * 0.5f, h * 0.5f, White);
                var cut = NewMask(w, h);
                cut.FillEllipse(cx, spring, w * 0.5f - pier, h * 0.5f - pier * 0.9f, White);
                Subtract(mask, cut);
                ClearRect(mask, pier, 0, w - pier, spring);
                if (v == 3) ClearRect(mask, cx + w * 0.08f, spring + h * 0.15f, w, h);
            }
            MatMask(r, m, mask, seed);
            // voussoir joints
            var joint = A(D(b, 0.5f), 0.6f);
            for (int i = 1; i < 8; i++)
            {
                float a = Mathf.PI * i / 8f;
                float x0 = cx + Mathf.Cos(a) * (w * 0.5f - pier), y0 = spring + Mathf.Sin(a) * (h * 0.5f - pier * 0.9f);
                float x1 = cx + Mathf.Cos(a) * w * 0.5f, y1 = spring + Mathf.Sin(a) * h * 0.5f;
                if (v == 3 && x0 > cx + w * 0.08f && y0 > spring + h * 0.15f) continue;
                r.Line(x0, y0, x1, y1, 1.5f, joint);
            }
            for (float y = spring * 0.25f; y < spring; y += spring * 0.25f)
            {
                r.FillRect(0, y, pier, y + 1.5f, joint);
                r.FillRect(w - pier, y, w, y + 1.5f, joint);
            }
            if (v == 2)
            {
                r.FillPolygon(new[] { V(cx - w * 0.07f, h * 0.78f), V(cx + w * 0.07f, h * 0.78f), V(cx + w * 0.1f, h), V(cx - w * 0.1f, h) }, L(b, 0.25f));
                r.RectOutline(cx - w * 0.1f, h * 0.78f, cx + w * 0.1f, h, 1.5f, D(b, 0.4f));
            }
            if (v == 3)
            {
                r.FillEllipse(w * 0.8f, h * 0.05f, w * 0.12f, h * 0.05f, D(b, 0.2f));
                r.FillEllipse(w * 0.66f, h * 0.04f, w * 0.08f, h * 0.035f, D(b, 0.1f));
                r.FillCircle(w * 0.9f, h * 0.1f, w * 0.05f, D(b, 0.15f));
            }
            Shadow(r, 0, 0, pier, 3, 0.3f); Shadow(r, w - pier, 0, w, 3, 0.3f);
        }

        static void DrawBattlement(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            var m = Mat(def, DecoMaterial.Granite);
            var b = BaseColor(m);
            int v = def.variant;   // 0 merlon, 1 crenellation strip, 2 corbel, 3 machicolation, 4 parapet
            switch (v)
            {
                case 0:
                    MatRect(r, m, w * 0.05f, 0, w * 0.95f, h * 0.88f, seed);
                    MatRect(r, m, 0, h * 0.88f, w, h, seed + 1);
                    Highlight(r, 0, h - 2, w, h, 0.25f);
                    r.FillRect(w * 0.42f, h * 0.3f, w * 0.58f, h * 0.7f, D(b, 0.55f));   // arrow slit
                    break;
                case 1:
                {
                    MatRect(r, m, 0, 0, w, h * 0.45f, seed);
                    int n = Mathf.Max(2, Mathf.RoundToInt(w / 40f));
                    float unit = w / (n * 2f - 1f);
                    for (int i = 0; i < n; i++) MatRect(r, m, i * unit * 2f, h * 0.45f, i * unit * 2f + unit, h, seed + i);
                    Highlight(r, 0, h * 0.45f - 2, w, h * 0.45f, 0.2f);
                    break;
                }
                case 2:
                    MatRect(r, m, 0, h * 0.6f, w, h, seed);
                    {
                        var mask = NewMask(w, h);
                        mask.FillPolygon(new[] { V(w * 0.2f, 0), V(w * 0.8f, 0), V(w, h * 0.6f), V(0, h * 0.6f) }, White);
                        MatMask(r, m, mask, seed + 1);
                        r.FillRect(0, h * 0.3f, w, h * 0.3f + 1.5f, A(D(b, 0.5f), 0.6f));
                    }
                    break;
                case 3:
                    MatRect(r, m, 0, h * 0.55f, w, h, seed);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = w * (0.1f + i * 0.3f), x1 = x + w * 0.2f;
                        MatRect(r, m, x, 0, x1, h * 0.55f, seed + i);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        float x = w * (0.3f + i * 0.3f), x1 = x + w * 0.1f;
                        r.FillEllipse((x + x1) / 2f, h * 0.55f, w * 0.05f, h * 0.15f, D(b, 0.6f));
                    }
                    Shadow(r, 0, h * 0.55f, w, h * 0.58f, 0.3f);
                    break;
                default:
                    MatRect(r, m, 0, 0, w, h * 0.7f, seed);
                    MatRect(r, m, 0, h * 0.7f, w, h, seed + 1, L(b, 0.15f));
                    Highlight(r, 0, h - 2, w, h, 0.25f);
                    break;
            }
        }

        static void DrawRelief(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var m = Mat(def, DecoMaterial.Marble);
            var b = BaseColor(m);
            var ink = A(D(b, 0.5f), 0.85f);
            var lit = L(b, 0.25f);
            MatRect(r, m, 0, 0, w, h, seed);
            r.RectOutline(0, 0, w, h, 3, A(D(b, 0.35f), 0.8f));
            r.RectOutline(4, 4, w - 4, h - 4, 1.5f, A(lit, 0.7f));
            float s = Mathf.Min(w, h) * 0.5f;
            switch (def.variant)   // 0 shield, 1 lion, 2 gargoyle, 3 grotesque, 4 sun, 5 fleur, 6 swords, 7 crown
            {
                case 0:
                    r.FillPolygon(new[] { V(cx - s * 0.6f, cy + s * 0.6f), V(cx + s * 0.6f, cy + s * 0.6f), V(cx + s * 0.6f, cy), V(cx, cy - s * 0.7f), V(cx - s * 0.6f, cy) }, ink);
                    r.FillPolygon(new[] { V(cx - s * 0.45f, cy + s * 0.45f), V(cx + s * 0.45f, cy + s * 0.45f), V(cx + s * 0.45f, cy), V(cx, cy - s * 0.5f), V(cx - s * 0.45f, cy) }, lit);
                    r.FillRect(cx - s * 0.06f, cy - s * 0.45f, cx + s * 0.06f, cy + s * 0.45f, ink);
                    r.FillRect(cx - s * 0.45f, cy - s * 0.06f, cx + s * 0.45f, cy + s * 0.06f, ink);
                    break;
                case 1:
                    r.FillCircle(cx, cy, s * 0.62f, ink);
                    r.FillCircle(cx, cy, s * 0.45f, lit);
                    r.FillCircle(cx - s * 0.18f, cy + s * 0.12f, s * 0.07f, ink); r.FillCircle(cx + s * 0.18f, cy + s * 0.12f, s * 0.07f, ink);
                    r.FillTriangle(V(cx - s * 0.1f, cy - s * 0.05f), V(cx + s * 0.1f, cy - s * 0.05f), V(cx, cy - s * 0.2f), ink);
                    r.FillRect(cx - s * 0.2f, cy - s * 0.32f, cx + s * 0.2f, cy - s * 0.26f, ink);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        r.FillTriangle(V(cx + Mathf.Cos(a) * s * 0.55f, cy + Mathf.Sin(a) * s * 0.55f), V(cx + Mathf.Cos(a + 0.35f) * s * 0.55f, cy + Mathf.Sin(a + 0.35f) * s * 0.55f), V(cx + Mathf.Cos(a + 0.17f) * s * 0.85f, cy + Mathf.Sin(a + 0.17f) * s * 0.85f), ink);
                    }
                    break;
                case 2:
                    r.FillEllipse(cx, cy, s * 0.55f, s * 0.45f, ink);
                    r.FillEllipse(cx, cy + s * 0.02f, s * 0.42f, s * 0.32f, lit);
                    r.FillTriangle(V(cx - s * 0.5f, cy + s * 0.3f), V(cx - s * 0.2f, cy + s * 0.35f), V(cx - s * 0.55f, cy + s * 0.8f), ink);
                    r.FillTriangle(V(cx + s * 0.5f, cy + s * 0.3f), V(cx + s * 0.2f, cy + s * 0.35f), V(cx + s * 0.55f, cy + s * 0.8f), ink);
                    r.FillCircle(cx - s * 0.17f, cy + s * 0.1f, s * 0.08f, ink); r.FillCircle(cx + s * 0.17f, cy + s * 0.1f, s * 0.08f, ink);
                    r.FillRect(cx - s * 0.3f, cy - s * 0.2f, cx + s * 0.3f, cy - s * 0.12f, ink);
                    r.FillTriangle(V(cx - s * 0.22f, cy - s * 0.12f), V(cx - s * 0.1f, cy - s * 0.12f), V(cx - s * 0.16f, cy - s * 0.32f), lit);
                    r.FillTriangle(V(cx + s * 0.22f, cy - s * 0.12f), V(cx + s * 0.1f, cy - s * 0.12f), V(cx + s * 0.16f, cy - s * 0.32f), lit);
                    break;
                case 3:
                    r.FillCircle(cx, cy, s * 0.6f, ink);
                    r.FillCircle(cx, cy, s * 0.48f, lit);
                    r.FillEllipse(cx - s * 0.2f, cy + s * 0.12f, s * 0.12f, s * 0.08f, ink); r.FillEllipse(cx + s * 0.2f, cy + s * 0.12f, s * 0.12f, s * 0.08f, ink);
                    r.FillEllipse(cx, cy - s * 0.2f, s * 0.28f, s * 0.12f, ink);
                    r.FillEllipse(cx, cy - s * 0.17f, s * 0.2f, s * 0.05f, lit);
                    r.FillCircle(cx, cy - s * 0.02f, s * 0.07f, ink);
                    break;
                case 4:
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI / 6f;
                        r.FillTriangle(V(cx + Mathf.Cos(a - 0.15f) * s * 0.45f, cy + Mathf.Sin(a - 0.15f) * s * 0.45f), V(cx + Mathf.Cos(a + 0.15f) * s * 0.45f, cy + Mathf.Sin(a + 0.15f) * s * 0.45f), V(cx + Mathf.Cos(a) * s * 0.85f, cy + Mathf.Sin(a) * s * 0.85f), ink);
                    }
                    r.FillCircle(cx, cy, s * 0.48f, ink);
                    r.FillCircle(cx, cy, s * 0.38f, lit);
                    r.FillCircle(cx - s * 0.12f, cy + s * 0.08f, s * 0.05f, ink); r.FillCircle(cx + s * 0.12f, cy + s * 0.08f, s * 0.05f, ink);
                    r.FillEllipse(cx, cy - s * 0.12f, s * 0.14f, s * 0.05f, ink);
                    break;
                case 5:
                    r.FillPolygon(new[] { V(cx - s * 0.12f, cy - s * 0.1f), V(cx + s * 0.12f, cy - s * 0.1f), V(cx + s * 0.05f, cy + s * 0.8f), V(cx, cy + s * 0.9f), V(cx - s * 0.05f, cy + s * 0.8f) }, ink);
                    r.FillEllipse(cx - s * 0.35f, cy + s * 0.25f, s * 0.22f, s * 0.35f, ink);
                    r.FillEllipse(cx + s * 0.35f, cy + s * 0.25f, s * 0.22f, s * 0.35f, ink);
                    r.FillEllipse(cx - s * 0.32f, cy + s * 0.25f, s * 0.1f, s * 0.2f, lit);
                    r.FillEllipse(cx + s * 0.32f, cy + s * 0.25f, s * 0.1f, s * 0.2f, lit);
                    r.FillRect(cx - s * 0.4f, cy - s * 0.15f, cx + s * 0.4f, cy - s * 0.02f, ink);
                    r.FillPolygon(new[] { V(cx - s * 0.2f, cy - s * 0.15f), V(cx + s * 0.2f, cy - s * 0.15f), V(cx + s * 0.1f, cy - s * 0.7f), V(cx - s * 0.1f, cy - s * 0.7f) }, ink);
                    break;
                case 6:
                    r.Line(cx - s * 0.7f, cy - s * 0.7f, cx + s * 0.7f, cy + s * 0.7f, 4, ink);
                    r.Line(cx + s * 0.7f, cy - s * 0.7f, cx - s * 0.7f, cy + s * 0.7f, 4, ink);
                    r.Line(cx - s * 0.55f, cy - s * 0.35f, cx - s * 0.35f, cy - s * 0.55f, 4, ink);
                    r.Line(cx + s * 0.55f, cy - s * 0.35f, cx + s * 0.35f, cy - s * 0.55f, 4, ink);
                    r.FillCircle(cx - s * 0.66f, cy - s * 0.66f, s * 0.08f, ink); r.FillCircle(cx + s * 0.66f, cy - s * 0.66f, s * 0.08f, ink);
                    r.FillCircle(cx, cy, s * 0.12f, lit);
                    break;
                default:
                    r.FillPolygon(new[] { V(cx - s * 0.7f, cy - s * 0.35f), V(cx + s * 0.7f, cy - s * 0.35f), V(cx + s * 0.7f, cy + s * 0.5f), V(cx - s * 0.7f, cy + s * 0.5f) }, ink);
                    r.FillTriangle(V(cx - s * 0.7f, cy + s * 0.45f), V(cx - s * 0.25f, cy + s * 0.45f), V(cx - s * 0.7f, cy + s * 0.9f), ink);
                    r.FillTriangle(V(cx + s * 0.7f, cy + s * 0.45f), V(cx + s * 0.25f, cy + s * 0.45f), V(cx + s * 0.7f, cy + s * 0.9f), ink);
                    r.FillTriangle(V(cx - s * 0.3f, cy + s * 0.45f), V(cx + s * 0.3f, cy + s * 0.45f), V(cx, cy + s * 0.95f), ink);
                    r.FillRect(cx - s * 0.6f, cy - s * 0.25f, cx + s * 0.6f, cy - s * 0.1f, lit);
                    for (int i = -1; i <= 1; i++) r.FillCircle(cx + i * s * 0.4f, cy + s * 0.2f, s * 0.08f, lit);
                    break;
            }
        }

        static void DrawMonument(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var m = Mat(def, DecoMaterial.Granite);
            var b = BaseColor(m);
            var d = D(b, 0.45f);
            var rng = new Rng(seed);
            switch (def.variant)
            {
                case 0:   // obelisk
                {
                    MatRect(r, m, w * 0.1f, 0, w * 0.9f, h * 0.08f, seed);
                    var mask = NewMask(w, h);
                    mask.FillPolygon(new[] { V(w * 0.3f, h * 0.08f), V(w * 0.7f, h * 0.08f), V(w * 0.6f, h * 0.88f), V(w * 0.4f, h * 0.88f) }, White);
                    mask.FillTriangle(V(w * 0.4f, h * 0.88f), V(w * 0.6f, h * 0.88f), V(cx, h), White);
                    MatMask(r, m, mask, seed + 1);
                    Shadow(r, cx, h * 0.08f, w * 0.62f, h * 0.9f, 0.2f);
                    for (int i = 0; i < 4; i++) r.FillRect(cx - 3, h * (0.2f + i * 0.15f), cx + 3, h * (0.2f + i * 0.15f) + 4, A(d, 0.6f));
                    break;
                }
                case 1:   // plinth
                    MatRect(r, m, 0, 0, w, h * 0.15f, seed);
                    MatRect(r, m, w * 0.1f, h * 0.15f, w * 0.9f, h * 0.85f, seed + 1);
                    MatRect(r, m, 0, h * 0.85f, w, h, seed + 2);
                    Highlight(r, 0, h - 2, w, h, 0.25f);
                    break;
                case 2:   // urn
                {
                    var mask = NewMask(w, h);
                    mask.FillRect(w * 0.25f, 0, w * 0.75f, h * 0.08f, White);
                    mask.FillRect(w * 0.4f, h * 0.08f, w * 0.6f, h * 0.2f, White);
                    mask.FillEllipse(cx, h * 0.5f, w * 0.42f, h * 0.34f, White);
                    mask.FillRect(w * 0.3f, h * 0.78f, w * 0.7f, h * 0.9f, White);
                    mask.FillRect(w * 0.22f, h * 0.88f, w * 0.78f, h, White);
                    mask.FillEllipse(w * 0.12f, h * 0.55f, w * 0.1f, h * 0.16f, White);
                    mask.FillEllipse(w * 0.88f, h * 0.55f, w * 0.1f, h * 0.16f, White);
                    var cut = NewMask(w, h);
                    cut.FillEllipse(w * 0.12f, h * 0.55f, w * 0.05f, h * 0.1f, White);
                    cut.FillEllipse(w * 0.88f, h * 0.55f, w * 0.05f, h * 0.1f, White);
                    Subtract(mask, cut);
                    MatMask(r, m, mask, seed);
                    Shadow(r, w * 0.6f, h * 0.2f, w * 0.85f, h * 0.85f, 0.2f);
                    r.FillEllipse(cx, h * 0.5f, w * 0.3f, h * 0.05f, A(d, 0.5f));
                    if (def.id.Contains("ivy")) for (int i = 0; i < 12; i++) Leaf(r, w * rng.Range(0.25f, 0.75f), h * rng.Range(0.75f, 1f), 4, rng.Next() < 0.5f ? Moss : D(Moss, 0.3f), rng.Range(-0.5f, 0.5f));
                    break;
                }
                case 3:   // sarcophagus
                    MatRect(r, m, 0, 0, w, h * 0.12f, seed);
                    MatRect(r, m, w * 0.06f, h * 0.12f, w * 0.94f, h * 0.7f, seed + 1);
                    MatRect(r, m, 0, h * 0.7f, w, h * 0.85f, seed + 2);
                    r.FillPolygon(new[] { V(w * 0.05f, h * 0.85f), V(w * 0.95f, h * 0.85f), V(w * 0.75f, h), V(w * 0.25f, h) }, L(b, 0.15f));
                    r.FillRect(w * 0.42f, h * 0.2f, w * 0.58f, h * 0.62f, A(d, 0.5f));
                    r.FillRect(w * 0.3f, h * 0.48f, w * 0.7f, h * 0.55f, A(d, 0.5f));
                    break;
                case 4:   // tombstone rounded
                case 5:   // cross
                case 6:   // tablet
                {
                    var mask = NewMask(w, h);
                    mask.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.08f, White);
                    if (def.variant == 4)
                    {
                        mask.FillRect(w * 0.22f, h * 0.05f, w * 0.78f, h * 0.7f, White);
                        mask.FillEllipse(cx, h * 0.7f, w * 0.28f, h * 0.28f, White);
                    }
                    else if (def.variant == 5)
                    {
                        mask.FillRect(w * 0.2f, h * 0.05f, w * 0.8f, h * 0.28f, White);
                        mask.FillRect(w * 0.4f, h * 0.28f, w * 0.6f, h, White);
                        mask.FillRect(w * 0.18f, h * 0.65f, w * 0.82f, h * 0.8f, White);
                    }
                    else
                    {
                        mask.FillRect(w * 0.18f, h * 0.05f, w * 0.82f, h * 0.9f, White);
                        mask.FillTriangle(V(w * 0.18f, h * 0.9f), V(w * 0.82f, h * 0.9f), V(cx, h), White);
                    }
                    MatMask(r, m, mask, seed);
                    if (def.variant != 5)
                    {
                        for (int i = 0; i < 3; i++) r.FillRect(w * 0.32f, h * (0.3f + i * 0.12f), w * (0.55f + rng.Range(0f, 0.12f)), h * (0.3f + i * 0.12f) + 2, A(d, 0.6f));
                        r.FillRect(w * 0.45f, h * 0.68f, w * 0.55f, h * 0.7f, A(d, 0.6f));
                    }
                    if (m == DecoMaterial.MossyStone || def.id.Contains("old")) r.FillEllipse(w * 0.35f, h * 0.15f, w * 0.18f, h * 0.06f, A(Moss, 0.7f));
                    break;
                }
                case 7:   // sundial
                    MatRect(r, m, w * 0.35f, 0, w * 0.65f, h * 0.55f, seed);
                    MatRect(r, m, w * 0.2f, 0, w * 0.8f, h * 0.08f, seed + 1);
                    r.FillEllipse(cx, h * 0.62f, w * 0.45f, h * 0.14f, b);
                    r.EllipseRing(cx, h * 0.62f, w * 0.45f, h * 0.14f, 2, d);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = Mathf.PI * (0.15f + i * 0.116f);
                        r.Line(cx, h * 0.62f, cx + Mathf.Cos(a) * w * 0.38f, h * 0.62f + Mathf.Sin(a) * h * 0.11f, 1, d);
                    }
                    r.FillTriangle(V(cx, h * 0.62f), V(cx + w * 0.2f, h * 0.62f), V(cx, h * 0.95f), IronC);
                    break;
                case 8:   // birdbath
                    MatRect(r, m, w * 0.4f, 0, w * 0.6f, h * 0.55f, seed);
                    MatRect(r, m, w * 0.25f, 0, w * 0.75f, h * 0.08f, seed + 1);
                    r.FillEllipse(cx, h * 0.62f, w * 0.48f, h * 0.18f, b);
                    r.FillEllipse(cx, h * 0.66f, w * 0.38f, h * 0.1f, Hex("5a8fc0"));
                    r.FillEllipse(cx - w * 0.1f, h * 0.68f, w * 0.12f, h * 0.03f, A(White, 0.4f));
                    r.FillEllipse(cx + w * 0.22f, h * 0.8f, w * 0.08f, h * 0.06f, Hex("6a5a4a"));
                    r.FillCircle(cx + w * 0.28f, h * 0.88f, w * 0.05f, Hex("6a5a4a"));
                    r.FillTriangle(V(cx + w * 0.32f, h * 0.88f), V(cx + w * 0.32f, h * 0.9f), V(cx + w * 0.38f, h * 0.89f), GoldC);
                    break;
                case 9:   // fountain
                {
                    MatRect(r, m, 0, 0, w, h * 0.12f, seed);
                    r.FillRect(w * 0.06f, h * 0.12f, w * 0.94f, h * 0.3f, Hex("4a86c0"));
                    MatRect(r, m, 0, h * 0.28f, w, h * 0.34f, seed + 1);
                    MatRect(r, m, w * 0.44f, h * 0.34f, w * 0.56f, h * 0.62f, seed + 2);
                    r.FillEllipse(cx, h * 0.64f, w * 0.28f, h * 0.08f, b);
                    r.FillEllipse(cx, h * 0.66f, w * 0.22f, h * 0.05f, Hex("4a86c0"));
                    MatRect(r, m, w * 0.47f, h * 0.66f, w * 0.53f, h * 0.8f, seed + 3);
                    for (int i = -2; i <= 2; i++)
                    {
                        float x0 = cx + i * w * 0.02f;
                        r.Line(x0, h * 0.8f, cx + i * w * 0.18f, h * 0.36f, 1.8f, A(Hex("9ad0ff"), 0.85f));
                    }
                    r.FillCircle(cx, h * 0.9f, w * 0.05f, A(Hex("d0ecff"), 0.9f));
                    for (int i = 0; i < 6; i++) r.FillCircle(w * rng.Range(0.1f, 0.9f), h * rng.Range(0.15f, 0.28f), 1.5f, A(White, 0.6f));
                    break;
                }
                case 10:  // well
                    MatRect(r, DecoMaterial.Cobble, w * 0.1f, 0, w * 0.9f, h * 0.42f, seed);
                    r.FillEllipse(cx, h * 0.42f, w * 0.4f, h * 0.08f, D(b, 0.7f));
                    r.FillRect(w * 0.16f, h * 0.3f, w * 0.22f, h * 0.9f, WoodD);
                    r.FillRect(w * 0.78f, h * 0.3f, w * 0.84f, h * 0.9f, WoodD);
                    r.FillPolygon(new[] { V(w * 0.05f, h * 0.82f), V(w * 0.95f, h * 0.82f), V(w * 0.7f, h), V(w * 0.3f, h) }, WoodC);
                    r.FillRect(w * 0.2f, h * 0.68f, w * 0.8f, h * 0.72f, IronC);
                    r.FillRect(cx - 1.5f, h * 0.45f, cx + 1.5f, h * 0.7f, IronC);
                    r.FillRect(cx - w * 0.1f, h * 0.45f, cx + w * 0.1f, h * 0.55f, WoodC);
                    r.FillRect(cx - w * 0.1f, h * 0.53f, cx + w * 0.1f, h * 0.55f, IronC);
                    break;
                case 11:  // bench
                    MatRect(r, m, w * 0.08f, 0, w * 0.2f, h * 0.55f, seed);
                    MatRect(r, m, w * 0.8f, 0, w * 0.92f, h * 0.55f, seed + 1);
                    MatRect(r, m, 0, h * 0.55f, w, h * 0.75f, seed + 2);
                    MatRect(r, m, w * 0.04f, h * 0.75f, w * 0.96f, h, seed + 3, L(b, 0.1f));
                    Shadow(r, 0, h * 0.55f, w, h * 0.58f, 0.3f);
                    break;
                case 12:  // stairs
                {
                    int steps = Mathf.Max(2, Mathf.RoundToInt(def.width * 2f));
                    for (int i = 0; i < steps; i++)
                    {
                        float x0 = w * i / (float)steps;
                        MatRect(r, m, x0, 0, w, h * (i + 1) / (float)steps, seed + i);
                        Highlight(r, x0, h * (i + 1) / (float)steps - 2, w, h * (i + 1) / (float)steps, 0.25f);
                    }
                    break;
                }
                case 13:  // broken column stump
                {
                    MatRect(r, m, w * 0.05f, 0, w * 0.95f, h * 0.12f, seed);
                    var mask = NewMask(w, h);
                    mask.FillRect(w * 0.3f, h * 0.12f, w * 0.7f, h * 0.7f, White);
                    for (float x = w * 0.3f; x < w * 0.7f; x += 6) mask.FillTriangle(V(x, h * 0.7f), V(x + 6, h * 0.7f), V(x + 3, h * 0.7f + rng.Range(3, 12)), White);
                    MatMask(r, m, mask, seed + 1);
                    for (float x = w * 0.36f; x < w * 0.66f; x += 7) r.FillRect(x, h * 0.13f, x + 1.5f, h * 0.68f, A(d, 0.5f));
                    r.FillEllipse(w * 0.8f, h * 0.16f, w * 0.14f, h * 0.05f, D(b, 0.2f));
                    break;
                }
                case 14:  // boulder
                case 15:  // boulder pile
                {
                    var mask = NewMask(w, h);
                    if (def.variant == 14)
                    {
                        mask.FillEllipse(cx, h * 0.45f, w * 0.48f, h * 0.44f, White);
                        mask.FillEllipse(cx + w * 0.15f, h * 0.6f, w * 0.3f, h * 0.36f, White);
                    }
                    else
                    {
                        mask.FillEllipse(w * 0.3f, h * 0.28f, w * 0.3f, h * 0.28f, White);
                        mask.FillEllipse(w * 0.7f, h * 0.3f, w * 0.3f, h * 0.3f, White);
                        mask.FillEllipse(cx, h * 0.65f, w * 0.3f, h * 0.32f, White);
                    }
                    MatMask(r, m, mask, seed);
                    Shadow(r, 0, 0, w, h * 0.15f, 0.25f);
                    if (m == DecoMaterial.MossyStone) for (int i = 0; i < 5; i++) r.FillEllipse(w * rng.Range(0.2f, 0.8f), h * rng.Range(0.6f, 0.9f), w * 0.1f, h * 0.05f, A(Moss, 0.8f));
                    break;
                }
                case 16:  // rubble
                    for (int i = 0; i < 9; i++)
                    {
                        float x = w * rng.Range(0.05f, 0.95f), y = h * rng.Range(0.05f, 0.55f), sz = w * rng.Range(0.06f, 0.14f);
                        r.FillPolygon(new[] { V(x - sz, y), V(x, y - sz * 0.6f), V(x + sz, y), V(x + sz * 0.3f, y + sz * 0.8f), V(x - sz * 0.5f, y + sz * 0.6f) }, Color.Lerp(b, d, rng.Next() * 0.6f));
                    }
                    break;
                case 17:  // stalagmite
                {
                    var mask = NewMask(w, h);
                    mask.FillTriangle(V(0, 0), V(w, 0), V(cx, h), White);
                    mask.FillTriangle(V(w * 0.1f, 0), V(w * 0.45f, 0), V(w * 0.25f, h * 0.55f), White);
                    MatMask(r, m, mask, seed);
                    Shadow(r, cx, 0, w, h, 0.22f);
                    break;
                }
                default:  // 18 flagstone
                    MatRect(r, m, 0, 0, w, h, seed);
                    r.Line(w * 0.1f, h * 0.9f, w * 0.55f, h * 0.4f, 1.5f, A(d, 0.8f));
                    r.Line(w * 0.55f, h * 0.4f, w * 0.7f, 0, 1.5f, A(d, 0.8f));
                    r.Line(w * 0.55f, h * 0.4f, w, h * 0.55f, 1.2f, A(d, 0.6f));
                    Shadow(r, 0, 0, w, 2, 0.25f);
                    break;
            }
        }

        // ---- ivy and plants ---------------------------------------------------------------------

        static void DrawIvy(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var leaf = def.primaryColor;
            var dark = def.secondaryColor;
            var stem = D(dark, 0.35f);
            var rng = new Rng(seed);
            void Strand(float x0, float y0, float x1, float y1, int leaves, float size)
            {
                float px = x0, py = y0;
                for (int i = 1; i <= leaves; i++)
                {
                    float t = i / (float)leaves;
                    float nx = Mathf.Lerp(x0, x1, t) + Mathf.Sin(t * 9f + seed) * w * 0.05f, ny = Mathf.Lerp(y0, y1, t);
                    r.Line(px, py, nx, ny, 1.4f, stem);
                    px = nx; py = ny;
                    float side = (i % 2 == 0) ? 1 : -1;
                    Leaf(r, nx + side * size * 0.9f, ny + rng.Range(-2, 2), size, Color.Lerp(dark, leaf, rng.Next()), side * 0.5f);
                    if (i % 3 == 0) Leaf(r, nx, ny - size * 0.6f, size * 0.8f, L(leaf, 0.15f), rng.Range(-0.4f, 0.4f));
                }
            }
            void Patch(float x0, float y0, float x1, float y1, int count, float size)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = rng.Range(x0, x1), y = rng.Range(y0, y1);
                    r.Line(x, y, x + rng.Range(-8, 8), y + rng.Range(-8, 8), 1, stem);
                }
                for (int i = 0; i < count * 2; i++)
                {
                    float x = rng.Range(x0, x1), y = rng.Range(y0, y1);
                    Leaf(r, x, y, size * rng.Range(0.7f, 1.2f), Color.Lerp(dark, leaf, rng.Next()), rng.Range(-0.6f, 0.6f));
                }
                for (int i = 0; i < count / 2; i++) Leaf(r, rng.Range(x0, x1), rng.Range(y0, y1), size * 0.8f, L(leaf, 0.2f), rng.Range(-0.3f, 0.3f));
            }
            switch (def.variant)
            {
                case 0:   // hanging strands from the top edge
                {
                    int n = Mathf.Max(1, Mathf.RoundToInt(def.width * 2f));
                    for (int k = 0; k < n; k++)
                    {
                        float x = w * (k + 0.5f) / n;
                        Strand(x, h, x + rng.Range(-w * 0.1f, w * 0.1f), h * rng.Range(0.05f, 0.3f), Mathf.Max(4, h / 9), 4.5f);
                    }
                    for (int k = 0; k < n * 3; k++) Leaf(r, rng.Range(0, w), h - rng.Range(0, 6), 5, Color.Lerp(dark, leaf, rng.Next()), rng.Range(-0.5f, 0.5f));
                    break;
                }
                case 1:   // wall patch
                    Patch(w * 0.05f, h * 0.05f, w * 0.95f, h * 0.95f, Mathf.Max(8, w * h / 350), 4.5f);
                    break;
                case 2:   // corner, growing from the left
                    Strand(0, h, w * 0.9f, h * 0.15f, Mathf.Max(5, w / 8), 4.5f);
                    Patch(0, h * 0.5f, w * 0.5f, h, Mathf.Max(4, w * h / 900), 4.5f);
                    break;
                case 3:   // corner from the right
                    Strand(w, h, w * 0.1f, h * 0.15f, Mathf.Max(5, w / 8), 4.5f);
                    Patch(w * 0.5f, h * 0.5f, w, h, Mathf.Max(4, w * h / 900), 4.5f);
                    break;
                case 4:   // creeping strip along the bottom
                    Strand(0, h * 0.4f, w, h * 0.5f, Mathf.Max(6, w / 7), 4.5f);
                    for (int i = 0; i < w / 10; i++) Leaf(r, rng.Range(0, w), rng.Range(h * 0.1f, h * 0.7f), 4.5f, Color.Lerp(dark, leaf, rng.Next()), rng.Range(-0.5f, 0.5f));
                    break;
                default:  // swag draped over an arch: a curve sagging in the middle
                {
                    float px = 0, py = h * 0.95f;
                    for (int i = 1; i <= 16; i++)
                    {
                        float t = i / 16f;
                        float nx = t * w, ny = h * (0.95f - 0.6f * Mathf.Sin(t * Mathf.PI));
                        r.Line(px, py, nx, ny, 2, stem);
                        Leaf(r, nx, ny - 3, 4.5f, Color.Lerp(dark, leaf, rng.Next()), rng.Range(-0.5f, 0.5f));
                        if (i % 2 == 0) Leaf(r, nx - 3, ny + 3, 4f, L(leaf, 0.15f), 0.3f);
                        px = nx; py = ny;
                    }
                    break;
                }
            }
        }

        static void DrawFoliage(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var p = def.primaryColor;
            var s = def.secondaryColor;
            var rng = new Rng(seed);
            void Blade(float x, float y, float len, float lean, float thick, Color c) => r.Line(x, y, x + lean, y + len, thick, c);
            void Flower(float x, float y, float rad, Color petal)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    r.FillCircle(x + Mathf.Cos(a) * rad * 0.6f, y + Mathf.Sin(a) * rad * 0.6f, rad * 0.45f, petal);
                }
                r.FillCircle(x, y, rad * 0.3f, Hex("ffe08a"));
            }
            switch (def.variant)
            {
                case 0:   // moss patch
                    for (int i = 0; i < 10; i++) r.FillEllipse(rng.Range(w * 0.1f, w * 0.9f), rng.Range(h * 0.2f, h * 0.7f), rng.Range(w * 0.12f, w * 0.25f), rng.Range(h * 0.2f, h * 0.45f), Color.Lerp(s, p, rng.Next()));
                    for (int i = 0; i < 12; i++) r.FillCircle(rng.Range(0, w), rng.Range(h * 0.3f, h * 0.9f), 1.5f, L(p, 0.3f));
                    break;
                case 1:   // moss curtain hanging down
                    for (int i = 0; i < w / 4; i++)
                    {
                        float x = rng.Range(2, w - 2), len = h * rng.Range(0.3f, 0.95f);
                        r.Line(x, h, x + rng.Range(-3, 3), h - len, rng.Range(1.5f, 3f), Color.Lerp(s, p, rng.Next()));
                    }
                    r.FillRect(0, h - 4, w, h, s);
                    break;
                case 2:   // lichen
                    for (int i = 0; i < 14; i++) r.Ring(rng.Range(w * 0.15f, w * 0.85f), rng.Range(h * 0.15f, h * 0.85f), rng.Range(4, 9), rng.Range(1, 3), A(Color.Lerp(s, p, rng.Next()), 0.85f));
                    for (int i = 0; i < 10; i++) r.FillCircle(rng.Range(0, w), rng.Range(0, h), 2, A(L(p, 0.3f), 0.7f));
                    break;
                case 3:   // fern
                    for (int k = 0; k < 5; k++)
                    {
                        float a = Mathf.PI * (0.25f + k * 0.125f);
                        float ex = cx + Mathf.Cos(a) * w * 0.45f, ey = Mathf.Sin(a) * h * 0.9f;
                        r.Line(cx, 0, ex, ey, 1.5f, s);
                        for (int i = 1; i < 7; i++)
                        {
                            float t = i / 7f, x = Mathf.Lerp(cx, ex, t), y = Mathf.Lerp(0, ey, t), len = (1 - t) * w * 0.12f + 2;
                            r.Line(x, y, x - len, y + len * 0.5f, 1.3f, p);
                            r.Line(x, y, x + len, y + len * 0.5f, 1.3f, p);
                        }
                    }
                    break;
                case 4:   // thistle
                    r.Line(cx, 0, cx, h * 0.6f, 2, s);
                    r.Line(cx, h * 0.25f, cx - w * 0.3f, h * 0.4f, 1.5f, s);
                    r.Line(cx, h * 0.35f, cx + w * 0.3f, h * 0.5f, 1.5f, s);
                    r.FillEllipse(cx, h * 0.65f, w * 0.14f, h * 0.12f, p);
                    for (int i = 0; i < 9; i++) r.Line(cx + (i - 4) * w * 0.035f, h * 0.72f, cx + (i - 4) * w * 0.06f, h * 0.95f, 1.5f, Hex("b070ff"));
                    break;
                case 5:   // reeds
                    for (int i = 0; i < 7; i++)
                    {
                        float x = w * (0.1f + i * 0.13f);
                        Blade(x, 0, h * rng.Range(0.6f, 0.95f), rng.Range(-6, 6), 2, Color.Lerp(s, p, rng.Next()));
                        if (i % 2 == 0) r.FillEllipse(x + 2, h * 0.75f, 3, 8, Hex("6a4a2a"));
                    }
                    break;
                case 6:   // briar
                    for (int i = 0; i < 4; i++)
                    {
                        float x0 = rng.Range(0, w), y0 = rng.Range(0, h * 0.3f), x1 = rng.Range(0, w), y1 = rng.Range(h * 0.4f, h);
                        r.Line(x0, y0, x1, y1, 2, s);
                        for (int k = 0; k < 6; k++)
                        {
                            float t = k / 6f, x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t);
                            r.FillTriangle(V(x - 2, y), V(x + 2, y), V(x + 4, y + 5), D(s, 0.3f));
                            if (k % 2 == 0) Leaf(r, x - 3, y + 2, 3.5f, p, -0.4f);
                        }
                    }
                    break;
                case 7:   // nettles
                    for (int i = 0; i < 4; i++)
                    {
                        float x = w * (0.2f + i * 0.2f);
                        r.Line(x, 0, x, h * 0.85f, 1.5f, s);
                        for (int k = 0; k < 4; k++)
                        {
                            float y = h * (0.25f + k * 0.18f);
                            r.FillTriangle(V(x, y), V(x - w * 0.12f, y + 4), V(x - w * 0.02f, y + 10), p);
                            r.FillTriangle(V(x, y + 6), V(x + w * 0.12f, y + 10), V(x + w * 0.02f, y + 16), p);
                        }
                    }
                    break;
                case 8:   // wheat sheaf
                    for (int i = 0; i < 9; i++)
                    {
                        float x = cx + (i - 4) * w * 0.05f;
                        r.Line(cx, h * 0.1f, x, h * 0.7f, 1.5f, s);
                        r.FillEllipse(x, h * 0.8f, 3, 9, p);
                        for (int k = 0; k < 4; k++) r.Line(x, h * (0.72f + k * 0.06f), x + 4, h * (0.76f + k * 0.06f), 1, L(p, 0.3f));
                    }
                    r.FillRect(cx - w * 0.18f, h * 0.3f, cx + w * 0.18f, h * 0.38f, Hex("8a5a2a"));
                    break;
                case 9:   // heather
                    for (int i = 0; i < 8; i++)
                    {
                        float x = w * (0.1f + i * 0.11f);
                        r.Line(x, 0, x + rng.Range(-3, 3), h * 0.5f, 1.5f, s);
                        for (int k = 0; k < 5; k++) r.FillCircle(x + rng.Range(-4, 4), h * rng.Range(0.3f, 0.95f), 2.5f, Color.Lerp(p, Hex("d090e0"), rng.Next()));
                    }
                    break;
                case 10:  // wildflowers
                    for (int i = 0; i < 6; i++)
                    {
                        float x = w * (0.1f + i * 0.16f), top = h * rng.Range(0.5f, 0.9f);
                        r.Line(x, 0, x, top, 1.5f, s);
                        Leaf(r, x - 4, top * 0.5f, 3, s, -0.5f);
                        Flower(x, top, w * 0.09f, p);
                    }
                    break;
                case 11:  // climbing roses
                    for (int i = 0; i < 3; i++)
                    {
                        float x0 = w * (0.2f + i * 0.3f);
                        r.Line(x0, 0, x0 + rng.Range(-8, 8), h, 1.8f, s);
                    }
                    for (int i = 0; i < w * h / 300; i++) Leaf(r, rng.Range(0, w), rng.Range(0, h), 4, Color.Lerp(s, Hex("3f8a3a"), rng.Next()), rng.Range(-0.5f, 0.5f));
                    for (int i = 0; i < 6; i++)
                    {
                        float x = rng.Range(w * 0.1f, w * 0.9f), y = rng.Range(h * 0.15f, h * 0.95f);
                        r.FillCircle(x, y, 5, D(p, 0.2f));
                        r.FillCircle(x - 1, y + 1, 3, p);
                        r.FillCircle(x + 1, y - 1, 1.5f, L(p, 0.4f));
                    }
                    break;
                case 12:  // wisteria
                    r.Line(0, h * 0.95f, w, h * 0.9f, 2.5f, s);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = w * (0.1f + i * 0.2f), len = h * rng.Range(0.4f, 0.85f);
                        for (int k = 0; k < 8; k++)
                        {
                            float t = k / 8f;
                            r.FillCircle(x + Mathf.Sin(k * 1.7f) * 4, h * 0.9f - len * t, (1 - t) * 5 + 2, Color.Lerp(p, Hex("d0b0ff"), rng.Next()));
                        }
                    }
                    for (int i = 0; i < 8; i++) Leaf(r, rng.Range(0, w), h * rng.Range(0.8f, 0.98f), 4, Hex("4f9a4a"), rng.Range(-0.5f, 0.5f));
                    break;
                case 13:  // grapevine
                    r.Line(0, h * 0.9f, w, h * 0.92f, 2.5f, s);
                    r.Line(cx, 0, cx, h * 0.9f, 2.5f, s);
                    for (int i = 0; i < w * h / 250; i++) Leaf(r, rng.Range(0, w), rng.Range(h * 0.5f, h), 5, Color.Lerp(p, L(p, 0.3f), rng.Next()), rng.Range(-0.5f, 0.5f));
                    for (int i = 0; i < 3; i++)
                    {
                        float x = w * (0.2f + i * 0.3f), y = h * 0.55f;
                        for (int k = 0; k < 10; k++) r.FillCircle(x + (k % 3 - 1) * 4 + (k / 3 % 2) * 2, y - (k / 3) * 5, 3, Color.Lerp(Hex("5a2a7a"), Hex("8a4aaa"), rng.Next()));
                    }
                    break;
                case 14:  // hops
                    r.Line(cx, 0, cx, h, 2, s);
                    for (int i = 0; i < 6; i++)
                    {
                        float y = h * (0.15f + i * 0.14f), x = cx + ((i % 2 == 0) ? -w * 0.22f : w * 0.22f);
                        r.Line(cx, y + 4, x, y, 1.2f, s);
                        for (int k = 0; k < 4; k++) r.FillTriangle(V(x - 5 + k * 1.5f, y - k * 3), V(x + 5 - k * 1.5f, y - k * 3), V(x, y - k * 3 - 4), Color.Lerp(p, L(p, 0.3f), k / 4f));
                        Leaf(r, cx + ((i % 2 == 0) ? 6 : -6), y + 8, 4, D(p, 0.2f), 0.3f);
                    }
                    break;
                case 15:  // topiary ball
                case 16:  // cone
                case 17:  // spiral
                    r.FillRect(w * 0.3f, 0, w * 0.7f, h * 0.12f, Hex("8a5a2a"));
                    r.FillRect(w * 0.25f, h * 0.12f, w * 0.75f, h * 0.18f, Hex("a9743d"));
                    r.FillRect(cx - 2, h * 0.18f, cx + 2, h * 0.35f, s);
                    if (def.variant == 15)
                    {
                        r.FillCircle(cx, h * 0.62f, w * 0.34f, s);
                        r.FillCircle(cx - w * 0.06f, h * 0.66f, w * 0.26f, p);
                        for (int i = 0; i < 18; i++) r.FillCircle(cx + rng.Range(-w * 0.3f, w * 0.3f), h * 0.62f + rng.Range(-w * 0.3f, w * 0.3f), 2.5f, Color.Lerp(s, L(p, 0.2f), rng.Next()));
                    }
                    else if (def.variant == 16)
                    {
                        r.FillTriangle(V(w * 0.15f, h * 0.3f), V(w * 0.85f, h * 0.3f), V(cx, h), s);
                        r.FillTriangle(V(w * 0.25f, h * 0.3f), V(w * 0.7f, h * 0.3f), V(cx - 2, h * 0.95f), p);
                    }
                    else
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            float y = h * (0.35f + i * 0.17f), rad = w * (0.32f - i * 0.06f);
                            r.FillEllipse(cx + ((i % 2 == 0) ? -3 : 3), y, rad, h * 0.09f, i % 2 == 0 ? p : s);
                        }
                        r.FillCircle(cx, h * 0.95f, w * 0.08f, p);
                    }
                    break;
                case 18:  // hedge
                    for (int i = 0; i < w / 6; i++) r.FillEllipse(rng.Range(0, w), rng.Range(h * 0.2f, h * 0.85f), rng.Range(w * 0.05f, w * 0.1f), rng.Range(h * 0.15f, h * 0.3f), Color.Lerp(s, p, rng.Next()));
                    r.FillRect(0, 0, w, h * 0.35f, s);
                    for (int i = 0; i < w / 8; i++) r.FillCircle(rng.Range(0, w), rng.Range(h * 0.6f, h * 0.9f), 2.5f, L(p, 0.25f));
                    break;
                case 19:  // hedge arch
                {
                    var mask = NewMask(w, h);
                    mask.FillRect(0, 0, w * 0.25f, h * 0.6f, White);
                    mask.FillRect(w * 0.75f, 0, w, h * 0.6f, White);
                    mask.FillEllipse(cx, h * 0.55f, w * 0.5f, h * 0.45f, White);
                    var cut = NewMask(w, h);
                    cut.FillEllipse(cx, h * 0.5f, w * 0.25f, h * 0.35f, White);
                    Subtract(mask, cut);
                    ClearRect(mask, w * 0.25f, 0, w * 0.75f, h * 0.5f);
                    var leaves = Material(w, h, DecoMaterial.Ivy, seed, p);
                    BlitMasked(r, leaves, mask);
                    break;
                }
                case 20:  // potted plant
                    MatRect(r, DecoMaterial.Terracotta, w * 0.25f, 0, w * 0.75f, h * 0.35f, seed);
                    r.FillRect(w * 0.2f, h * 0.33f, w * 0.8f, h * 0.42f, Hex("c2643a"));
                    for (int i = 0; i < 12; i++) Leaf(r, cx + rng.Range(-w * 0.3f, w * 0.3f), h * rng.Range(0.42f, 0.95f), 5, Color.Lerp(s, p, rng.Next()), rng.Range(-0.6f, 0.6f));
                    break;
                case 21:  // window box
                    MatRect(r, DecoMaterial.Oak, 0, 0, w, h * 0.4f, seed);
                    r.FillRect(0, h * 0.38f, w, h * 0.45f, Hex("5a3a1a"));
                    for (int i = 0; i < 6; i++)
                    {
                        float x = w * (0.1f + i * 0.16f);
                        r.Line(x, h * 0.45f, x, h * 0.7f, 1.2f, s);
                        Flower(x, h * 0.78f, w * 0.07f, i % 2 == 0 ? p : Hex("f0f0f0"));
                    }
                    for (int i = 0; i < 6; i++) Leaf(r, rng.Range(0, w), h * rng.Range(0.42f, 0.6f), 3.5f, s, rng.Range(-0.5f, 0.5f));
                    break;
                case 22:  // grass tuft
                    for (int i = 0; i < 9; i++) Blade(w * (0.1f + i * 0.1f), 0, h * rng.Range(0.5f, 0.98f), rng.Range(-8, 8), 2, Color.Lerp(s, p, rng.Next()));
                    break;
                case 23:  // stump
                    r.FillRect(w * 0.15f, 0, w * 0.85f, h * 0.7f, s);
                    r.FillEllipse(cx, h * 0.7f, w * 0.35f, h * 0.18f, p);
                    r.EllipseRing(cx, h * 0.7f, w * 0.22f, h * 0.11f, 1.5f, D(p, 0.3f));
                    r.EllipseRing(cx, h * 0.7f, w * 0.1f, h * 0.05f, 1.5f, D(p, 0.3f));
                    r.FillTriangle(V(0, 0), V(w * 0.2f, 0), V(w * 0.16f, h * 0.2f), s);
                    r.FillTriangle(V(w, 0), V(w * 0.8f, 0), V(w * 0.84f, h * 0.2f), s);
                    for (float x = w * 0.2f; x < w * 0.8f; x += 6) r.FillRect(x, 0, x + 1.5f, h * 0.68f, A(D(s, 0.4f), 0.6f));
                    break;
                case 24:  // log
                    r.FillRoundRect(0, h * 0.1f, w, h * 0.9f, h * 0.35f, s);
                    r.FillEllipse(w * 0.92f, h * 0.5f, w * 0.08f, h * 0.4f, p);
                    r.EllipseRing(w * 0.92f, h * 0.5f, w * 0.045f, h * 0.22f, 1.2f, D(p, 0.3f));
                    for (int i = 0; i < 5; i++) r.Line(w * 0.05f, h * (0.25f + i * 0.13f), w * 0.85f, h * (0.28f + i * 0.13f), 1, A(D(s, 0.4f), 0.5f));
                    r.FillEllipse(w * 0.4f, h * 0.85f, w * 0.15f, h * 0.08f, A(Moss, 0.8f));
                    break;
                case 25:  // fallen branch
                    r.Line(0, h * 0.3f, w, h * 0.6f, 4, s);
                    r.Line(w * 0.35f, h * 0.4f, w * 0.5f, h * 0.95f, 2.5f, s);
                    r.Line(w * 0.7f, h * 0.5f, w * 0.85f, h * 0.05f, 2, s);
                    for (int i = 0; i < 6; i++) Leaf(r, rng.Range(w * 0.3f, w), rng.Range(h * 0.3f, h), 3.5f, p, rng.Range(-0.5f, 0.5f));
                    break;
                default:  // 26 lily pads and flower
                    r.FillEllipse(w * 0.3f, h * 0.35f, w * 0.26f, h * 0.2f, p);
                    r.FillEllipse(w * 0.72f, h * 0.3f, w * 0.22f, h * 0.17f, s);
                    r.FillCircle(w * 0.55f, h * 0.65f, w * 0.12f, Hex("f5b0d0"));
                    for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; r.FillEllipse(w * 0.55f + Mathf.Cos(a) * w * 0.12f, h * 0.65f + Mathf.Sin(a) * h * 0.1f, w * 0.07f, h * 0.05f, Hex("ffd0e6")); }
                    r.FillCircle(w * 0.55f, h * 0.65f, w * 0.04f, Hex("ffe08a"));
                    break;
            }
        }

        static void DrawTree(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var p = def.primaryColor;
            var s = def.secondaryColor;
            var rng = new Rng(seed);
            switch (def.variant)
            {
                case 0:   // willow: trunk + drooping strands
                    r.FillRect(cx - w * 0.05f, 0, cx + w * 0.05f, h * 0.6f, s);
                    r.FillEllipse(cx, h * 0.72f, w * 0.42f, h * 0.24f, D(p, 0.15f));
                    for (int i = 0; i < 26; i++)
                    {
                        float x = cx + rng.Range(-w * 0.45f, w * 0.45f), top = h * 0.72f + rng.Range(-h * 0.1f, h * 0.15f), len = rng.Range(h * 0.25f, h * 0.55f);
                        r.Line(x, top, x + rng.Range(-4, 4), top - len, 1.8f, Color.Lerp(D(p, 0.2f), L(p, 0.2f), rng.Next()));
                    }
                    break;
                case 1:   // birch: white trunk with dark marks, light canopy
                    r.FillRect(cx - w * 0.06f, 0, cx + w * 0.06f, h * 0.75f, Hex("e8e4dc"));
                    for (int i = 0; i < 6; i++) r.FillRect(cx - w * 0.06f + rng.Range(0, w * 0.06f), h * rng.Range(0.05f, 0.7f), cx + rng.Range(0, w * 0.05f), h * rng.Range(0.05f, 0.7f) + 3, Hex("3a3a3a"));
                    for (int i = 0; i < 9; i++) r.FillEllipse(cx + rng.Range(-w * 0.35f, w * 0.35f), h * rng.Range(0.55f, 0.92f), w * rng.Range(0.12f, 0.2f), h * rng.Range(0.08f, 0.14f), Color.Lerp(s, p, rng.Next()));
                    break;
                case 2:   // apple: round canopy with red fruit
                    r.FillRect(cx - w * 0.06f, 0, cx + w * 0.06f, h * 0.5f, s);
                    r.FillCircle(cx, h * 0.68f, w * 0.38f, D(p, 0.1f));
                    r.FillCircle(cx - w * 0.15f, h * 0.6f, w * 0.26f, p);
                    r.FillCircle(cx + w * 0.16f, h * 0.62f, w * 0.26f, p);
                    r.FillCircle(cx, h * 0.8f, w * 0.2f, L(p, 0.15f));
                    for (int i = 0; i < 8; i++) r.FillCircle(cx + rng.Range(-w * 0.32f, w * 0.32f), h * rng.Range(0.5f, 0.9f), 3, Hex("d83a3a"));
                    break;
                case 3:   // yew: dark dense cone
                    r.FillRect(cx - w * 0.06f, 0, cx + w * 0.06f, h * 0.2f, s);
                    r.FillTriangle(V(w * 0.05f, h * 0.15f), V(w * 0.95f, h * 0.15f), V(cx, h), D(p, 0.2f));
                    r.FillTriangle(V(w * 0.15f, h * 0.15f), V(w * 0.7f, h * 0.15f), V(cx - w * 0.05f, h * 0.9f), p);
                    for (int i = 0; i < 8; i++) r.FillCircle(cx + rng.Range(-w * 0.35f, w * 0.35f), h * rng.Range(0.2f, 0.75f), 2, Hex("d83a3a"));
                    break;
                case 4:   // hawthorn: gnarled, blossom
                    r.FillRect(cx - w * 0.07f, 0, cx + w * 0.07f, h * 0.4f, s);
                    r.Line(cx, h * 0.35f, cx - w * 0.3f, h * 0.6f, 4, s);
                    r.Line(cx, h * 0.4f, cx + w * 0.3f, h * 0.65f, 4, s);
                    for (int i = 0; i < 10; i++) r.FillEllipse(cx + rng.Range(-w * 0.4f, w * 0.4f), h * rng.Range(0.5f, 0.9f), w * rng.Range(0.1f, 0.18f), h * rng.Range(0.08f, 0.14f), Color.Lerp(s, p, rng.Next()));
                    for (int i = 0; i < 14; i++) r.FillCircle(cx + rng.Range(-w * 0.4f, w * 0.4f), h * rng.Range(0.5f, 0.92f), 2, Hex("fff0f4"));
                    break;
                default:  // 5 cypress: tall narrow flame shape
                    r.FillRect(cx - w * 0.06f, 0, cx + w * 0.06f, h * 0.12f, s);
                    r.FillEllipse(cx, h * 0.5f, w * 0.3f, h * 0.42f, D(p, 0.2f));
                    r.FillTriangle(V(cx - w * 0.3f, h * 0.5f), V(cx + w * 0.3f, h * 0.5f), V(cx, h), D(p, 0.2f));
                    r.FillEllipse(cx - w * 0.06f, h * 0.5f, w * 0.18f, h * 0.36f, p);
                    r.FillTriangle(V(cx - w * 0.22f, h * 0.5f), V(cx + w * 0.12f, h * 0.5f), V(cx - w * 0.04f, h * 0.97f), p);
                    break;
            }
        }

        // ---- roofs, windows, doors ---------------------------------------------------------------

        static void DrawRoof(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            var m = Mat(def, DecoMaterial.Slate);
            var b = BaseColor(m);
            var mask = NewMask(w, h);
            float eave = h * 0.12f;
            switch (def.variant)   // 0 rises to the right, 1 rises to the left, 2 gable, 3 flat strip
            {
                case 0: mask.FillPolygon(new[] { V(0, 0), V(w, 0), V(w, h), V(0, eave) }, White); break;
                case 1: mask.FillPolygon(new[] { V(0, 0), V(w, 0), V(w, eave), V(0, h) }, White); break;
                case 2: mask.FillPolygon(new[] { V(0, 0), V(w, 0), V(w, eave), V(w / 2f, h), V(0, eave) }, White); break;
                default: mask.FillRect(0, 0, w, h, White); break;
            }
            // material runs in horizontal courses regardless of slope
            MatMask(r, m, mask, seed);
            if (m == DecoMaterial.Slate || m == DecoMaterial.Lead || m == DecoMaterial.Copper)
                for (float y = 6; y < h; y += 8) r.FillRect(0, y, w, y + 1.2f, A(D(b, 0.5f), 0.4f));
            if (def.variant != 3)
            {
                float rx = def.variant == 0 ? w : (def.variant == 1 ? 0 : w / 2f);
                float lx = def.variant == 0 ? 0 : (def.variant == 1 ? w : 0);
                var ridge = A(D(b, 0.55f), 0.9f);
                if (def.variant == 2)
                {
                    r.Line(0, eave, w / 2f, h, 3, ridge); r.Line(w, eave, w / 2f, h, 3, ridge);
                }
                else r.Line(lx, eave, rx, h, 3, ridge);
            }
            // eave board and shadow underneath
            r.FillRect(0, eave - 3, w, eave, D(WoodD, 0.1f));
            r.FillRect(0, 0, w, eave - 3, A(D(b, 0.65f), 0.9f));
            Highlight(r, 0, h - 2, w, h, 0.15f);
        }

        static void DrawTowerCap(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var m = Mat(def, DecoMaterial.Slate);
            var b = BaseColor(m);
            var mask = NewMask(w, h);
            switch (def.variant)   // 0 cone, 1 onion dome, 2 dome, 3 chimney, 4 finial, 5 weathervane, 6 dormer
            {
                case 0:
                    mask.FillTriangle(V(0, h * 0.1f), V(w, h * 0.1f), V(cx, h), White);
                    mask.FillRect(0, 0, w, h * 0.12f, White);
                    MatMask(r, m, mask, seed);
                    for (float y = h * 0.14f; y < h; y += 8) r.FillRect(0, y, w, y + 1, A(D(b, 0.5f), 0.4f));
                    Shadow(r, cx, 0, w, h, 0.2f);
                    r.FillRect(cx - 1.5f, h * 0.92f, cx + 1.5f, h, GoldC);
                    break;
                case 1:
                    mask.FillEllipse(cx, h * 0.45f, w * 0.5f, h * 0.36f, White);
                    mask.FillTriangle(V(cx - w * 0.3f, h * 0.6f), V(cx + w * 0.3f, h * 0.6f), V(cx, h * 0.95f), White);
                    mask.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.15f, White);
                    MatMask(r, m, mask, seed);
                    for (int i = 1; i < 6; i++) r.Line(cx, h * 0.9f, w * i / 6f, h * 0.15f, 1.2f, A(D(b, 0.5f), 0.5f));
                    Shadow(r, cx, 0, w, h, 0.2f);
                    r.FillCircle(cx, h * 0.96f, w * 0.05f, GoldC);
                    break;
                case 2:
                    mask.FillEllipse(cx, h * 0.1f, w * 0.5f, h * 0.88f, White);
                    ClearRect(mask, 0, 0, w, h * 0.1f);
                    mask.FillRect(0, 0, w, h * 0.12f, White);
                    MatMask(r, m, mask, seed);
                    for (int i = 1; i < 6; i++) r.Line(cx, h * 0.98f, w * i / 6f, h * 0.12f, 1.2f, A(D(b, 0.5f), 0.5f));
                    Shadow(r, cx, 0, w, h, 0.2f);
                    break;
                case 3:
                    MatRect(r, m, w * 0.2f, 0, w * 0.8f, h * 0.8f, seed);
                    MatRect(r, m, w * 0.1f, h * 0.8f, w * 0.9f, h * 0.92f, seed + 1);
                    r.FillRect(w * 0.3f, h * 0.92f, w * 0.45f, h, IronC);
                    r.FillRect(w * 0.55f, h * 0.92f, w * 0.7f, h, IronC);
                    for (int i = 0; i < 4; i++) r.FillCircle(w * 0.5f + i * 3, h * 0.96f + i * 6, 4 + i, A(Hex("c8c8d0"), 0.35f - i * 0.06f));
                    break;
                case 4:
                    r.FillRect(cx - 2, 0, cx + 2, h * 0.5f, IronC);
                    r.FillRect(cx - w * 0.15f, h * 0.2f, cx + w * 0.15f, h * 0.26f, IronC);
                    r.FillCircle(cx, h * 0.55f, w * 0.14f, GoldC);
                    r.FillTriangle(V(cx - w * 0.12f, h * 0.62f), V(cx + w * 0.12f, h * 0.62f), V(cx, h), GoldC);
                    r.FillCircle(cx - w * 0.04f, h * 0.58f, w * 0.04f, L(GoldC, 0.5f));
                    break;
                case 5:
                    r.FillRect(cx - 2, 0, cx + 2, h * 0.6f, IronC);
                    r.FillRect(cx - w * 0.3f, h * 0.35f, cx + w * 0.3f, h * 0.38f, IronC);
                    r.FillRect(cx - w * 0.3f, h * 0.35f, cx - w * 0.28f, h * 0.45f, IronC);
                    r.FillRect(cx + w * 0.28f, h * 0.35f, cx + w * 0.3f, h * 0.45f, IronC);
                    r.FillPolygon(new[] { V(cx - w * 0.28f, h * 0.68f), V(cx + w * 0.1f, h * 0.62f), V(cx + w * 0.3f, h * 0.72f), V(cx + w * 0.1f, h * 0.8f) }, IronC);
                    r.FillTriangle(V(cx - w * 0.3f, h * 0.6f), V(cx - w * 0.3f, h * 0.78f), V(cx - w * 0.15f, h * 0.69f), IronC);
                    r.FillCircle(cx + w * 0.27f, h * 0.75f, w * 0.05f, IronC);
                    r.FillCircle(cx, h * 0.62f, w * 0.05f, GoldC);
                    break;
                default:
                    MatRect(r, DecoMaterial.Plaster, w * 0.2f, 0, w * 0.8f, h * 0.55f, seed);
                    r.FillRect(w * 0.35f, h * 0.1f, w * 0.65f, h * 0.5f, Hex("3a2a5a"));
                    r.FillRect(cx - 1, h * 0.1f, cx + 1, h * 0.5f, Hex("9a9aa8"));
                    r.FillRect(w * 0.35f, h * 0.3f, w * 0.65f, h * 0.3f + 2, Hex("9a9aa8"));
                    mask.FillPolygon(new[] { V(0, h * 0.5f), V(w, h * 0.5f), V(w, h * 0.58f), V(cx, h), V(0, h * 0.58f) }, White);
                    MatMask(r, m, mask, seed + 1);
                    r.Line(0, h * 0.58f, cx, h, 2.5f, A(D(b, 0.55f), 0.9f)); r.Line(w, h * 0.58f, cx, h, 2.5f, A(D(b, 0.55f), 0.9f));
                    break;
            }
        }

        static void DrawWindow(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var m = Mat(def, DecoMaterial.Granite);
            var stone = BaseColor(m);
            var glass = def.primaryColor;
            var lead = def.secondaryColor;
            int v = def.variant;   // 0 arrow slit, 1 round, 2 gothic, 3 rose, 4 trefoil, 5 quatrefoil, 6 shutters open, 7 shutters closed, 8 barred, 9 lit, 10 oriel, 11 stained
            switch (v)
            {
                case 0:
                    MatRect(r, m, w * 0.2f, 0, w * 0.8f, h, seed);
                    r.FillRect(w * 0.42f, h * 0.1f, w * 0.58f, h * 0.9f, Hex("1a1420"));
                    r.FillRect(w * 0.3f, h * 0.45f, w * 0.7f, h * 0.55f, Hex("1a1420"));
                    r.FillRect(w * 0.42f, h * 0.1f, w * 0.45f, h * 0.9f, A(White, 0.15f));
                    break;
                case 1:
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.48f, stone);
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.38f, glass);
                    r.FillRect(cx - 1.5f, cy - h * 0.38f, cx + 1.5f, cy + h * 0.38f, lead);
                    r.FillRect(cx - w * 0.38f, cy - 1.5f, cx + w * 0.38f, cy + 1.5f, lead);
                    r.FillEllipse(cx - w * 0.14f, cy + h * 0.14f, w * 0.08f, h * 0.05f, A(White, 0.3f));
                    break;
                case 2:
                case 9:
                case 11:
                {
                    var mask = NewMask(w, h);
                    mask.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.65f, White);
                    mask.FillPolygon(new[] { V(w * 0.1f, h * 0.65f), V(cx, h), V(w * 0.9f, h * 0.65f) }, White);
                    mask.FillEllipse(cx, h * 0.62f, w * 0.4f, h * 0.3f, White);
                    MatMask(r, m, mask, seed);
                    var pane = v == 9 ? Hex("ffd27a") : glass;
                    r.FillRect(w * 0.2f, h * 0.06f, w * 0.8f, h * 0.62f, pane);
                    r.FillTriangle(V(w * 0.2f, h * 0.62f), V(cx, h * 0.9f), V(w * 0.8f, h * 0.62f), pane);
                    r.FillEllipse(cx, h * 0.6f, w * 0.3f, h * 0.24f, pane);
                    if (v == 11)
                    {
                        var c2 = Hex("2c4fb8"); var c3 = Hex("e0b64a"); var c4 = Hex("2f8a3a");
                        r.FillRect(w * 0.2f, h * 0.06f, cx, h * 0.34f, c2);
                        r.FillRect(cx, h * 0.34f, w * 0.8f, h * 0.62f, c4);
                        r.FillCircle(cx, h * 0.72f, w * 0.12f, c3);
                        r.FillRect(w * 0.2f, h * 0.34f, cx, h * 0.62f, glass);
                    }
                    if (v == 9)
                    {
                        r.FillRect(w * 0.25f, h * 0.1f, w * 0.45f, h * 0.55f, A(White, 0.25f));
                        for (int i = 0; i < 3; i++) r.FillCircle(cx, cy, w * (0.6f + i * 0.2f), A(Hex("ffd27a"), 0.06f));
                    }
                    r.FillRect(cx - 1.5f, h * 0.06f, cx + 1.5f, h * 0.88f, lead);
                    r.FillRect(w * 0.2f, h * 0.34f, w * 0.8f, h * 0.34f + 3, lead);
                    r.FillRect(w * 0.2f, h * 0.62f, w * 0.8f, h * 0.62f + 3, lead);
                    break;
                }
                case 3:
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.49f, stone);
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.42f, lead);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        r.FillEllipse(cx + Mathf.Cos(a) * w * 0.25f, cy + Mathf.Sin(a) * h * 0.25f, w * 0.1f, h * 0.1f, i % 2 == 0 ? glass : Hex("2c4fb8"));
                        r.FillTriangle(V(cx + Mathf.Cos(a - 0.2f) * w * 0.4f, cy + Mathf.Sin(a - 0.2f) * h * 0.4f), V(cx + Mathf.Cos(a + 0.2f) * w * 0.4f, cy + Mathf.Sin(a + 0.2f) * h * 0.4f), V(cx + Mathf.Cos(a) * w * 0.32f, cy + Mathf.Sin(a) * h * 0.32f), Hex("e0b64a"));
                    }
                    r.FillCircle(cx, cy, w * 0.12f, Hex("e0b64a"));
                    r.FillCircle(cx, cy, w * 0.07f, glass);
                    break;
                case 4:
                case 5:
                {
                    MatRect(r, m, 0, 0, w, h, seed);
                    int lobes = v == 4 ? 3 : 4;
                    for (int i = 0; i < lobes; i++)
                    {
                        float a = Mathf.PI / 2f + i * Mathf.PI * 2f / lobes;
                        r.FillCircle(cx + Mathf.Cos(a) * w * 0.17f, cy + Mathf.Sin(a) * h * 0.17f, Mathf.Min(w, h) * 0.17f, glass);
                    }
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.14f, glass);
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.05f, lead);
                    break;
                }
                case 6:
                case 7:
                    MatRect(r, m, w * 0.28f, 0, w * 0.72f, h, seed);
                    if (v == 6)
                    {
                        r.FillRect(w * 0.34f, h * 0.08f, w * 0.66f, h * 0.92f, Hex("3a2a5a"));
                        r.FillRect(cx - 1, h * 0.08f, cx + 1, h * 0.92f, lead);
                        r.FillRect(w * 0.34f, cy - 1, w * 0.66f, cy + 1, lead);
                        MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.05f, w * 0.27f, h * 0.95f, seed + 1);
                        MatRect(r, DecoMaterial.Oak, w * 0.73f, h * 0.05f, w * 0.95f, h * 0.95f, seed + 2);
                    }
                    else
                    {
                        MatRect(r, DecoMaterial.Oak, w * 0.33f, h * 0.05f, w * 0.67f, h * 0.95f, seed + 1);
                        r.FillRect(cx - 1, h * 0.05f, cx + 1, h * 0.95f, D(WoodD, 0.3f));
                        r.FillRect(w * 0.36f, h * 0.25f, w * 0.64f, h * 0.29f, IronC);
                        r.FillRect(w * 0.36f, h * 0.71f, w * 0.64f, h * 0.75f, IronC);
                        r.FillRect(cx - w * 0.06f, h * 0.55f, cx - w * 0.02f, h * 0.65f, D(WoodD, 0.5f));
                        r.FillRect(cx + w * 0.02f, h * 0.55f, cx + w * 0.06f, h * 0.65f, D(WoodD, 0.5f));
                    }
                    break;
                case 8:
                    MatRect(r, m, 0, 0, w, h, seed);
                    r.FillRect(w * 0.18f, h * 0.15f, w * 0.82f, h * 0.85f, Hex("14101c"));
                    for (int i = 0; i < 3; i++) r.FillRect(w * (0.3f + i * 0.2f) - 2, h * 0.15f, w * (0.3f + i * 0.2f) + 2, h * 0.85f, IronC);
                    r.FillRect(w * 0.18f, cy - 2, w * 0.82f, cy + 2, IronC);
                    break;
                default:  // 10 oriel: bay window on corbels
                    MatRect(r, m, w * 0.1f, h * 0.15f, w * 0.9f, h * 0.9f, seed);
                    MatRect(r, DecoMaterial.Slate, 0, h * 0.88f, w, h, seed + 1);
                    r.FillTriangle(V(w * 0.1f, h * 0.15f), V(w * 0.9f, h * 0.15f), V(cx, 0), stone);
                    for (int i = 0; i < 3; i++)
                    {
                        float x0 = w * (0.16f + i * 0.24f), x1 = x0 + w * 0.18f;
                        r.FillRect(x0, h * 0.3f, x1, h * 0.8f, glass);
                        r.FillRect((x0 + x1) / 2f - 1, h * 0.3f, (x0 + x1) / 2f + 1, h * 0.8f, lead);
                        r.FillRect(x0, h * 0.55f, x1, h * 0.55f + 2, lead);
                    }
                    break;
            }
        }

        static void DrawDoor(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var wood = Mat(def, DecoMaterial.Oak);
            var mask = NewMask(w, h);
            int v = def.variant;   // 0 oak, 1 iron-banded, 2 gothic, 3 double, 4 dungeon, 5 trapdoor
            if (v == 5)
            {
                MatRect(r, wood, 0, 0, w, h, seed);
                r.RectOutline(0, 0, w, h, 4, D(WoodD, 0.2f));
                r.FillRect(w * 0.1f, h * 0.45f, w * 0.9f, h * 0.55f, IronC);
                r.Ring(cx, h * 0.5f, w * 0.1f, w * 0.06f, IronC);
                return;
            }
            // stone surround
            MatRect(r, DecoMaterial.Granite, 0, 0, w, h * 0.7f, seed + 9);
            var sur = NewMask(w, h);
            sur.FillEllipse(cx, h * 0.7f, w * 0.5f, h * 0.3f, White);
            ClearRect(sur, 0, 0, w, h * 0.7f);
            MatMask(r, DecoMaterial.Granite, sur, seed + 9);
            // leaf
            if (v == 2) { mask.FillRect(w * 0.15f, 0, w * 0.85f, h * 0.62f, White); mask.FillPolygon(new[] { V(w * 0.15f, h * 0.62f), V(cx, h * 0.92f), V(w * 0.85f, h * 0.62f) }, White); mask.FillEllipse(cx, h * 0.62f, w * 0.35f, h * 0.24f, White); }
            else { mask.FillRect(w * 0.15f, 0, w * 0.85f, h * 0.66f, White); mask.FillEllipse(cx, h * 0.66f, w * 0.35f, h * 0.25f, White); }
            ClearRect(mask, 0, h * 0.92f, w, h);
            MatMask(r, wood, mask, seed);
            var band = IronC;
            if (v == 1 || v == 4)
            {
                for (int i = 0; i < 3; i++)
                {
                    float y = h * (0.15f + i * 0.25f);
                    r.FillRect(w * 0.15f, y, w * 0.85f, y + 4, band);
                    for (int k = 0; k < 4; k++) r.FillCircle(w * (0.22f + k * 0.19f), y + 2, 1.6f, L(band, 0.4f));
                }
            }
            if (v == 3) r.FillRect(cx - 1.5f, 0, cx + 1.5f, h * 0.9f, D(WoodD, 0.5f));
            if (v == 4)
            {
                r.FillRect(w * 0.35f, h * 0.55f, w * 0.65f, h * 0.75f, Hex("14101c"));
                for (int i = 0; i < 3; i++) r.FillRect(w * (0.4f + i * 0.1f) - 1, h * 0.55f, w * (0.4f + i * 0.1f) + 1, h * 0.75f, band);
            }
            // handle / ring
            if (v == 3)
            {
                r.Ring(cx - w * 0.08f, h * 0.4f, w * 0.045f, w * 0.025f, band);
                r.Ring(cx + w * 0.08f, h * 0.4f, w * 0.045f, w * 0.025f, band);
            }
            else r.Ring(w * 0.7f, h * 0.4f, w * 0.05f, w * 0.03f, band);
            Shadow(r, w * 0.15f, 0, w * 0.85f, 3, 0.3f);
        }

        // ---- furnishings ----------------------------------------------------------------------------

        static void DrawFurniture(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var wood = Mat(def, DecoMaterial.Oak);
            var wb = BaseColor(wood);
            var wd = D(wb, 0.4f);
            var cloth = def.primaryColor;
            var trim = def.secondaryColor;
            var rng = new Rng(seed);
            switch (def.variant)
            {
                case 0:   // throne
                    MatRect(r, wood, w * 0.1f, 0, w * 0.9f, h * 0.12f, seed);
                    MatRect(r, wood, w * 0.2f, h * 0.12f, w * 0.8f, h * 0.95f, seed + 1);
                    r.FillTriangle(V(w * 0.2f, h * 0.9f), V(w * 0.8f, h * 0.9f), V(cx, h), GoldC);
                    r.FillRect(w * 0.26f, h * 0.15f, w * 0.74f, h * 0.85f, cloth);
                    r.FillRect(w * 0.26f, h * 0.38f, w * 0.74f, h * 0.45f, D(cloth, 0.25f));
                    r.FillRect(w * 0.1f, h * 0.35f, w * 0.22f, h * 0.55f, wd);
                    r.FillRect(w * 0.78f, h * 0.35f, w * 0.9f, h * 0.55f, wd);
                    r.FillCircle(w * 0.16f, h * 0.55f, w * 0.06f, GoldC);
                    r.FillCircle(w * 0.84f, h * 0.55f, w * 0.06f, GoldC);
                    r.FillCircle(cx, h * 0.72f, w * 0.07f, trim);
                    break;
                case 1:   // table
                    MatRect(r, wood, 0, h * 0.7f, w, h * 0.85f, seed);
                    r.FillRect(w * 0.08f, h * 0.05f, w * 0.16f, h * 0.7f, wd);
                    r.FillRect(w * 0.84f, h * 0.05f, w * 0.92f, h * 0.7f, wd);
                    r.FillRect(w * 0.08f, h * 0.35f, w * 0.92f, h * 0.4f, wd);
                    r.FillRect(0, h * 0.85f, w, h * 0.92f, L(wb, 0.15f));
                    r.FillRect(w * 0.3f, h * 0.92f, w * 0.7f, h, trim);   // cloth runner
                    r.FillEllipse(w * 0.5f, h * 0.97f, w * 0.12f, h * 0.05f, Hex("d0d0d8"));
                    break;
                case 2:   // chair
                    r.FillRect(w * 0.2f, 0, w * 0.28f, h * 0.5f, wd);
                    r.FillRect(w * 0.72f, 0, w * 0.8f, h * 0.95f, wd);
                    MatRect(r, wood, w * 0.15f, h * 0.45f, w * 0.85f, h * 0.55f, seed);
                    r.FillRect(w * 0.6f, h * 0.55f, w * 0.85f, h * 0.9f, wb);
                    r.FillRect(w * 0.64f, h * 0.6f, w * 0.8f, h * 0.85f, cloth);
                    r.FillRect(w * 0.15f, h * 0.55f, w * 0.85f, h * 0.62f, cloth);
                    break;
                case 3:   // bookshelf
                    MatRect(r, wood, 0, 0, w, h, seed);
                    for (int row = 0; row < 3; row++)
                    {
                        float y0 = h * (0.08f + row * 0.3f), y1 = y0 + h * 0.24f;
                        r.FillRect(w * 0.08f, y0, w * 0.92f, y1, Hex("2a2030"));
                        float x = w * 0.1f;
                        while (x < w * 0.88f)
                        {
                            float bw = rng.Range(w * 0.06f, w * 0.12f), bh = rng.Range(h * 0.14f, h * 0.22f);
                            var c = new[] { Hex("b8302c"), Hex("2c4fb8"), Hex("2f8a3a"), Hex("e0b64a"), Hex("6a3cff"), Hex("d8d0c0") }[rng.Int(6)];
                            r.FillRect(x, y0, x + bw, y0 + bh, c);
                            r.FillRect(x + 1, y0 + bh * 0.3f, x + bw - 1, y0 + bh * 0.34f, A(GoldC, 0.6f));
                            x += bw + 1;
                        }
                    }
                    break;
                case 4:   // chest closed
                case 5:   // chest open
                    MatRect(r, wood, w * 0.05f, 0, w * 0.95f, h * 0.5f, seed);
                    if (def.variant == 4)
                    {
                        r.FillRoundRect(w * 0.05f, h * 0.45f, w * 0.95f, h * 0.8f, h * 0.2f, wb);
                        r.FillRect(w * 0.05f, h * 0.45f, w * 0.95f, h * 0.55f, wb);
                        r.FillRect(w * 0.2f, h * 0.05f, w * 0.26f, h * 0.78f, IronC);
                        r.FillRect(w * 0.74f, h * 0.05f, w * 0.8f, h * 0.78f, IronC);
                        r.FillRect(cx - w * 0.06f, h * 0.4f, cx + w * 0.06f, h * 0.55f, GoldC);
                    }
                    else
                    {
                        r.FillRoundRect(w * 0.05f, h * 0.55f, w * 0.95f, h * 0.95f, h * 0.15f, wd);
                        r.FillRect(w * 0.05f, h * 0.55f, w * 0.95f, h * 0.62f, wd);
                        for (int i = 0; i < 14; i++) r.FillCircle(w * rng.Range(0.15f, 0.85f), h * rng.Range(0.45f, 0.62f), 3, i % 3 == 0 ? Hex("ff7fd1") : GoldC);
                        r.FillRect(w * 0.2f, h * 0.05f, w * 0.26f, h * 0.5f, IronC);
                        r.FillRect(w * 0.74f, h * 0.05f, w * 0.8f, h * 0.5f, IronC);
                    }
                    break;
                case 6:   // bed
                    r.FillRect(w * 0.04f, 0, w * 0.1f, h * 0.7f, wd);
                    r.FillRect(w * 0.9f, 0, w * 0.96f, h * 0.95f, wd);
                    MatRect(r, wood, w * 0.05f, h * 0.15f, w * 0.95f, h * 0.3f, seed);
                    r.FillRect(w * 0.08f, h * 0.3f, w * 0.92f, h * 0.55f, cloth);
                    r.FillRect(w * 0.08f, h * 0.5f, w * 0.92f, h * 0.55f, L(cloth, 0.3f));
                    r.FillEllipse(w * 0.78f, h * 0.62f, w * 0.14f, h * 0.1f, Hex("f4efe4"));
                    r.FillRect(w * 0.88f, h * 0.7f, w * 0.98f, h * 0.78f, wd);
                    break;
                case 7:   // wardrobe
                    MatRect(r, wood, 0, 0, w, h, seed);
                    r.RectOutline(w * 0.08f, h * 0.08f, cx - 1, h * 0.92f, 2, wd);
                    r.RectOutline(cx + 1, h * 0.08f, w * 0.92f, h * 0.92f, 2, wd);
                    r.FillCircle(cx - w * 0.06f, h * 0.5f, 2.5f, GoldC);
                    r.FillCircle(cx + w * 0.06f, h * 0.5f, 2.5f, GoldC);
                    r.FillTriangle(V(0, h * 0.95f), V(w, h * 0.95f), V(cx, h), wd);
                    break;
                case 8:   // wine rack / barrel rack
                    MatRect(r, wood, 0, 0, w, h, seed);
                    for (int row = 0; row < 2; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        float x = w * (0.2f + col * 0.3f), y = h * (0.3f + row * 0.4f);
                        r.FillCircle(x, y, w * 0.11f, Hex("2a2030"));
                        r.FillCircle(x, y, w * 0.08f, Hex("3a2a1a"));
                        r.FillCircle(x, y, w * 0.03f, Hex("8a2a2a"));
                    }
                    break;
                case 9:   // cauldron
                    r.FillRect(w * 0.15f, 0, w * 0.22f, h * 0.15f, IronC);
                    r.FillRect(w * 0.78f, 0, w * 0.85f, h * 0.15f, IronC);
                    r.FillEllipse(cx, h * 0.45f, w * 0.45f, h * 0.38f, IronC);
                    r.FillRect(w * 0.05f, h * 0.5f, w * 0.95f, h * 0.75f, IronC);
                    r.FillEllipse(cx, h * 0.75f, w * 0.45f, h * 0.1f, D(IronC, 0.3f));
                    r.FillEllipse(cx, h * 0.75f, w * 0.38f, h * 0.07f, cloth);
                    r.FillCircle(cx - w * 0.15f, h * 0.8f, 3, L(cloth, 0.4f));
                    r.FillCircle(cx + w * 0.1f, h * 0.86f, 4, A(L(cloth, 0.3f), 0.7f));
                    r.FillCircle(cx + w * 0.2f, h * 0.95f, 3, A(L(cloth, 0.5f), 0.5f));
                    r.EllipseRing(cx, h * 0.55f, w * 0.46f, h * 0.06f, 2, D(IronC, 0.3f));
                    break;
                case 10:  // anvil
                    r.FillRect(w * 0.3f, 0, w * 0.7f, h * 0.15f, D(IronC, 0.2f));
                    r.FillRect(w * 0.38f, h * 0.15f, w * 0.62f, h * 0.55f, IronC);
                    r.FillPolygon(new[] { V(0, h * 0.7f), V(w * 0.25f, h * 0.55f), V(w * 0.85f, h * 0.55f), V(w, h * 0.72f), V(w, h * 0.9f), V(0, h * 0.9f) }, IronC);
                    r.FillRect(0, h * 0.86f, w, h * 0.9f, L(IronC, 0.35f));
                    break;
                case 11:  // spinning wheel
                    r.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.1f, wd);
                    r.Ring(w * 0.4f, h * 0.55f, w * 0.34f, w * 0.29f, wb);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; r.Line(w * 0.4f, h * 0.55f, w * 0.4f + Mathf.Cos(a) * w * 0.3f, h * 0.55f + Mathf.Sin(a) * w * 0.3f, 1.5f, wd); }
                    r.FillRect(w * 0.38f, h * 0.1f, w * 0.42f, h * 0.55f, wd);
                    r.FillRect(w * 0.78f, h * 0.1f, w * 0.84f, h * 0.7f, wd);
                    r.FillEllipse(w * 0.81f, h * 0.78f, w * 0.08f, h * 0.12f, Hex("f4efe4"));
                    break;
                case 12:  // loom
                    r.FillRect(w * 0.08f, 0, w * 0.14f, h, wd);
                    r.FillRect(w * 0.86f, 0, w * 0.92f, h, wd);
                    r.FillRect(w * 0.08f, h * 0.9f, w * 0.92f, h * 0.96f, wd);
                    r.FillRect(w * 0.08f, h * 0.15f, w * 0.92f, h * 0.2f, wd);
                    for (int i = 0; i < 12; i++) r.FillRect(w * (0.18f + i * 0.06f), h * 0.2f, w * (0.18f + i * 0.06f) + 1, h * 0.9f, Hex("e8dcc0"));
                    r.FillRect(w * 0.14f, h * 0.2f, w * 0.86f, h * 0.5f, cloth);
                    r.FillRect(w * 0.14f, h * 0.32f, w * 0.86f, h * 0.36f, trim);
                    break;
                case 13:  // desk with lectern
                    MatRect(r, wood, w * 0.1f, 0, w * 0.9f, h * 0.55f, seed);
                    r.FillPolygon(new[] { V(w * 0.05f, h * 0.55f), V(w * 0.95f, h * 0.55f), V(w * 0.95f, h * 0.7f), V(w * 0.05f, h * 0.85f) }, wb);
                    r.FillPolygon(new[] { V(w * 0.2f, h * 0.62f), V(w * 0.8f, h * 0.62f), V(w * 0.8f, h * 0.74f), V(w * 0.2f, h * 0.86f) }, Hex("f4efe4"));
                    for (int i = 0; i < 4; i++) r.Line(w * 0.25f, h * (0.66f + i * 0.045f), w * 0.75f, h * (0.56f + i * 0.045f), 1, Hex("3a2a1a"));
                    r.FillRect(w * 0.82f, h * 0.7f, w * 0.86f, h * 0.95f, Hex("f4efe4"));
                    r.FillTriangle(V(w * 0.82f, h * 0.95f), V(w * 0.86f, h * 0.95f), V(w * 0.84f, h), Flame);
                    break;
                case 14:  // altar
                    MatRect(r, DecoMaterial.Marble, w * 0.1f, 0, w * 0.9f, h * 0.6f, seed);
                    MatRect(r, DecoMaterial.Marble, 0, h * 0.6f, w, h * 0.72f, seed + 1);
                    r.FillRect(w * 0.2f, 0, w * 0.8f, h * 0.6f, A(cloth, 0.8f));
                    r.FillRect(w * 0.2f, h * 0.2f, w * 0.8f, h * 0.26f, trim);
                    r.FillRect(w * 0.3f, h * 0.72f, w * 0.34f, h * 0.92f, Hex("f4efe4"));
                    r.FillRect(w * 0.66f, h * 0.72f, w * 0.7f, h * 0.92f, Hex("f4efe4"));
                    r.FillCircle(w * 0.32f, h * 0.95f, 3, Flame); r.FillCircle(w * 0.68f, h * 0.95f, 3, Flame);
                    r.FillRect(cx - 2, h * 0.72f, cx + 2, h, GoldC);
                    r.FillRect(cx - w * 0.1f, h * 0.88f, cx + w * 0.1f, h * 0.92f, GoldC);
                    break;
                case 15:  // rug
                    r.FillRect(0, 0, w, h, cloth);
                    r.RectOutline(0, 0, w, h, 4, trim);
                    r.RectOutline(w * 0.15f, h * 0.2f, w * 0.85f, h * 0.8f, 2, trim);
                    r.FillPolygon(new[] { V(cx - w * 0.12f, h * 0.5f), V(cx, h * 0.8f), V(cx + w * 0.12f, h * 0.5f), V(cx, h * 0.2f) }, trim);
                    for (int i = 0; i < w / 6; i++) r.FillRect(i * 6, 0, i * 6 + 1, 3, D(cloth, 0.4f));
                    break;
                case 16:  // painting
                    MatRect(r, DecoMaterial.Gold, 0, 0, w, h, seed);
                    r.FillRect(w * 0.12f, h * 0.12f, w * 0.88f, h * 0.88f, Hex("6a8fc0"));
                    r.FillRect(w * 0.12f, h * 0.12f, w * 0.88f, h * 0.45f, Hex("3f8a3a"));
                    r.FillTriangle(V(w * 0.3f, h * 0.45f), V(w * 0.7f, h * 0.45f), V(cx, h * 0.75f), Hex("8f8c94"));
                    r.FillRect(w * 0.44f, h * 0.45f, w * 0.56f, h * 0.7f, Hex("8f8c94"));
                    r.FillCircle(w * 0.72f, h * 0.72f, w * 0.06f, Hex("fff2a0"));
                    break;
                case 17:  // mirror
                    r.FillEllipse(cx, h * 0.55f, w * 0.45f, h * 0.42f, GoldC);
                    r.FillEllipse(cx, h * 0.55f, w * 0.36f, h * 0.34f, Hex("bcd6e8"));
                    r.Line(cx - w * 0.2f, h * 0.3f, cx + w * 0.1f, h * 0.85f, 3, A(White, 0.5f));
                    r.FillRect(w * 0.35f, 0, w * 0.65f, h * 0.15f, wd);
                    break;
                case 18:  // treasure pile
                    r.FillEllipse(cx, h * 0.25f, w * 0.48f, h * 0.25f, D(GoldC, 0.15f));
                    r.FillEllipse(cx - w * 0.1f, h * 0.45f, w * 0.3f, h * 0.22f, GoldC);
                    r.FillEllipse(cx + w * 0.2f, h * 0.35f, w * 0.22f, h * 0.18f, GoldC);
                    for (int i = 0; i < 24; i++) r.FillCircle(w * rng.Range(0.08f, 0.92f), h * rng.Range(0.05f, 0.6f), 2.5f, i % 2 == 0 ? L(GoldC, 0.3f) : D(GoldC, 0.25f));
                    r.FillPolygon(new[] { V(cx - w * 0.08f, h * 0.65f), V(cx + w * 0.08f, h * 0.65f), V(cx + w * 0.05f, h * 0.8f), V(cx - w * 0.05f, h * 0.8f) }, Hex("ff7fd1"));
                    r.FillPolygon(new[] { V(cx - w * 0.3f, h * 0.55f), V(cx - w * 0.2f, h * 0.55f), V(cx - w * 0.15f, h * 0.7f), V(cx - w * 0.35f, h * 0.7f) }, GoldC);
                    r.FillTriangle(V(cx - w * 0.35f, h * 0.7f), V(cx - w * 0.15f, h * 0.7f), V(cx - w * 0.25f, h * 0.85f), GoldC);
                    r.FillCircle(cx + w * 0.25f, h * 0.6f, w * 0.06f, Hex("6ad4ff"));
                    break;
                case 19:  // coin sacks
                    r.FillEllipse(w * 0.32f, h * 0.35f, w * 0.28f, h * 0.35f, Hex("c9b27a"));
                    r.FillEllipse(w * 0.7f, h * 0.3f, w * 0.26f, h * 0.3f, Hex("b9a06a"));
                    r.FillRect(w * 0.26f, h * 0.65f, w * 0.38f, h * 0.85f, Hex("c9b27a"));
                    r.FillRect(w * 0.64f, h * 0.55f, w * 0.76f, h * 0.75f, Hex("b9a06a"));
                    r.FillRect(w * 0.24f, h * 0.66f, w * 0.4f, h * 0.7f, Hex("8a5a2a"));
                    r.FillRect(w * 0.62f, h * 0.56f, w * 0.78f, h * 0.6f, Hex("8a5a2a"));
                    for (int i = 0; i < 6; i++) r.FillCircle(w * rng.Range(0.1f, 0.9f), h * rng.Range(0.05f, 0.15f), 2.5f, GoldC);
                    break;
                case 20:  // potion shelf
                    MatRect(r, wood, 0, h * 0.4f, w, h * 0.5f, seed);
                    MatRect(r, wood, 0, 0, w, h * 0.08f, seed + 1);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = w * (0.15f + i * 0.23f);
                        var c = new[] { Hex("6ad4ff"), Hex("ff4a4a"), Hex("5fff7a"), Hex("b070ff") }[i];
                        r.FillRect(x - 4, h * 0.5f, x + 4, h * 0.7f, c);
                        r.FillCircle(x, h * 0.62f, 6, c);
                        r.FillRect(x - 2, h * 0.7f, x + 2, h * 0.82f, Hex("d0d0d8"));
                        r.FillRect(x - 3, h * 0.82f, x + 3, h * 0.86f, Hex("8a5a2a"));
                        r.FillCircle(x - 2, h * 0.6f, 1.5f, A(White, 0.6f));
                    }
                    break;
                case 21:  // scroll shelf
                    MatRect(r, wood, 0, 0, w, h, seed);
                    for (int row = 0; row < 2; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        float x = w * (0.2f + col * 0.3f), y = h * (0.3f + row * 0.4f);
                        r.FillCircle(x, y, w * 0.12f, Hex("2a2030"));
                        r.FillCircle(x, y, w * 0.09f, Hex("e8dcc0"));
                        r.Ring(x, y, w * 0.05f, w * 0.03f, Hex("c9b27a"));
                    }
                    break;
                case 22:  // candelabra (also a light)
                    r.FillRect(w * 0.3f, 0, w * 0.7f, h * 0.06f, GoldC);
                    r.FillRect(cx - 2, h * 0.06f, cx + 2, h * 0.55f, GoldC);
                    r.FillRect(w * 0.15f, h * 0.5f, w * 0.85f, h * 0.55f, GoldC);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = w * (0.18f + i * 0.32f);
                        r.FillRect(x - 2.5f, h * 0.55f, x + 2.5f, h * 0.8f, Hex("f4efe4"));
                        r.FillCircle(x, h * 0.86f, 5, A(Flame, 0.35f));
                        r.FillTriangle(V(x - 3, h * 0.8f), V(x + 3, h * 0.8f), V(x, h * 0.95f), Flame);
                        r.FillTriangle(V(x - 1.5f, h * 0.81f), V(x + 1.5f, h * 0.81f), V(x, h * 0.88f), FlameCore);
                    }
                    break;
                case 23:  // crown on cushion
                    r.FillEllipse(cx, h * 0.25f, w * 0.45f, h * 0.22f, cloth);
                    r.FillEllipse(cx, h * 0.3f, w * 0.35f, h * 0.12f, L(cloth, 0.2f));
                    r.FillRect(w * 0.25f, h * 0.35f, w * 0.75f, h * 0.65f, GoldC);
                    for (int i = 0; i < 4; i++) r.FillTriangle(V(w * (0.25f + i * 0.167f), h * 0.65f), V(w * (0.25f + (i + 1) * 0.167f), h * 0.65f), V(w * (0.33f + i * 0.167f), h * 0.92f), GoldC);
                    for (int i = 0; i < 3; i++) r.FillCircle(w * (0.33f + i * 0.17f), h * 0.5f, 3, new[] { Hex("ff4a4a"), Hex("6ad4ff"), Hex("5fff7a") }[i]);
                    break;
                default:  // 24 bones cage
                    r.FillRect(cx - 2, h * 0.8f, cx + 2, h, IronC);
                    r.FillEllipse(cx, h * 0.45f, w * 0.38f, h * 0.38f, A(Hex("14101c"), 0.6f));
                    for (int i = 0; i < 5; i++) { float x = cx + (i - 2) * w * 0.16f; r.Line(x, h * 0.1f, x, h * 0.8f, 2, IronC); }
                    r.EllipseRing(cx, h * 0.8f, w * 0.34f, h * 0.06f, 2, IronC);
                    r.EllipseRing(cx, h * 0.12f, w * 0.34f, h * 0.06f, 2, IronC);
                    r.FillCircle(cx, h * 0.5f, w * 0.12f, Hex("f0e9d2"));
                    r.FillCircle(cx - 3, h * 0.5f, 2, Hex("14101c")); r.FillCircle(cx + 3, h * 0.5f, 2, Hex("14101c"));
                    r.Line(cx - w * 0.15f, h * 0.25f, cx + w * 0.15f, h * 0.35f, 2.5f, Hex("f0e9d2"));
                    break;
            }
        }

        static void DrawTapestry(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var cloth = def.primaryColor;
            var trim = def.secondaryColor;
            var dark = D(cloth, 0.3f);
            r.FillRect(w * 0.08f, h * 0.08f, w * 0.92f, h * 0.96f, cloth);
            for (int y = 0; y < h; y += 3) r.FillRect(w * 0.08f, y, w * 0.92f, y + 1, A(dark, 0.12f));   // weave
            r.FillRect(w * 0.08f, h * 0.08f, w * 0.92f, h * 0.14f, trim);
            r.FillRect(w * 0.08f, h * 0.9f, w * 0.92f, h * 0.96f, trim);
            r.RectOutline(w * 0.08f, h * 0.08f, w * 0.92f, h * 0.96f, 2, trim);
            for (float x = w * 0.1f; x < w * 0.9f; x += 6) r.FillTriangle(V(x, h * 0.08f), V(x + 4, h * 0.08f), V(x + 2, 0), trim);   // fringe
            r.FillRect(0, h * 0.94f, w, h, WoodD);   // rod
            r.FillCircle(w * 0.03f, h * 0.97f, 3, GoldC); r.FillCircle(w * 0.97f, h * 0.97f, 3, GoldC);
            float s = Mathf.Min(w, h) * 0.32f;
            switch (def.variant)   // 0 stripes, 1 lion, 2 cross, 3 chevron, 4 fleur, 5 tree, 6 dragon, 7 stars
            {
                case 0:
                    for (int i = 0; i < 4; i++) r.FillRect(w * 0.08f, h * (0.2f + i * 0.18f), w * 0.92f, h * (0.26f + i * 0.18f), trim);
                    break;
                case 1:
                    r.FillEllipse(cx, cy, s * 0.7f, s * 0.5f, trim);
                    r.FillCircle(cx + s * 0.6f, cy + s * 0.2f, s * 0.3f, trim);
                    r.FillRect(cx - s * 0.6f, cy - s * 0.9f, cx - s * 0.4f, cy - s * 0.3f, trim);
                    r.FillRect(cx + s * 0.3f, cy - s * 0.9f, cx + s * 0.5f, cy - s * 0.3f, trim);
                    r.Line(cx - s * 0.7f, cy + s * 0.2f, cx - s * 1.1f, cy + s * 0.8f, 3, trim);
                    r.FillCircle(cx + s * 0.7f, cy + s * 0.3f, s * 0.06f, cloth);
                    break;
                case 2:
                    r.FillRect(cx - s * 0.18f, cy - s * 0.9f, cx + s * 0.18f, cy + s * 0.9f, trim);
                    r.FillRect(cx - s * 0.9f, cy - s * 0.18f, cx + s * 0.9f, cy + s * 0.18f, trim);
                    break;
                case 3:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = cy - s * 0.8f + i * s * 0.6f;
                        r.FillPolygon(new[] { V(w * 0.1f, y), V(cx, y + s * 0.5f), V(w * 0.9f, y), V(w * 0.9f, y + s * 0.2f), V(cx, y + s * 0.7f), V(w * 0.1f, y + s * 0.2f) }, trim);
                    }
                    break;
                case 4:
                    r.FillPolygon(new[] { V(cx - s * 0.15f, cy - s * 0.2f), V(cx + s * 0.15f, cy - s * 0.2f), V(cx + s * 0.06f, cy + s * 0.9f), V(cx, cy + s), V(cx - s * 0.06f, cy + s * 0.9f) }, trim);
                    r.FillEllipse(cx - s * 0.45f, cy + s * 0.3f, s * 0.28f, s * 0.42f, trim);
                    r.FillEllipse(cx + s * 0.45f, cy + s * 0.3f, s * 0.28f, s * 0.42f, trim);
                    r.FillEllipse(cx - s * 0.4f, cy + s * 0.3f, s * 0.12f, s * 0.25f, cloth);
                    r.FillEllipse(cx + s * 0.4f, cy + s * 0.3f, s * 0.12f, s * 0.25f, cloth);
                    r.FillRect(cx - s * 0.5f, cy - s * 0.25f, cx + s * 0.5f, cy - s * 0.1f, trim);
                    r.FillRect(cx - s * 0.12f, cy - s * 0.8f, cx + s * 0.12f, cy - s * 0.25f, trim);
                    break;
                case 5:
                    r.FillRect(cx - s * 0.1f, cy - s * 0.9f, cx + s * 0.1f, cy, trim);
                    r.FillCircle(cx, cy + s * 0.35f, s * 0.55f, trim);
                    r.FillCircle(cx - s * 0.35f, cy + s * 0.1f, s * 0.35f, trim);
                    r.FillCircle(cx + s * 0.35f, cy + s * 0.1f, s * 0.35f, trim);
                    for (int i = 0; i < 6; i++) r.FillCircle(cx + Mathf.Cos(i) * s * 0.4f, cy + s * 0.3f + Mathf.Sin(i * 1.3f) * s * 0.3f, s * 0.07f, cloth);
                    break;
                case 6:
                    r.FillEllipse(cx, cy, s * 0.6f, s * 0.35f, trim);
                    r.FillTriangle(V(cx - s * 0.5f, cy + s * 0.2f), V(cx - s * 0.2f, cy + s * 0.3f), V(cx - s * 0.6f, cy + s * 0.9f), trim);
                    r.FillTriangle(V(cx, cy + s * 0.3f), V(cx + s * 0.3f, cy + s * 0.25f), V(cx + s * 0.2f, cy + s * 0.95f), trim);
                    r.FillRect(cx + s * 0.5f, cy + s * 0.1f, cx + s * 0.9f, cy + s * 0.35f, trim);
                    r.FillTriangle(V(cx + s * 0.9f, cy + s * 0.2f), V(cx + s * 1.2f, cy + s * 0.05f), V(cx + s * 1.1f, cy + s * 0.35f), Flame);
                    r.Line(cx - s * 0.6f, cy, cx - s * 1.1f, cy - s * 0.5f, 4, trim);
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        float x = w * (0.2f + (i % 3) * 0.3f), y = h * (0.25f + (i / 3) * 0.25f) + (i % 2) * h * 0.08f;
                        for (int k = 0; k < 5; k++) { float a = Mathf.PI / 2f + k * Mathf.PI * 2f / 5f; r.FillTriangle(V(x, y), V(x + Mathf.Cos(a - 0.3f) * s * 0.18f, y + Mathf.Sin(a - 0.3f) * s * 0.18f), V(x + Mathf.Cos(a) * s * 0.4f, y + Mathf.Sin(a) * s * 0.4f), trim); }
                    }
                    break;
            }
        }

        static void DrawArmoury(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var p = def.primaryColor;
            var s = def.secondaryColor;
            var steel = Hex("b8bcc8");
            var steelD = D(steel, 0.35f);
            void Sword(float x0, float y0, float x1, float y1)
            {
                r.Line(x0, y0, x1, y1, 3.5f, steelD);
                r.Line(x0, y0, x1, y1, 1.5f, steel);
                float dx = x1 - x0, dy = y1 - y0, len = Mathf.Sqrt(dx * dx + dy * dy);
                float gx = x0 + dx * 0.22f, gy = y0 + dy * 0.22f;
                r.Line(gx - dy / len * 6, gy + dx / len * 6, gx + dy / len * 6, gy - dx / len * 6, 3, GoldC);
                r.FillCircle(x0, y0, 3, GoldC);
            }
            void Shield(float x, float y, float sz, Color a, Color b)
            {
                r.FillPolygon(new[] { V(x - sz, y + sz * 0.9f), V(x + sz, y + sz * 0.9f), V(x + sz, y), V(x, y - sz * 1.1f), V(x - sz, y) }, D(a, 0.35f));
                r.FillPolygon(new[] { V(x - sz * 0.82f, y + sz * 0.75f), V(x + sz * 0.82f, y + sz * 0.75f), V(x + sz * 0.82f, y), V(x, y - sz * 0.9f), V(x - sz * 0.82f, y) }, a);
                r.FillRect(x - sz * 0.82f, y + sz * 0.2f, x + sz * 0.82f, y + sz * 0.4f, b);
                r.FillRect(x - sz * 0.1f, y - sz * 0.9f, x + sz * 0.1f, y + sz * 0.75f, b);
            }
            switch (def.variant)   // 0 heraldic shield, 1 crossed swords, 2 weapon rack, 3 armour stand, 4 bow rack, 5 spear rack, 6 shield wall, 7 helmet on stand, 8 axe, 9 banner shield
            {
                case 0:
                    Shield(cx, cy, Mathf.Min(w, h) * 0.45f, p, s);
                    break;
                case 1:
                    Shield(cx, cy, Mathf.Min(w, h) * 0.3f, p, s);
                    Sword(w * 0.1f, h * 0.1f, w * 0.9f, h * 0.9f);
                    Sword(w * 0.9f, h * 0.1f, w * 0.1f, h * 0.9f);
                    break;
                case 2:
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, 0, w * 0.95f, h * 0.1f, seed);
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.75f, w * 0.95f, h * 0.85f, seed + 1);
                    r.FillRect(w * 0.08f, 0, w * 0.14f, h * 0.85f, WoodD); r.FillRect(w * 0.86f, 0, w * 0.92f, h * 0.85f, WoodD);
                    Sword(w * 0.25f, h * 0.95f, w * 0.25f, h * 0.15f);
                    Sword(w * 0.5f, h * 0.95f, w * 0.5f, h * 0.15f);
                    r.Line(w * 0.75f, h * 0.15f, w * 0.75f, h * 0.95f, 3, WoodD);
                    r.FillPolygon(new[] { V(w * 0.75f, h * 0.7f), V(w * 0.9f, h * 0.78f), V(w * 0.9f, h * 0.95f), V(w * 0.75f, h * 0.9f) }, steel);
                    break;
                case 3:
                    r.FillRect(w * 0.3f, 0, w * 0.7f, h * 0.06f, WoodD);
                    r.FillRect(cx - 2, h * 0.06f, cx + 2, h * 0.9f, WoodD);
                    r.FillRoundRect(w * 0.25f, h * 0.35f, w * 0.75f, h * 0.75f, 8, steel);
                    r.FillRect(w * 0.1f, h * 0.55f, w * 0.25f, h * 0.72f, steel); r.FillRect(w * 0.75f, h * 0.55f, w * 0.9f, h * 0.72f, steel);
                    r.FillRect(w * 0.3f, h * 0.15f, w * 0.7f, h * 0.35f, steelD);
                    r.FillRect(w * 0.3f, h * 0.5f, w * 0.7f, h * 0.53f, steelD);
                    r.FillCircle(cx, h * 0.85f, w * 0.15f, steel);
                    r.FillRect(cx - w * 0.15f, h * 0.8f, cx + w * 0.15f, h * 0.84f, steelD);
                    r.FillRect(cx - w * 0.12f, h * 0.84f, cx + w * 0.12f, h * 0.86f, Hex("14101c"));
                    r.FillTriangle(V(cx - 2, h * 0.95f), V(cx + 2, h * 0.95f), V(cx - 8, h), p);
                    break;
                case 4:
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, 0, w * 0.95f, h * 0.1f, seed);
                    r.FillRect(w * 0.08f, 0, w * 0.14f, h, WoodD); r.FillRect(w * 0.86f, 0, w * 0.92f, h, WoodD);
                    for (int i = 0; i < 2; i++)
                    {
                        float x = w * (0.32f + i * 0.36f);
                        r.EllipseRing(x + w * 0.1f, h * 0.55f, w * 0.12f, h * 0.4f, 2.5f, WoodC);
                        r.Line(x - w * 0.01f, h * 0.15f, x - w * 0.01f, h * 0.95f, 1, Hex("e8dcc0"));
                    }
                    for (int i = 0; i < 3; i++) r.Line(w * 0.5f + i * 3, h * 0.12f, w * 0.5f + i * 3, h * 0.6f, 1.2f, WoodD);
                    break;
                case 5:
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, 0, w * 0.95f, h * 0.1f, seed);
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.5f, w * 0.95f, h * 0.56f, seed + 1);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = w * (0.2f + i * 0.2f);
                        r.FillRect(x - 1.5f, h * 0.1f, x + 1.5f, h * 0.85f, WoodC);
                        r.FillTriangle(V(x - 4, h * 0.82f), V(x + 4, h * 0.82f), V(x, h), steel);
                    }
                    break;
                case 6:
                    MatRect(r, DecoMaterial.Oak, 0, 0, w, h, seed);
                    for (int i = 0; i < 3; i++) Shield(w * (0.2f + i * 0.3f), cy, Mathf.Min(w, h) * 0.22f, i == 1 ? s : p, i == 1 ? p : s);
                    break;
                case 7:
                    r.FillRect(w * 0.3f, 0, w * 0.7f, h * 0.08f, WoodD);
                    r.FillRect(cx - 2, h * 0.08f, cx + 2, h * 0.45f, WoodD);
                    r.FillCircle(cx, h * 0.62f, w * 0.3f, steel);
                    r.FillRect(cx - w * 0.3f, h * 0.35f, cx + w * 0.3f, h * 0.62f, steel);
                    r.FillRect(cx - w * 0.28f, h * 0.5f, cx + w * 0.28f, h * 0.54f, steelD);
                    r.FillRect(cx - w * 0.22f, h * 0.55f, cx + w * 0.22f, h * 0.6f, Hex("14101c"));
                    r.FillRect(cx - 2, h * 0.35f, cx + 2, h * 0.6f, steelD);
                    r.FillTriangle(V(cx - 3, h * 0.9f), V(cx + 3, h * 0.9f), V(cx - w * 0.2f, h), p);
                    r.FillRect(cx - 3, h * 0.88f, cx + 3, h * 0.96f, p);
                    break;
                case 8:
                    r.Line(w * 0.3f, h * 0.05f, w * 0.7f, h * 0.9f, 4, WoodC);
                    r.FillPolygon(new[] { V(w * 0.55f, h * 0.55f), V(w * 0.9f, h * 0.6f), V(w * 0.95f, h * 0.95f), V(w * 0.6f, h * 0.85f) }, steel);
                    r.FillPolygon(new[] { V(w * 0.45f, h * 0.6f), V(w * 0.1f, h * 0.7f), V(w * 0.12f, h * 0.98f), V(w * 0.5f, h * 0.88f) }, steel);
                    r.Line(w * 0.9f, h * 0.6f, w * 0.95f, h * 0.95f, 1.5f, A(White, 0.5f));
                    break;
                default:
                    r.FillRect(0, h * 0.92f, w, h, WoodD);
                    r.FillRect(w * 0.15f, h * 0.2f, w * 0.85f, h * 0.92f, p);
                    r.FillTriangle(V(w * 0.15f, h * 0.2f), V(w * 0.85f, h * 0.2f), V(cx, 0), p);
                    r.FillRect(w * 0.15f, h * 0.8f, w * 0.85f, h * 0.86f, s);
                    Shield(cx, h * 0.5f, w * 0.25f, s, p);
                    break;
            }
        }

        // ---- lights -------------------------------------------------------------------------------

        static void Glow(Raster r, float x, float y, float rad, Color c)
        {
            for (int i = 0; i < 4; i++) r.FillCircle(x, y, rad * (1f - i * 0.22f), A(c, 0.08f + i * 0.04f));
        }

        static void DrawLantern(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var p = def.primaryColor;
            var s = def.secondaryColor;
            var rng = new Rng(seed);
            void FlameAt(float x, float y, float sz)
            {
                Glow(r, x, y, sz * 2.4f, p);
                r.FillTriangle(V(x - sz * 0.6f, y - sz * 0.5f), V(x + sz * 0.6f, y - sz * 0.5f), V(x, y + sz), p);
                r.FillCircle(x, y - sz * 0.2f, sz * 0.55f, p);
                r.FillTriangle(V(x - sz * 0.3f, y - sz * 0.3f), V(x + sz * 0.3f, y - sz * 0.3f), V(x, y + sz * 0.45f), FlameCore);
            }
            switch (def.variant)   // 0 brazier, 1 hanging lantern, 2 standing lantern, 3 paper lantern, 4 candles, 5 sconce, 6 chandelier, 7 campfire, 8 lamp post, 9 glow moss, 10 crystal lamp, 11 oil lamp, 12 beacon, 13 string lights
            {
                case 0:
                    r.FillRect(w * 0.2f, 0, w * 0.3f, h * 0.35f, s); r.FillRect(w * 0.7f, 0, w * 0.8f, h * 0.35f, s); r.FillRect(cx - 3, 0, cx + 3, h * 0.35f, s);
                    r.FillEllipse(cx, h * 0.45f, w * 0.42f, h * 0.2f, s);
                    r.FillRect(w * 0.08f, h * 0.45f, w * 0.92f, h * 0.55f, s);
                    r.FillEllipse(cx, h * 0.55f, w * 0.42f, h * 0.08f, D(s, 0.3f));
                    for (int i = 0; i < 6; i++) r.FillCircle(cx + rng.Range(-w * 0.3f, w * 0.3f), h * 0.56f, 4, Hex("3a2a1a"));
                    FlameAt(cx, h * 0.78f, w * 0.22f);
                    FlameAt(cx - w * 0.18f, h * 0.68f, w * 0.12f);
                    FlameAt(cx + w * 0.16f, h * 0.7f, w * 0.13f);
                    break;
                case 1:
                case 2:
                {
                    float top = def.variant == 1 ? h : h * 0.95f;
                    if (def.variant == 1) { r.FillRect(cx - 1.5f, h * 0.75f, cx + 1.5f, h, s); r.Ring(cx, h * 0.72f, w * 0.12f, w * 0.08f, s); }
                    else { r.FillRect(w * 0.25f, 0, w * 0.75f, h * 0.06f, s); r.FillRect(cx - 2, h * 0.06f, cx + 2, h * 0.2f, s); }
                    float y0 = def.variant == 1 ? h * 0.1f : h * 0.2f, y1 = def.variant == 1 ? h * 0.65f : h * 0.85f;
                    Glow(r, cx, (y0 + y1) / 2f, w * 0.7f, p);
                    r.FillRect(w * 0.2f, y0, w * 0.8f, y1, A(p, 0.85f));
                    r.FillRect(w * 0.2f, y0, w * 0.8f, y0 + 4, s); r.FillRect(w * 0.2f, y1 - 4, w * 0.8f, y1, s);
                    r.FillRect(w * 0.2f, y0, w * 0.26f, y1, s); r.FillRect(w * 0.74f, y0, w * 0.8f, y1, s); r.FillRect(cx - 1.5f, y0, cx + 1.5f, y1, s);
                    r.FillTriangle(V(w * 0.15f, y1), V(w * 0.85f, y1), V(cx, y1 + h * 0.12f), s);
                    FlameAt(cx, (y0 + y1) / 2f, w * 0.12f);
                    break;
                }
                case 3:
                    r.FillRect(cx - 1, h * 0.85f, cx + 1, h, s);
                    Glow(r, cx, h * 0.5f, w * 0.7f, p);
                    r.FillEllipse(cx, h * 0.5f, w * 0.42f, h * 0.36f, p);
                    for (int i = 1; i < 5; i++) r.FillRect(w * 0.08f, h * (0.14f + i * 0.14f), w * 0.92f, h * (0.14f + i * 0.14f) + 1.5f, A(D(p, 0.3f), 0.5f));
                    r.FillRect(w * 0.35f, h * 0.84f, w * 0.65f, h * 0.88f, s); r.FillRect(w * 0.35f, h * 0.12f, w * 0.65f, h * 0.16f, s);
                    for (int i = 0; i < 3; i++) r.FillRect(cx + (i - 1) * 5 - 1, 0, cx + (i - 1) * 5 + 1, h * 0.12f, Hex("b8302c"));
                    break;
                case 4:
                    for (int i = 0; i < 3; i++)
                    {
                        float x = w * (0.25f + i * 0.25f), top = h * (0.45f + (i % 2) * 0.2f);
                        r.FillRect(x - w * 0.06f, h * 0.05f, x + w * 0.06f, top, Hex("f4efe4"));
                        r.FillEllipse(x, top, w * 0.06f, 2, Hex("e8dcc0"));
                        FlameAt(x, top + h * 0.08f, w * 0.06f);
                    }
                    r.FillEllipse(cx, h * 0.05f, w * 0.45f, h * 0.05f, Hex("b8bcc8"));
                    break;
                case 5:
                    r.FillRect(w * 0.35f, 0, w * 0.65f, h * 0.35f, s);
                    r.FillPolygon(new[] { V(w * 0.25f, h * 0.35f), V(w * 0.75f, h * 0.35f), V(w * 0.65f, h * 0.5f), V(w * 0.35f, h * 0.5f) }, s);
                    r.FillRect(cx - w * 0.05f, h * 0.5f, cx + w * 0.05f, h * 0.7f, Hex("f4efe4"));
                    FlameAt(cx, h * 0.8f, w * 0.14f);
                    break;
                case 6:
                    r.FillRect(cx - 1.5f, h * 0.7f, cx + 1.5f, h, s);
                    r.EllipseRing(cx, h * 0.45f, w * 0.45f, h * 0.12f, 3, s);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = Mathf.PI * (0.1f + i * 0.2f);
                        float x = cx + Mathf.Cos(a) * w * 0.42f, y = h * 0.45f - Mathf.Sin(a) * h * 0.1f;
                        r.Line(cx, h * 0.7f, x, y, 1, s);
                        r.FillRect(x - 2, y, x + 2, y + h * 0.15f, Hex("f4efe4"));
                        FlameAt(x, y + h * 0.2f, w * 0.05f);
                    }
                    break;
                case 7:
                    for (int i = 0; i < 4; i++) r.Line(w * (0.1f + i * 0.2f), h * 0.05f, w * (0.35f + i * 0.15f), h * 0.22f, 5, i % 2 == 0 ? WoodD : Hex("3a2a1a"));
                    for (int i = 0; i < 6; i++) r.FillCircle(w * (0.08f + i * 0.17f), h * 0.05f, 4, Hex("6c6f78"));
                    FlameAt(cx, h * 0.55f, w * 0.3f);
                    FlameAt(cx - w * 0.2f, h * 0.4f, w * 0.15f);
                    FlameAt(cx + w * 0.2f, h * 0.42f, w * 0.16f);
                    for (int i = 0; i < 6; i++) r.FillCircle(cx + rng.Range(-w * 0.3f, w * 0.3f), h * rng.Range(0.75f, 0.98f), 1.5f, A(p, 0.8f));
                    break;
                case 8:
                    r.FillRect(w * 0.3f, 0, w * 0.7f, h * 0.05f, s);
                    r.FillRect(cx - 2.5f, h * 0.05f, cx + 2.5f, h * 0.8f, s);
                    r.Line(cx, h * 0.72f, cx + w * 0.3f, h * 0.8f, 2.5f, s);
                    Glow(r, cx + w * 0.3f, h * 0.64f, w * 0.4f, p);
                    r.FillRect(cx + w * 0.18f, h * 0.55f, cx + w * 0.42f, h * 0.76f, A(p, 0.85f));
                    r.RectOutline(cx + w * 0.18f, h * 0.55f, cx + w * 0.42f, h * 0.76f, 2, s);
                    r.FillTriangle(V(cx + w * 0.14f, h * 0.76f), V(cx + w * 0.46f, h * 0.76f), V(cx + w * 0.3f, h * 0.86f), s);
                    FlameAt(cx + w * 0.3f, h * 0.65f, w * 0.06f);
                    break;
                case 9:
                    for (int i = 0; i < 8; i++)
                    {
                        float x = rng.Range(w * 0.1f, w * 0.9f), y = rng.Range(h * 0.15f, h * 0.6f);
                        Glow(r, x, y, w * 0.25f, p);
                        r.FillEllipse(x, y, rng.Range(w * 0.08f, w * 0.16f), rng.Range(h * 0.06f, h * 0.12f), Color.Lerp(s, p, rng.Next()));
                    }
                    for (int i = 0; i < 10; i++) r.FillCircle(rng.Range(0, w), rng.Range(0, h), 1.5f, A(FlameCore, 0.8f));
                    break;
                case 10:
                    r.FillRect(w * 0.25f, 0, w * 0.75f, h * 0.1f, s);
                    r.FillRect(w * 0.35f, h * 0.1f, w * 0.65f, h * 0.2f, s);
                    Glow(r, cx, h * 0.58f, w * 0.75f, p);
                    r.FillPolygon(new[] { V(cx, h * 0.2f), V(w * 0.25f, h * 0.5f), V(cx, h * 0.98f), V(w * 0.75f, h * 0.5f) }, p);
                    r.FillPolygon(new[] { V(cx, h * 0.3f), V(w * 0.38f, h * 0.5f), V(cx, h * 0.85f), V(w * 0.5f, h * 0.5f) }, A(White, 0.45f));
                    break;
                case 11:
                    r.FillEllipse(cx, h * 0.3f, w * 0.4f, h * 0.22f, s);
                    r.FillRect(w * 0.7f, h * 0.35f, w * 0.95f, h * 0.45f, s);
                    r.FillRect(w * 0.15f, h * 0.05f, w * 0.85f, h * 0.15f, s);
                    r.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.06f, D(s, 0.3f));
                    FlameAt(w * 0.9f, h * 0.55f, w * 0.1f);
                    r.FillRect(cx - w * 0.05f, h * 0.5f, cx + w * 0.05f, h * 0.7f, s);
                    r.FillCircle(cx, h * 0.7f, w * 0.06f, s);
                    break;
                case 12:
                    MatRect(r, DecoMaterial.Iron, w * 0.3f, 0, w * 0.7f, h * 0.5f, seed);
                    r.FillRect(w * 0.15f, h * 0.45f, w * 0.85f, h * 0.55f, s);
                    r.FillRect(w * 0.15f, h * 0.45f, w * 0.2f, h * 0.65f, s); r.FillRect(w * 0.8f, h * 0.45f, w * 0.85f, h * 0.65f, s);
                    FlameAt(cx, h * 0.78f, w * 0.3f);
                    FlameAt(cx - w * 0.2f, h * 0.68f, w * 0.14f);
                    FlameAt(cx + w * 0.2f, h * 0.7f, w * 0.14f);
                    break;
                default:
                {
                    float px = 0, py = h * 0.9f;
                    for (int i = 1; i <= 12; i++)
                    {
                        float t = i / 12f, nx = t * w, ny = h * (0.9f - 0.35f * Mathf.Sin(t * Mathf.PI));
                        r.Line(px, py, nx, ny, 1.5f, s);
                        if (i < 12)
                        {
                            var c = new[] { p, Hex("6ad4ff"), Hex("5fff7a"), Hex("ff7fd1") }[i % 4];
                            r.FillRect(nx - 1, ny - 6, nx + 1, ny, s);
                            Glow(r, nx, ny - 10, 10, c);
                            r.FillEllipse(nx, ny - 10, 3.5f, 5, c);
                        }
                        px = nx; py = ny;
                    }
                    break;
                }
            }
        }

        // ---- yard, fences, siege ----------------------------------------------------------------------

        static void DrawYard(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var p = def.primaryColor;
            var s = def.secondaryColor;
            var rng = new Rng(seed);
            var straw = Hex("d8b85a");
            switch (def.variant)   // 0 hay round, 1 hay square, 2 cart wheel, 3 wagon, 4 trough, 5 rain barrel, 6 bell, 7 signpost, 8 milestone, 9 stocks, 10 training dummy, 11 archery target, 12 firewood, 13 crate stack, 14 sacks, 15 scarecrow, 16 wheelbarrow, 17 ladder, 18 rope coil, 19 bucket, 20 grindstone, 21 beehive, 22 dovecote, 23 chopping block
            {
                case 0:
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.48f, D(straw, 0.25f));
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.42f, straw);
                    for (int i = 0; i < 4; i++) r.Ring(cx, cy, Mathf.Min(w, h) * (0.1f + i * 0.09f), Mathf.Min(w, h) * (0.08f + i * 0.09f), A(D(straw, 0.35f), 0.7f));
                    for (int i = 0; i < 30; i++) r.Line(cx + rng.Range(-w * 0.4f, w * 0.4f), cy + rng.Range(-h * 0.4f, h * 0.4f), cx + rng.Range(-w * 0.4f, w * 0.4f), cy + rng.Range(-h * 0.4f, h * 0.4f), 0.8f, A(L(straw, 0.3f), 0.4f));
                    break;
                case 1:
                    MatRect(r, DecoMaterial.Thatch, w * 0.05f, 0, w * 0.95f, h * 0.9f, seed, straw);
                    r.FillRect(w * 0.05f, h * 0.3f, w * 0.95f, h * 0.34f, Hex("8a5a2a")); r.FillRect(w * 0.05f, h * 0.6f, w * 0.95f, h * 0.64f, Hex("8a5a2a"));
                    break;
                case 2:
                    r.Ring(cx, cy, Mathf.Min(w, h) * 0.48f, Mathf.Min(w, h) * 0.4f, IronC);
                    r.Ring(cx, cy, Mathf.Min(w, h) * 0.4f, Mathf.Min(w, h) * 0.32f, WoodC);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; r.Line(cx, cy, cx + Mathf.Cos(a) * w * 0.36f, cy + Mathf.Sin(a) * h * 0.36f, 3, WoodC); }
                    r.FillCircle(cx, cy, Mathf.Min(w, h) * 0.1f, IronC);
                    break;
                case 3:
                    r.Ring(w * 0.25f, h * 0.2f, h * 0.2f, h * 0.13f, IronC); r.Ring(w * 0.75f, h * 0.2f, h * 0.2f, h * 0.13f, IronC);
                    for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; r.Line(w * 0.25f, h * 0.2f, w * 0.25f + Mathf.Cos(a) * h * 0.15f, h * 0.2f + Mathf.Sin(a) * h * 0.15f, 2, WoodC); r.Line(w * 0.75f, h * 0.2f, w * 0.75f + Mathf.Cos(a) * h * 0.15f, h * 0.2f + Mathf.Sin(a) * h * 0.15f, 2, WoodC); }
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.3f, w * 0.95f, h * 0.62f, seed);
                    r.FillRect(w * 0.05f, h * 0.44f, w * 0.95f, h * 0.47f, IronC);
                    r.FillEllipse(cx, h * 0.72f, w * 0.42f, h * 0.2f, p);
                    r.FillEllipse(cx - w * 0.15f, h * 0.78f, w * 0.2f, h * 0.14f, L(p, 0.15f));
                    r.FillRect(w * 0.9f, h * 0.35f, w, h * 0.42f, WoodD);
                    break;
                case 4:
                    r.FillRect(w * 0.1f, 0, w * 0.2f, h * 0.35f, WoodD); r.FillRect(w * 0.8f, 0, w * 0.9f, h * 0.35f, WoodD);
                    MatRect(r, DecoMaterial.Oak, 0, h * 0.3f, w, h * 0.85f, seed);
                    r.FillRect(w * 0.06f, h * 0.65f, w * 0.94f, h * 0.85f, Hex("4a86c0"));
                    r.FillEllipse(w * 0.35f, h * 0.78f, w * 0.15f, h * 0.03f, A(White, 0.4f));
                    r.FillRect(0, h * 0.85f, w, h * 0.92f, WoodD);
                    break;
                case 5:
                    r.FillRoundRect(w * 0.1f, 0, w * 0.9f, h * 0.85f, w * 0.15f, WoodC);
                    r.FillRect(w * 0.1f, h * 0.15f, w * 0.9f, h * 0.22f, IronC); r.FillRect(w * 0.1f, h * 0.65f, w * 0.9f, h * 0.72f, IronC);
                    for (int i = 1; i < 4; i++) r.FillRect(w * 0.1f + i * w * 0.2f, 0, w * 0.1f + i * w * 0.2f + 2, h * 0.85f, A(WoodD, 0.5f));
                    r.FillEllipse(cx, h * 0.85f, w * 0.4f, h * 0.09f, Hex("4a86c0"));
                    r.FillEllipse(cx - w * 0.1f, h * 0.87f, w * 0.12f, h * 0.02f, A(White, 0.5f));
                    for (int i = 0; i < 3; i++) r.FillCircle(cx + (i - 1) * w * 0.15f, h * (0.9f + (i % 2) * 0.06f), 2, A(Hex("9ad0ff"), 0.8f));
                    break;
                case 6:
                    r.FillRect(w * 0.1f, h * 0.88f, w * 0.9f, h * 0.95f, WoodD);
                    r.FillRect(cx - 3, h * 0.78f, cx + 3, h * 0.88f, IronC);
                    r.FillPolygon(new[] { V(cx - w * 0.15f, h * 0.8f), V(cx + w * 0.15f, h * 0.8f), V(cx + w * 0.42f, h * 0.25f), V(cx - w * 0.42f, h * 0.25f) }, Hex("c99b3a"));
                    r.FillEllipse(cx, h * 0.25f, w * 0.42f, h * 0.1f, Hex("c99b3a"));
                    r.FillEllipse(cx, h * 0.25f, w * 0.42f, h * 0.05f, D(Hex("c99b3a"), 0.3f));
                    r.FillRect(cx - w * 0.42f, h * 0.3f, cx + w * 0.42f, h * 0.33f, D(Hex("c99b3a"), 0.3f));
                    r.FillCircle(cx, h * 0.14f, w * 0.06f, IronC);
                    r.Line(cx - w * 0.2f, h * 0.7f, cx - w * 0.3f, h * 0.35f, 2, A(White, 0.4f));
                    break;
                case 7:
                    r.FillRect(cx - 3, 0, cx + 3, h, WoodD);
                    r.FillPolygon(new[] { V(cx - 3, h * 0.72f), V(cx + w * 0.45f, h * 0.72f), V(cx + w * 0.5f, h * 0.8f), V(cx + w * 0.45f, h * 0.88f), V(cx - 3, h * 0.88f) }, WoodC);
                    r.FillPolygon(new[] { V(cx + 3, h * 0.5f), V(cx - w * 0.45f, h * 0.5f), V(cx - w * 0.5f, h * 0.58f), V(cx - w * 0.45f, h * 0.66f), V(cx + 3, h * 0.66f) }, WoodC);
                    for (int i = 0; i < 3; i++) { r.FillRect(cx + w * (0.08f + i * 0.12f), h * 0.78f, cx + w * (0.14f + i * 0.12f), h * 0.82f, WoodD); r.FillRect(cx - w * (0.14f + i * 0.12f), h * 0.56f, cx - w * (0.08f + i * 0.12f), h * 0.6f, WoodD); }
                    break;
                case 8:
                    MatRect(r, DecoMaterial.Limestone, w * 0.25f, 0, w * 0.75f, h * 0.8f, seed);
                    r.FillEllipse(cx, h * 0.8f, w * 0.25f, h * 0.15f, BaseColor(DecoMaterial.Limestone));
                    r.FillRect(w * 0.38f, h * 0.35f, w * 0.62f, h * 0.4f, A(Hex("3a3a3a"), 0.6f)); r.FillRect(w * 0.4f, h * 0.5f, w * 0.6f, h * 0.55f, A(Hex("3a3a3a"), 0.6f));
                    break;
                case 9:
                    r.FillRect(w * 0.12f, 0, w * 0.2f, h * 0.75f, WoodD); r.FillRect(w * 0.8f, 0, w * 0.88f, h * 0.75f, WoodD);
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.5f, w * 0.95f, h * 0.62f, seed);
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.62f, w * 0.95f, h * 0.75f, seed + 1);
                    for (int i = 0; i < 3; i++) r.FillCircle(w * (0.3f + i * 0.2f), h * 0.62f, w * 0.05f, Hex("2a2030"));
                    r.FillRect(w * 0.88f, h * 0.55f, w * 0.95f, h * 0.7f, IronC);
                    break;
                case 10:
                    r.FillRect(cx - 3, 0, cx + 3, h * 0.9f, WoodD);
                    r.FillRect(w * 0.15f, h * 0.55f, w * 0.85f, h * 0.62f, WoodD);
                    r.FillEllipse(cx, h * 0.45f, w * 0.2f, h * 0.2f, straw);
                    r.FillRect(cx - w * 0.2f, h * 0.35f, cx + w * 0.2f, h * 0.6f, straw);
                    r.FillCircle(cx, h * 0.78f, w * 0.14f, straw);
                    r.FillRect(cx - w * 0.14f, h * 0.7f, cx + w * 0.14f, h * 0.76f, Hex("b8bcc8"));
                    for (int i = 0; i < 12; i++) r.Line(cx + rng.Range(-w * 0.2f, w * 0.2f), h * rng.Range(0.35f, 0.6f), cx + rng.Range(-w * 0.2f, w * 0.2f), h * rng.Range(0.35f, 0.6f), 0.8f, A(D(straw, 0.4f), 0.6f));
                    r.FillRect(cx - w * 0.02f, h * 0.44f, cx + w * 0.02f, h * 0.5f, Hex("b8302c"));
                    break;
                case 11:
                    r.FillRect(w * 0.2f, 0, w * 0.3f, h * 0.35f, WoodD); r.FillRect(w * 0.7f, 0, w * 0.8f, h * 0.35f, WoodD);
                    r.FillCircle(cx, h * 0.58f, Mathf.Min(w, h) * 0.4f, straw);
                    r.Ring(cx, h * 0.58f, Mathf.Min(w, h) * 0.32f, Mathf.Min(w, h) * 0.24f, Hex("2c4fb8"));
                    r.Ring(cx, h * 0.58f, Mathf.Min(w, h) * 0.16f, Mathf.Min(w, h) * 0.08f, Hex("b8302c"));
                    r.FillCircle(cx, h * 0.58f, Mathf.Min(w, h) * 0.05f, GoldC);
                    r.Line(cx + w * 0.1f, h * 0.62f, cx + w * 0.4f, h * 0.9f, 2, WoodC);
                    r.FillTriangle(V(cx + w * 0.36f, h * 0.9f), V(cx + w * 0.42f, h * 0.84f), V(cx + w * 0.45f, h * 0.95f), Hex("b8302c"));
                    break;
                case 12:
                    for (int row = 0; row < 3; row++)
                    for (int i = 0; i < 4 - row % 2; i++)
                    {
                        float x = w * (0.14f + i * 0.24f + (row % 2) * 0.12f), y = h * (0.15f + row * 0.28f);
                        r.FillCircle(x, y, w * 0.12f, WoodD);
                        r.FillCircle(x, y, w * 0.09f, WoodC);
                        r.FillCircle(x, y, w * 0.045f, D(WoodC, 0.25f));
                    }
                    break;
                case 13:
                    for (int i = 0; i < 3; i++)
                    {
                        float x0 = i == 2 ? w * 0.25f : w * (0.02f + i * 0.5f), y0 = i == 2 ? h * 0.5f : 0, sz = w * 0.46f;
                        MatRect(r, DecoMaterial.Oak, x0, y0, x0 + sz, y0 + sz, seed + i);
                        r.RectOutline(x0, y0, x0 + sz, y0 + sz, 3, WoodD);
                        r.Line(x0, y0, x0 + sz, y0 + sz, 3, WoodD); r.Line(x0, y0 + sz, x0 + sz, y0, 3, WoodD);
                    }
                    break;
                case 14:
                    for (int i = 0; i < 3; i++)
                    {
                        float x = w * (0.22f + i * 0.28f), y = i == 1 ? h * 0.55f : h * 0.3f;
                        r.FillEllipse(x, y, w * 0.2f, h * 0.28f, i == 1 ? Hex("b9a06a") : Hex("c9b27a"));
                        r.FillRect(x - w * 0.08f, y + h * 0.22f, x + w * 0.08f, y + h * 0.36f, i == 1 ? Hex("b9a06a") : Hex("c9b27a"));
                        r.FillRect(x - w * 0.09f, y + h * 0.24f, x + w * 0.09f, y + h * 0.27f, Hex("8a5a2a"));
                    }
                    break;
                case 15:
                    r.FillRect(cx - 3, 0, cx + 3, h * 0.85f, WoodD);
                    r.FillRect(w * 0.1f, h * 0.6f, w * 0.9f, h * 0.65f, WoodD);
                    r.FillRect(cx - w * 0.2f, h * 0.35f, cx + w * 0.2f, h * 0.68f, p);
                    r.FillRect(cx - w * 0.2f, h * 0.5f, cx + w * 0.2f, h * 0.54f, s);
                    for (int i = 0; i < 8; i++) r.Line(cx + rng.Range(-w * 0.35f, w * 0.35f), h * rng.Range(0.6f, 0.68f), cx + rng.Range(-w * 0.45f, w * 0.45f), h * rng.Range(0.5f, 0.75f), 1.2f, straw);
                    r.FillCircle(cx, h * 0.78f, w * 0.13f, straw);
                    r.FillRect(cx - w * 0.22f, h * 0.86f, cx + w * 0.22f, h * 0.9f, WoodD);
                    r.FillTriangle(V(cx - w * 0.14f, h * 0.88f), V(cx + w * 0.14f, h * 0.88f), V(cx, h), WoodD);
                    r.FillCircle(cx - 4, h * 0.79f, 1.5f, Hex("14101c")); r.FillCircle(cx + 4, h * 0.79f, 1.5f, Hex("14101c"));
                    break;
                case 16:
                    r.Ring(w * 0.25f, h * 0.22f, h * 0.2f, h * 0.12f, IronC);
                    r.FillPolygon(new[] { V(w * 0.15f, h * 0.35f), V(w * 0.85f, h * 0.35f), V(w * 0.95f, h * 0.7f), V(w * 0.2f, h * 0.7f) }, WoodC);
                    r.FillRect(w * 0.15f, h * 0.35f, w * 0.85f, h * 0.4f, WoodD);
                    r.Line(w * 0.85f, h * 0.4f, w, h * 0.55f, 4, WoodD);
                    r.FillRect(w * 0.6f, 0, w * 0.68f, h * 0.35f, WoodD);
                    for (int i = 0; i < 6; i++) r.FillCircle(w * rng.Range(0.3f, 0.85f), h * rng.Range(0.62f, 0.8f), w * 0.07f, Color.Lerp(Hex("8f8c94"), Hex("5c5963"), rng.Next()));
                    break;
                case 17:
                    r.FillRect(w * 0.2f, 0, w * 0.3f, h, WoodC); r.FillRect(w * 0.7f, 0, w * 0.8f, h, WoodC);
                    for (int i = 0; i < 6; i++) r.FillRect(w * 0.3f, h * (0.08f + i * 0.16f), w * 0.7f, h * (0.08f + i * 0.16f) + 4, WoodD);
                    break;
                case 18:
                    for (int i = 0; i < 5; i++) r.EllipseRing(cx, h * (0.15f + i * 0.14f), w * 0.4f, h * 0.12f, 4, i % 2 == 0 ? Hex("c9b27a") : Hex("b9a06a"));
                    r.Line(cx + w * 0.3f, h * 0.7f, w * 0.95f, h * 0.95f, 4, Hex("c9b27a"));
                    break;
                case 19:
                    r.FillPolygon(new[] { V(w * 0.2f, 0), V(w * 0.8f, 0), V(w * 0.9f, h * 0.6f), V(w * 0.1f, h * 0.6f) }, WoodC);
                    r.FillRect(w * 0.1f, h * 0.15f, w * 0.9f, h * 0.2f, IronC); r.FillRect(w * 0.12f, h * 0.45f, w * 0.88f, h * 0.5f, IronC);
                    r.FillEllipse(cx, h * 0.6f, w * 0.4f, h * 0.08f, Hex("4a86c0"));
                    r.EllipseRing(cx, h * 0.72f, w * 0.38f, h * 0.24f, 3, IronC);
                    break;
                case 20:
                    r.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.12f, WoodD);
                    r.FillRect(w * 0.15f, h * 0.12f, w * 0.22f, h * 0.55f, WoodD); r.FillRect(w * 0.78f, h * 0.12f, w * 0.85f, h * 0.55f, WoodD);
                    r.FillCircle(cx, h * 0.6f, Mathf.Min(w, h) * 0.36f, Hex("8f8c94"));
                    r.Ring(cx, h * 0.6f, Mathf.Min(w, h) * 0.36f, Mathf.Min(w, h) * 0.3f, Hex("6c6a68"));
                    r.FillCircle(cx, h * 0.6f, w * 0.06f, WoodD);
                    r.Line(cx, h * 0.6f, cx + w * 0.4f, h * 0.9f, 3, IronC);
                    break;
                case 21:
                    for (int i = 0; i < 5; i++) r.FillEllipse(cx, h * (0.12f + i * 0.17f), w * (0.4f - Mathf.Abs(i - 2) * 0.05f), h * 0.1f, i % 2 == 0 ? straw : D(straw, 0.2f));
                    r.FillEllipse(cx, h * 0.9f, w * 0.18f, h * 0.08f, D(straw, 0.2f));
                    r.FillEllipse(cx, h * 0.14f, w * 0.06f, h * 0.04f, Hex("14101c"));
                    for (int i = 0; i < 5; i++) r.FillCircle(cx + rng.Range(-w * 0.45f, w * 0.45f), h * rng.Range(0.05f, 0.95f), 1.5f, Hex("e0b64a"));
                    break;
                case 22:
                    r.FillRect(cx - 3, 0, cx + 3, h * 0.3f, WoodD);
                    MatRect(r, DecoMaterial.Plaster, w * 0.2f, h * 0.3f, w * 0.8f, h * 0.8f, seed);
                    r.FillTriangle(V(w * 0.1f, h * 0.78f), V(w * 0.9f, h * 0.78f), V(cx, h), BaseColor(DecoMaterial.Slate));
                    for (int i = 0; i < 4; i++) r.FillEllipse(w * (0.32f + (i % 2) * 0.36f), h * (0.45f + (i / 2) * 0.2f), w * 0.06f, h * 0.06f, Hex("2a2030"));
                    r.FillEllipse(w * 0.25f, h * 0.83f, w * 0.06f, h * 0.04f, Hex("f4efe4"));
                    r.FillCircle(w * 0.3f, h * 0.85f, w * 0.03f, Hex("f4efe4"));
                    break;
                default:
                    r.FillRect(w * 0.2f, 0, w * 0.8f, h * 0.55f, WoodD);
                    r.FillEllipse(cx, h * 0.55f, w * 0.3f, h * 0.12f, WoodC);
                    r.EllipseRing(cx, h * 0.55f, w * 0.18f, h * 0.07f, 1.5f, D(WoodC, 0.3f));
                    r.Line(cx + w * 0.05f, h * 0.6f, cx + w * 0.35f, h * 0.95f, 3, WoodC);
                    r.FillPolygon(new[] { V(cx - w * 0.15f, h * 0.55f), V(cx + w * 0.12f, h * 0.55f), V(cx + w * 0.1f, h * 0.8f), V(cx - w * 0.2f, h * 0.75f) }, Hex("b8bcc8"));
                    break;
            }
        }

        static void DrawFence(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            switch (def.variant)   // 0 wood, 1 iron, 2 wood gate, 3 stone wall, 4 wattle
            {
                case 0:
                    r.FillRect(0, h * 0.35f, w, h * 0.45f, WoodC); r.FillRect(0, h * 0.7f, w, h * 0.8f, WoodC);
                    for (float x = 4; x < w; x += 16)
                    {
                        r.FillRect(x, 0, x + 6, h * 0.9f, WoodD);
                        r.FillTriangle(V(x, h * 0.9f), V(x + 6, h * 0.9f), V(x + 3, h), WoodD);
                    }
                    break;
                case 1:
                    r.FillRect(0, h * 0.1f, w, h * 0.15f, IronC); r.FillRect(0, h * 0.7f, w, h * 0.75f, IronC);
                    for (float x = 4; x < w; x += 12)
                    {
                        r.FillRect(x, 0, x + 2.5f, h * 0.88f, IronC);
                        r.FillTriangle(V(x - 2, h * 0.86f), V(x + 4.5f, h * 0.86f), V(x + 1.25f, h), IronC);
                    }
                    for (float x = 0; x < w; x += w / 2f) r.FillRect(x, 0, x + 6, h * 0.95f, IronC);
                    r.FillCircle(3, h * 0.96f, 4, IronC); r.FillCircle(w / 2f + 3, h * 0.96f, 4, IronC);
                    break;
                case 2:
                    r.FillRect(0, 0, w * 0.08f, h, WoodD); r.FillRect(w * 0.92f, 0, w, h, WoodD);
                    r.FillRect(w * 0.08f, h * 0.3f, w * 0.92f, h * 0.38f, WoodC); r.FillRect(w * 0.08f, h * 0.75f, w * 0.92f, h * 0.83f, WoodC);
                    r.Line(w * 0.1f, h * 0.3f, w * 0.9f, h * 0.8f, 5, WoodC);
                    for (float x = w * 0.14f; x < w * 0.9f; x += w * 0.16f) r.FillRect(x, h * 0.05f, x + 5, h * 0.92f, WoodD);
                    r.FillRect(w * 0.82f, h * 0.5f, w * 0.9f, h * 0.55f, IronC);
                    break;
                case 3:
                    MatRect(r, Mat(def, DecoMaterial.Cobble), 0, 0, w, h * 0.82f, seed);
                    MatRect(r, DecoMaterial.Slate, 0, h * 0.8f, w, h, seed + 1);
                    break;
                default:
                    for (float x = 6; x < w; x += 18) r.FillRect(x, 0, x + 4, h, WoodD);
                    for (int i = 0; i < 6; i++)
                    {
                        float y = h * (0.1f + i * 0.15f);
                        for (float x = 0; x < w; x += 18) r.Line(x, y + (i % 2 == 0 ? -3 : 3), x + 18, y + (i % 2 == 0 ? 3 : -3), 2.5f, i % 2 == 0 ? WoodC : D(WoodC, 0.15f));
                    }
                    break;
            }
        }

        static void DrawSiege(Raster r, ObjectDefinition def, int seed)
        {
            int w = r.width, h = r.height;
            float cx = w / 2f;
            var p = def.primaryColor;
            var s = def.secondaryColor;
            switch (def.variant)   // 0 ballista, 1 catapult, 2 siege ladder, 3 battering ram, 4 tent, 5 pavilion, 6 siege tower, 7 mantlet, 8 supply cart
            {
                case 0:
                    r.Ring(w * 0.3f, h * 0.15f, h * 0.15f, h * 0.09f, IronC); r.Ring(w * 0.7f, h * 0.15f, h * 0.15f, h * 0.09f, IronC);
                    MatRect(r, DecoMaterial.Oak, w * 0.25f, h * 0.22f, w * 0.75f, h * 0.35f, seed);
                    r.Line(w * 0.1f, h * 0.35f, w * 0.9f, h * 0.65f, 6, WoodC);
                    r.Line(w * 0.15f, h * 0.7f, w * 0.15f, h * 0.3f, 4, WoodD);
                    r.FillRect(w * 0.05f, h * 0.45f, w * 0.95f, h * 0.5f, WoodD);
                    r.Line(w * 0.05f, h * 0.5f, w * 0.5f, h * 0.42f, 1.2f, Hex("e8dcc0")); r.Line(w * 0.95f, h * 0.5f, w * 0.5f, h * 0.42f, 1.2f, Hex("e8dcc0"));
                    r.Line(w * 0.35f, h * 0.44f, w * 0.95f, h * 0.68f, 2.5f, WoodD);
                    r.FillTriangle(V(w * 0.9f, h * 0.62f), V(w * 0.98f, h * 0.72f), V(w * 0.92f, h * 0.74f), Hex("b8bcc8"));
                    break;
                case 1:
                    r.Ring(w * 0.2f, h * 0.15f, h * 0.15f, h * 0.09f, IronC); r.Ring(w * 0.75f, h * 0.15f, h * 0.15f, h * 0.09f, IronC);
                    MatRect(r, DecoMaterial.Oak, w * 0.1f, h * 0.22f, w * 0.85f, h * 0.32f, seed);
                    r.FillPolygon(new[] { V(w * 0.35f, h * 0.32f), V(w * 0.45f, h * 0.32f), V(w * 0.55f, h * 0.65f), V(w * 0.5f, h * 0.65f) }, WoodD);
                    r.FillPolygon(new[] { V(w * 0.6f, h * 0.32f), V(w * 0.7f, h * 0.32f), V(w * 0.55f, h * 0.65f), V(w * 0.5f, h * 0.65f) }, WoodD);
                    r.FillRect(w * 0.3f, h * 0.6f, w * 0.75f, h * 0.65f, WoodD);
                    r.Line(w * 0.6f, h * 0.35f, w * 0.15f, h * 0.95f, 5, WoodC);
                    r.FillEllipse(w * 0.15f, h * 0.95f, w * 0.1f, h * 0.05f, WoodD);
                    r.FillCircle(w * 0.15f, h * 0.9f, w * 0.08f, Hex("6c6a68"));
                    break;
                case 2:
                    r.FillRect(w * 0.25f, 0, w * 0.33f, h, WoodC); r.FillRect(w * 0.67f, 0, w * 0.75f, h, WoodC);
                    for (int i = 0; i < 8; i++) r.FillRect(w * 0.33f, h * (0.06f + i * 0.12f), w * 0.67f, h * (0.06f + i * 0.12f) + 4, WoodD);
                    r.FillRect(w * 0.2f, h * 0.9f, w * 0.3f, h * 0.98f, IronC); r.FillRect(w * 0.7f, h * 0.9f, w * 0.8f, h * 0.98f, IronC);
                    break;
                case 3:
                    r.FillRect(w * 0.1f, 0, w * 0.18f, h * 0.7f, WoodD); r.FillRect(w * 0.82f, 0, w * 0.9f, h * 0.7f, WoodD);
                    r.FillPolygon(new[] { V(w * 0.02f, h * 0.65f), V(w * 0.98f, h * 0.65f), V(w * 0.8f, h * 0.95f), V(w * 0.2f, h * 0.95f) }, BaseColor(DecoMaterial.Thatch));
                    for (float x = w * 0.05f; x < w * 0.95f; x += 8) r.Line(x, h * 0.65f, x + 4, h * 0.93f, 1, A(WoodD, 0.4f));
                    r.Line(w * 0.05f, h * 0.35f, w * 0.95f, h * 0.35f, 8, WoodC);
                    r.FillRect(w * 0.88f, h * 0.27f, w, h * 0.43f, IronC);
                    r.FillRect(w * 0.3f, h * 0.3f, w * 0.34f, h * 0.65f, Hex("c9b27a")); r.FillRect(w * 0.66f, h * 0.3f, w * 0.7f, h * 0.65f, Hex("c9b27a"));
                    break;
                case 4:
                    r.FillTriangle(V(0, 0), V(w, 0), V(cx, h * 0.92f), p);
                    r.FillTriangle(V(cx - w * 0.14f, 0), V(cx + w * 0.14f, 0), V(cx, h * 0.6f), D(p, 0.45f));
                    for (int i = 0; i < 3; i++) r.Line(cx, h * 0.92f, w * (0.15f + i * 0.35f), 0, 1.5f, A(s, 0.7f));
                    r.FillRect(cx - 1.5f, h * 0.9f, cx + 1.5f, h, WoodD);
                    r.FillTriangle(V(cx, h * 0.98f), V(cx, h * 0.86f), V(cx + w * 0.15f, h * 0.92f), s);
                    break;
                case 5:
                    r.FillRect(w * 0.1f, 0, w * 0.15f, h * 0.55f, WoodD); r.FillRect(w * 0.85f, 0, w * 0.9f, h * 0.55f, WoodD);
                    r.FillRect(w * 0.12f, h * 0.05f, w * 0.88f, h * 0.55f, p);
                    for (int i = 0; i < 5; i++) r.FillRect(w * (0.12f + i * 0.152f), h * 0.05f, w * (0.12f + i * 0.152f) + w * 0.076f, h * 0.55f, i % 2 == 0 ? p : s);
                    r.FillPolygon(new[] { V(0, h * 0.5f), V(w, h * 0.5f), V(cx, h * 0.95f) }, p);
                    r.FillPolygon(new[] { V(w * 0.1f, h * 0.5f), V(cx, h * 0.85f), V(cx, h * 0.5f) }, s);
                    for (float x = 0; x < w; x += w * 0.1f) r.FillTriangle(V(x, h * 0.5f), V(x + w * 0.1f, h * 0.5f), V(x + w * 0.05f, h * 0.42f), s);
                    r.FillRect(cx - 1.5f, h * 0.9f, cx + 1.5f, h, WoodD);
                    r.FillTriangle(V(cx, h * 0.99f), V(cx, h * 0.9f), V(cx + w * 0.12f, h * 0.95f), GoldC);
                    break;
                case 6:
                    r.Ring(w * 0.2f, h * 0.05f, h * 0.05f, h * 0.03f, IronC); r.Ring(w * 0.8f, h * 0.05f, h * 0.05f, h * 0.03f, IronC);
                    MatRect(r, DecoMaterial.DarkWood, w * 0.15f, h * 0.08f, w * 0.85f, h * 0.9f, seed);
                    r.FillRect(w * 0.15f, h * 0.35f, w * 0.85f, h * 0.38f, WoodC); r.FillRect(w * 0.15f, h * 0.62f, w * 0.85f, h * 0.65f, WoodC);
                    for (int i = 0; i < 3; i++) r.FillRect(w * (0.25f + i * 0.2f), h * 0.72f, w * (0.25f + i * 0.2f) + w * 0.1f, h * 0.84f, Hex("14101c"));
                    r.FillRect(w * 0.1f, h * 0.88f, w * 0.9f, h * 0.95f, WoodD);
                    for (int i = 0; i < 5; i++) r.FillRect(w * (0.12f + i * 0.17f), h * 0.95f, w * (0.12f + i * 0.17f) + w * 0.08f, h, WoodD);
                    r.FillPolygon(new[] { V(w * 0.85f, h * 0.85f), V(w, h * 0.7f), V(w, h * 0.74f), V(w * 0.85f, h * 0.89f) }, WoodC);
                    break;
                case 7:
                    r.FillRect(w * 0.15f, 0, w * 0.22f, h * 0.2f, WoodD); r.FillRect(w * 0.78f, 0, w * 0.85f, h * 0.2f, WoodD);
                    r.Ring(w * 0.3f, h * 0.08f, h * 0.08f, h * 0.04f, IronC); r.Ring(w * 0.7f, h * 0.08f, h * 0.08f, h * 0.04f, IronC);
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.15f, w * 0.95f, h * 0.9f, seed);
                    r.FillRect(w * 0.05f, h * 0.9f, w * 0.95f, h * 0.95f, IronC);
                    r.FillRect(w * 0.4f, h * 0.55f, w * 0.6f, h * 0.7f, Hex("14101c"));
                    for (int i = 0; i < 4; i++) r.FillRect(w * (0.15f + i * 0.22f), h * 0.3f, w * (0.15f + i * 0.22f) + 3, h * 0.85f, IronC);
                    r.Line(w * 0.3f, h * 0.5f, w * 0.34f, h * 0.7f, 2, Hex("c9b27a")); r.FillTriangle(V(w * 0.32f, h * 0.7f), V(w * 0.36f, h * 0.68f), V(w * 0.35f, h * 0.76f), Hex("b8302c"));
                    break;
                default:
                    r.Ring(w * 0.25f, h * 0.2f, h * 0.2f, h * 0.13f, IronC); r.Ring(w * 0.75f, h * 0.2f, h * 0.2f, h * 0.13f, IronC);
                    MatRect(r, DecoMaterial.Oak, w * 0.05f, h * 0.3f, w * 0.95f, h * 0.6f, seed);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = w * (0.2f + i * 0.28f);
                        r.FillRoundRect(x - w * 0.12f, h * 0.6f, x + w * 0.12f, h * 0.95f, 6, WoodC);
                        r.FillRect(x - w * 0.12f, h * 0.7f, x + w * 0.12f, h * 0.74f, IronC);
                    }
                    r.FillRect(w * 0.9f, h * 0.35f, w, h * 0.42f, WoodD);
                    break;
            }
        }
    }
}
