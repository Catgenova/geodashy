using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Draws procedural stand-in art for every catalog object so the editor is fully usable
    /// before real sprites exist. Replace any of these by dropping a PNG in Resources/Sprites/{id}.png.
    /// </summary>
    public static class PlaceholderSpriteFactory
    {
        public const float PPU = 64f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite ForDefinition(ObjectDefinition def)
        {
            if (cache.TryGetValue("def:" + def.id, out var s) && s != null) return s;
            int w = Mathf.Max(4, Mathf.RoundToInt(def.width * PPU));
            int h = Mathf.Max(4, Mathf.RoundToInt(def.height * PPU));
            var r = new Raster(w, h);
            Draw(r, def);
            s = r.ToSprite(PPU);
            s.name = "ph_" + def.id;
            cache["def:" + def.id] = s;
            return s;
        }

        public static Sprite ForText(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) text = " ";
            string key = "text:" + text + ":" + ColorUtility.ToHtmlStringRGBA(color);
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var m = BitmapFont.Measure(text);
            int scale = Mathf.RoundToInt(PPU / BitmapFont.LineHeight); // one line = 1 unit tall
            var r = new Raster(m.x * scale + scale, m.y * scale + scale);
            BitmapFont.Draw(r, text, scale / 2, r.height - scale / 2, scale, color);
            s = r.ToSprite(PPU, null, null, FilterMode.Point);
            s.name = "text";
            if (cache.Count > 2000) cache.Clear();
            cache[key] = s;
            return s;
        }

        /// <summary>Size in world units of a text object at scale 1.</summary>
        public static Vector2 TextSize(string text)
        {
            if (string.IsNullOrEmpty(text)) text = " ";
            var m = BitmapFont.Measure(text);
            int scale = Mathf.RoundToInt(PPU / BitmapFont.LineHeight);
            return new Vector2((m.x * scale + scale) / PPU, (m.y * scale + scale) / PPU);
        }

        public static Sprite WhiteSquare()
        {
            if (cache.TryGetValue("white", out var s) && s != null) return s;
            var r = new Raster(4, 4);
            r.Clear(Color.white);
            s = r.ToSprite(4f);
            cache["white"] = s;
            return s;
        }

        public static Sprite Circle()
        {
            if (cache.TryGetValue("circle", out var s) && s != null) return s;
            var r = new Raster(64, 64);
            r.FillCircle(32, 32, 31, Color.white);
            s = r.ToSprite(64f);
            cache["circle"] = s;
            return s;
        }

        /// <summary>9-sliced outline used for selection boxes and camera guides. Border thickness is 3px of 16.</summary>
        public static Sprite Outline()
        {
            if (cache.TryGetValue("outline", out var s) && s != null) return s;
            var r = new Raster(16, 16);
            r.Border(3, Color.white);
            s = r.ToSprite(64f, null, new Vector4(5, 5, 5, 5), FilterMode.Point);
            cache["outline"] = s;
            return s;
        }

        /// <summary>A single grid cell texture (lines on the left and bottom) tiled over the world.</summary>
        public static Sprite GridCell(int px, float pixelsPerUnit, Color line, Color major, int majorEvery)
        {
            string key = "grid:" + px + ":" + majorEvery + ":" + pixelsPerUnit;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            int size = px * majorEvery;
            var r = new Raster(size, size);
            r.Clear(new Color(0, 0, 0, 0));
            for (int i = 0; i < majorEvery; i++)
            {
                r.FillRect(i * px, 0, i * px + 1, size, line);
                r.FillRect(0, i * px, size, i * px + 1, line);
            }
            r.FillRect(0, 0, 2, size, major);
            r.FillRect(0, 0, size, 2, major);
            s = r.ToSprite(pixelsPerUnit, new Vector2(0, 0), null, FilterMode.Point);
            cache[key] = s;
            return s;
        }

        public static Sprite Ground(GroundTheme g)
        {
            string key = "ground:" + g.id;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            int w = 256, h = 256; // 4 x 4 blocks
            var r = new Raster(w, h) { wrapX = true };
            r.Clear(g.body);
            var dark = Raster.Darken(g.body, 0.25f);
            var light = Raster.Lighten(g.body, 0.12f);
            switch (g.style)
            {
                case "bricks":
                    for (int row = 0; row < 8; row++)
                    {
                        int y0 = row * 32;
                        r.FillRect(0, y0, w, y0 + 2, dark);
                        int off = (row % 2) * 32;
                        for (int col = 0; col < 4; col++) r.FillRect(off + col * 64, y0, off + col * 64 + 2, y0 + 32, dark);
                        r.FillRect(0, y0 + 30, w, y0 + 31, light);
                    }
                    // battlement notches cut into the top course
                    for (int x = 8; x < w; x += 32) r.FillRect(x, h - 30, x + 16, h - 14, dark);
                    break;
                case "planks":
                    for (int col = 0; col < 8; col++)
                    {
                        int x0 = col * 32;
                        r.FillRect(x0, 0, x0 + 2, h, dark);
                        r.FillRect(x0 + 30, 0, x0 + 31, h, light);
                        r.FillCircle(x0 + 16, (col % 2) * 128 + 40, 2, dark);
                    }
                    break;
                case "grass":
                    for (int i = 0; i < 40; i++)
                    {
                        int x = (i * 37) % w;
                        int y = (i * 53) % (h - 20);
                        r.FillCircle(x, y, 3, dark);
                    }
                    // roots reaching down from the turf
                    for (int i = 0; i < 6; i++)
                    {
                        float x = 20 + i * 42;
                        r.Line(x, h - 14, x - 10, h - 60, 3, dark);
                        r.Line(x, h - 14, x + 12, h - 48, 2, dark);
                    }
                    for (int x = 4; x < w; x += 9) r.FillTriangle(new Vector2(x - 2, h - 14), new Vector2(x + 2, h - 14), new Vector2(x + 1, h - 4), Raster.Lighten(g.top, 0.15f));
                    break;
                case "sand":
                    for (int i = 0; i < 30; i++) r.FillEllipse((i * 41) % w, (i * 67) % (h - 24), 5, 3, light);
                    for (int y = 40; y < h - 20; y += 48)
                    for (int x = 0; x < w; x += 4)
                        r.FillRect(x, y + Mathf.RoundToInt(Mathf.Sin(x * 0.2f) * 4f), x + 2, y + Mathf.RoundToInt(Mathf.Sin(x * 0.2f) * 4f) + 1, dark);
                    break;
                case "ice":
                    for (int i = 0; i < 7; i++)
                    {
                        float x = 12 + i * 36;
                        r.Line(x, h - 14, x + 18, h - 70, 1.5f, Raster.Lighten(g.body, 0.5f));
                        r.Line(x + 18, h - 70, x + 4, h - 120, 1.5f, Raster.Lighten(g.body, 0.5f));
                    }
                    r.FillRect(0, h - 40, w, h - 38, Raster.Lighten(g.body, 0.35f));
                    break;
                case "lava":
                    for (int row = 0; row < 8; row++)
                    {
                        int y0 = row * 32;
                        r.FillRect(0, y0, w, y0 + 2, dark);
                        int off = (row % 2) * 32;
                        for (int col = 0; col < 4; col++) r.FillRect(off + col * 64, y0, off + col * 64 + 2, y0 + 32, dark);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        float x = 10 + i * 50;
                        r.Line(x, h - 20, x + 14, h - 90, 3, g.line);
                        r.Line(x + 14, h - 90, x - 4, h - 150, 2, g.line);
                    }
                    break;
                case "crystal":
                    for (int i = 0; i < 9; i++)
                    {
                        float x = i * 30;
                        r.Line(x, 0, x + 60, h, 2, light);
                        r.Line(x + 30, 0, x - 30, h, 1.5f, dark);
                    }
                    break;
                case "swamp":
                    for (int i = 0; i < 18; i++) r.Ring((i * 47) % w, (i * 71) % (h - 30), 5, 3, light);
                    for (int x = 6; x < w; x += 22) r.FillRect(x, h - 14, x + 2, h - 44, dark);
                    break;
                case "cloud":
                    for (int i = 0; i < 16; i++) r.FillEllipse((i * 53) % w, h - 30 - (i * 29) % 100, 22, 12, Raster.Lighten(g.body, 0.25f));
                    break;
                default:
                    for (int i = 0; i < 24; i++)
                    {
                        int x = (i * 41) % w;
                        int y = (i * 67) % (h - 20);
                        r.FillEllipse(x, y, 6, 3, light);
                    }
                    break;
            }
            // top surface band
            r.FillRect(0, h - 12, w, h, g.top);
            r.FillRect(0, h - 14, w, h - 12, dark);
            s = r.ToSprite(64f, new Vector2(0.5f, 1f));
            cache[key] = s;
            return s;
        }

        public static Sprite Gradient(Color bottom, Color top)
        {
            string key = "grad:" + ColorUtility.ToHtmlStringRGBA(bottom) + ColorUtility.ToHtmlStringRGBA(top);
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var r = new Raster(64, 64);
            r.GradientV(bottom, top);
            s = r.ToSprite(64f);
            s.name = "gradient";
            cache[key] = s;
            return s;
        }

        public static Sprite ForMount(MountDefinition m)
        {
            string key = "mount:" + m.id;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var r = new Raster(96, 96);
            var body = m.Color;
            var accent = m.Accent;
            var dark = Raster.Darken(body, 0.35f);
            var rider = new Color(0.35f, 0.75f, 0.35f);
            switch (m.id)
            {
                case "dragon":
                    r.FillTriangle(new Vector2(30, 60), new Vector2(60, 60), new Vector2(45, 92), Raster.Darken(body, 0.15f)); // wing
                    r.FillEllipse(48, 42, 40, 18, body);
                    r.FillEllipse(84, 50, 12, 10, body); // head
                    r.FillTriangle(new Vector2(6, 42), new Vector2(20, 34), new Vector2(20, 50), body); // tail
                    r.FillCircle(88, 53, 3, Color.black);
                    r.FillTriangle(new Vector2(92, 44), new Vector2(96, 48), new Vector2(88, 46), accent);
                    r.FillCircle(44, 66, 9, rider);
                    r.FillRect(38, 52, 50, 66, rider);
                    break;
                case "griffin":
                    r.FillTriangle(new Vector2(20, 50), new Vector2(64, 50), new Vector2(38, 94), Raster.Darken(body, 0.15f));
                    r.FillEllipse(48, 40, 34, 20, body);
                    r.FillCircle(80, 52, 12, accent);
                    r.FillTriangle(new Vector2(88, 52), new Vector2(96, 48), new Vector2(88, 44), Color.yellow);
                    r.FillCircle(84, 56, 3, Color.black);
                    r.FillCircle(44, 66, 9, rider);
                    r.FillRect(38, 52, 50, 66, rider);
                    break;
                case "boar":
                    r.FillCircle(48, 48, 40, body);
                    r.Ring(48, 48, 40, 34, dark);
                    r.FillCircle(72, 44, 8, accent);
                    r.FillCircle(28, 48, 3, Color.black);
                    r.FillCircle(48, 60, 9, rider);
                    break;
                case "wisp":
                    r.FillCircle(48, 48, 40, Raster.Alpha(body, 0.35f));
                    r.FillCircle(48, 48, 28, body);
                    r.FillCircle(48, 48, 14, accent);
                    r.FillTriangle(new Vector2(48, 30), new Vector2(0, 48), new Vector2(48, 66), Raster.Alpha(body, 0.5f));
                    break;
                case "cart":
                    r.FillRect(10, 30, 86, 70, body);
                    r.RectOutline(10, 30, 86, 70, 4, dark);
                    r.FillCircle(28, 26, 14, accent);
                    r.FillCircle(68, 26, 14, accent);
                    r.FillCircle(28, 26, 5, dark);
                    r.FillCircle(68, 26, 5, dark);
                    r.FillCircle(48, 82, 9, rider);
                    r.FillRect(42, 68, 54, 82, rider);
                    break;
                case "shadowcat":
                    r.FillEllipse(48, 36, 40, 20, body);
                    r.FillCircle(80, 50, 14, body);
                    r.FillTriangle(new Vector2(70, 60), new Vector2(76, 76), new Vector2(82, 60), body);
                    r.FillTriangle(new Vector2(84, 60), new Vector2(90, 76), new Vector2(94, 60), body);
                    r.FillCircle(84, 52, 3, accent);
                    r.FillCircle(76, 52, 3, accent);
                    r.Line(10, 40, 4, 70, 5, body);
                    r.FillCircle(44, 62, 9, rider);
                    r.FillRect(38, 50, 50, 62, rider);
                    break;
                default: // horse, side view facing right
                {
                    var mane = Raster.Darken(body, 0.45f);
                    var hoof = Raster.Darken(body, 0.6f);
                    // tail: curved dark strokes off the rump
                    r.Line(18, 54, 8, 34, 6, mane);
                    r.Line(18, 54, 4, 46, 5, mane);
                    r.Line(16, 52, 10, 26, 4, mane);
                    // legs: two pairs, slight stride, hooves at the bottom
                    void Leg(float topX, float bottomX, Color c)
                    {
                        r.FillPolygon(new[] { new Vector2(topX - 3.5f, 44), new Vector2(topX + 3.5f, 44), new Vector2(bottomX + 3f, 14), new Vector2(bottomX - 3f, 14) }, c);
                        r.FillRect(bottomX - 3.5f, 10, bottomX + 3.5f, 15, hoof);
                    }
                    Leg(30, 24, Raster.Darken(body, 0.18f)); // far rear
                    Leg(60, 66, Raster.Darken(body, 0.18f)); // far front
                    Leg(34, 36, body);                      // near rear
                    Leg(64, 60, body);                      // near front
                    // body
                    r.FillEllipse(46, 50, 28, 14, body);
                    r.FillEllipse(24, 52, 10, 11, body); // rump
                    r.FillEllipse(66, 52, 11, 12, body); // chest
                    // neck rising forward to the head
                    r.FillPolygon(new[] { new Vector2(60, 58), new Vector2(74, 52), new Vector2(86, 74), new Vector2(72, 78) }, body);
                    // mane along the top of the neck
                    r.FillPolygon(new[] { new Vector2(56, 62), new Vector2(72, 80), new Vector2(80, 86), new Vector2(66, 64) }, mane);
                    r.FillPolygon(new[] { new Vector2(60, 64), new Vector2(76, 82), new Vector2(72, 88), new Vector2(58, 68) }, mane);
                    // head + muzzle, ears, eye
                    r.FillEllipse(82, 78, 11, 7, body);
                    r.FillEllipse(91, 76, 5, 4, Raster.Lighten(body, 0.2f));
                    r.FillTriangle(new Vector2(76, 82), new Vector2(80, 82), new Vector2(77, 90), body);
                    r.FillTriangle(new Vector2(81, 83), new Vector2(85, 82), new Vector2(84, 90), body);
                    r.FillCircle(85, 80, 1.6f, Color.black);
                    r.FillCircle(94, 74, 1, hoof);
                    // bridle + reins
                    r.Line(88, 74, 82, 84, 1.5f, accent);
                    r.Line(84, 74, 52, 62, 1, accent);
                    // saddle and rider
                    r.FillEllipse(44, 60, 11, 4, accent);
                    r.FillRect(38, 56, 50, 72, rider);
                    r.FillRect(36, 60, 40, 70, rider); // arm
                    r.FillCircle(44, 78, 7, rider);
                    r.FillRect(36, 78, 52, 83, Raster.Darken(rider, 0.35f)); // helmet brim
                    r.FillRect(46, 50, 50, 58, rider); // leg
                    break;
                }
            }
            s = r.ToSprite(96f);
            s.name = "mount_" + m.id;
            cache[key] = s;
            return s;
        }

        public static void ClearCache() => cache.Clear();

        // ---------------------------------------------------------------------

        static void Draw(Raster r, ObjectDefinition def)
        {
            var p = def.primaryColor;
            var s = def.secondaryColor;
            int w = r.width, h = r.height;
            float cx = w / 2f, cy = h / 2f;
            var dark = Raster.Darken(p, 0.3f);
            var light = Raster.Lighten(p, 0.25f);

            switch (def.shape)
            {
                case PlaceholderShape.Block:
                case PlaceholderShape.HalfBlock:
                    r.Clear(p);
                    r.Border(3, s);
                    r.FillRect(3, h - 6, w - 3, h - 3, light);
                    r.FillRect(3, 3, 6, h - 3, light);
                    break;

                case PlaceholderShape.BlockOutline:
                    if (p.a < 0.01f)
                    {
                        // invisible block: editor-only magenta dashes
                        r.Clear(new Color(0, 0, 0, 0));
                        var m = Raster.Alpha(s, 0.55f);
                        for (int i = 0; i < w; i += 12) r.FillRect(i, 0, Mathf.Min(w, i + 6), 3, m);
                        for (int i = 0; i < w; i += 12) r.FillRect(i, h - 3, Mathf.Min(w, i + 6), h, m);
                        for (int i = 0; i < h; i += 12) r.FillRect(0, i, 3, Mathf.Min(h, i + 6), m);
                        for (int i = 0; i < h; i += 12) r.FillRect(w - 3, i, w, Mathf.Min(h, i + 6), m);
                    }
                    else
                    {
                        r.Clear(Raster.Alpha(s, 0.85f));
                        r.Border(5, p);
                        r.RectOutline(7, 7, w - 7, h - 7, 1, Raster.Alpha(p, 0.4f));
                    }
                    break;

                case PlaceholderShape.Bricks:
                {
                    r.Clear(p);
                    int rows = Mathf.Max(1, Mathf.RoundToInt(h / 32f));
                    int cols = Mathf.Max(1, Mathf.RoundToInt(w / 64f));
                    int rh = h / rows;
                    for (int row = 0; row < rows; row++)
                    {
                        int y0 = row * rh;
                        r.FillRect(0, y0, w, y0 + 3, s);
                        int off = (row % 2) * (rh);
                        for (int col = -1; col <= cols; col++)
                        {
                            int x0 = off + col * (w / cols);
                            r.FillRect(x0, y0, x0 + 3, y0 + rh, s);
                        }
                        r.FillRect(0, y0 + rh - 2, w, y0 + rh, Raster.Alpha(light, 0.6f));
                    }
                    r.Border(3, s);
                    break;
                }

                case PlaceholderShape.Planks:
                {
                    r.Clear(p);
                    int rows = Mathf.Max(1, Mathf.RoundToInt(h / 16f));
                    int rh = h / rows;
                    for (int row = 0; row < rows; row++)
                    {
                        int y0 = row * rh;
                        r.FillRect(0, y0, w, y0 + 2, s);
                        r.FillRect(0, y0 + rh - 2, w, y0 + rh, Raster.Alpha(light, 0.5f));
                        int nx = (row % 2 == 0) ? 8 : w - 12;
                        r.FillCircle(nx, y0 + rh / 2f, 2, s);
                        r.FillCircle(nx + (row % 2 == 0 ? w / 2 : -w / 2), y0 + rh / 2f, 2, s);
                    }
                    r.Border(3, s);
                    break;
                }

                case PlaceholderShape.Slope45:
                case PlaceholderShape.Slope22:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillTriangle(new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, h), p);
                    r.Line(0, 0, w, h, 5, s);
                    r.FillRect(0, 0, w, 3, s);
                    r.FillRect(w - 3, 0, w, h, s);
                    break;

                case PlaceholderShape.Spike:
                case PlaceholderShape.SpikeSmall:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillTriangle(new Vector2(0, 0), new Vector2(w, 0), new Vector2(cx, h), p);
                    r.FillTriangle(new Vector2(cx, 0), new Vector2(w, 0), new Vector2(cx, h), Raster.Darken(p, 0.2f));
                    r.Line(0, 0, cx, h, 3, s);
                    r.Line(w, 0, cx, h, 3, s);
                    r.FillRect(0, 0, w, 3, s);
                    break;

                case PlaceholderShape.SpikeWide:
                {
                    r.Clear(new Color(0, 0, 0, 0));
                    int n = Mathf.Max(1, Mathf.RoundToInt(def.width));
                    float sw = w / (float)n;
                    for (int i = 0; i < n; i++)
                    {
                        float x0 = i * sw;
                        r.FillTriangle(new Vector2(x0, 0), new Vector2(x0 + sw, 0), new Vector2(x0 + sw / 2f, h), p);
                        r.FillTriangle(new Vector2(x0 + sw / 2f, 0), new Vector2(x0 + sw, 0), new Vector2(x0 + sw / 2f, h), Raster.Darken(p, 0.2f));
                        r.Line(x0, 0, x0 + sw / 2f, h, 3, s);
                        r.Line(x0 + sw, 0, x0 + sw / 2f, h, 3, s);
                    }
                    r.FillRect(0, 0, w, 3, s);
                    break;
                }

                case PlaceholderShape.Lance:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(cx - 6, 0, cx + 6, h * 0.55f, s);
                    r.FillTriangle(new Vector2(cx - 14, h * 0.5f), new Vector2(cx + 14, h * 0.5f), new Vector2(cx, h), p);
                    r.Line(cx - 14, h * 0.5f, cx, h, 2, s);
                    r.Line(cx + 14, h * 0.5f, cx, h, 2, s);
                    r.FillRect(cx - 18, h * 0.1f, cx + 18, h * 0.16f, dark);
                    break;

                case PlaceholderShape.Thorns:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillCircle(cx, cy, w * 0.32f, p);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * Mathf.PI * 2f / 10f;
                        var b0 = new Vector2(cx + Mathf.Cos(a + 0.25f) * w * 0.3f, cy + Mathf.Sin(a + 0.25f) * h * 0.3f);
                        var b1 = new Vector2(cx + Mathf.Cos(a - 0.25f) * w * 0.3f, cy + Mathf.Sin(a - 0.25f) * h * 0.3f);
                        var tip = new Vector2(cx + Mathf.Cos(a) * w * 0.49f, cy + Mathf.Sin(a) * h * 0.49f);
                        r.FillTriangle(b0, b1, tip, s);
                    }
                    r.FillCircle(cx - 6, cy + 6, 5, Raster.Lighten(p, 0.3f));
                    break;

                case PlaceholderShape.Fire:
                {
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(0, 0, w, h * 0.15f, new Color(0.25f, 0.2f, 0.2f));
                    int n = Mathf.Max(1, Mathf.RoundToInt(def.width));
                    float fw = w / (float)n;
                    for (int i = 0; i < n; i++)
                    {
                        float fx = i * fw + fw / 2f;
                        r.FillTriangle(new Vector2(fx - fw * 0.42f, h * 0.12f), new Vector2(fx + fw * 0.42f, h * 0.12f), new Vector2(fx + fw * 0.1f, h * 0.95f), p);
                        r.FillTriangle(new Vector2(fx - fw * 0.25f, h * 0.12f), new Vector2(fx + fw * 0.25f, h * 0.12f), new Vector2(fx - fw * 0.05f, h * 0.65f), s);
                        r.FillCircle(fx, h * 0.3f, fw * 0.18f, Raster.Lighten(s, 0.5f));
                    }
                    break;
                }

                case PlaceholderShape.Bog:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRoundRect(0, 0, w, h * 0.7f, h * 0.3f, p);
                    r.FillRoundRect(4, 4, w - 4, h * 0.55f, h * 0.2f, s);
                    for (int i = 0; i < 5; i++) r.Ring(w * (i + 0.5f) / 5f, h * (0.5f + 0.35f * ((i % 2 == 0) ? 1 : 0.4f)), h * 0.12f, h * 0.07f, Raster.Lighten(p, 0.3f));
                    break;

                case PlaceholderShape.Saw:
                {
                    r.Clear(new Color(0, 0, 0, 0));
                    float rad = Mathf.Min(w, h) * 0.5f;
                    int teeth = Mathf.Clamp(Mathf.RoundToInt(def.width * 10f), 8, 36);
                    for (int i = 0; i < teeth; i++)
                    {
                        float a = i * Mathf.PI * 2f / teeth;
                        float a2 = a + Mathf.PI * 2f / teeth;
                        var b0 = new Vector2(cx + Mathf.Cos(a) * rad * 0.78f, cy + Mathf.Sin(a) * rad * 0.78f);
                        var b1 = new Vector2(cx + Mathf.Cos(a2) * rad * 0.78f, cy + Mathf.Sin(a2) * rad * 0.78f);
                        var tip = new Vector2(cx + Mathf.Cos(a + 0.6f / teeth * 6f) * rad, cy + Mathf.Sin(a + 0.6f / teeth * 6f) * rad);
                        r.FillTriangle(b0, b1, tip, s);
                    }
                    r.FillCircle(cx, cy, rad * 0.8f, p);
                    r.Ring(cx, cy, rad * 0.8f, rad * 0.72f, s);
                    r.FillCircle(cx, cy, rad * 0.2f, s);
                    r.FillCircle(cx, cy, rad * 0.1f, dark);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI / 2f + 0.4f;
                        r.FillCircle(cx + Mathf.Cos(a) * rad * 0.45f, cy + Mathf.Sin(a) * rad * 0.45f, rad * 0.08f, s);
                    }
                    break;
                }

                case PlaceholderShape.Mace:
                {
                    r.Clear(new Color(0, 0, 0, 0));
                    float rad = Mathf.Min(w, h) * 0.5f;
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI * 2f / 12f;
                        var b0 = new Vector2(cx + Mathf.Cos(a + 0.2f) * rad * 0.62f, cy + Mathf.Sin(a + 0.2f) * rad * 0.62f);
                        var b1 = new Vector2(cx + Mathf.Cos(a - 0.2f) * rad * 0.62f, cy + Mathf.Sin(a - 0.2f) * rad * 0.62f);
                        var tip = new Vector2(cx + Mathf.Cos(a) * rad, cy + Mathf.Sin(a) * rad);
                        r.FillTriangle(b0, b1, tip, s);
                    }
                    r.FillCircle(cx, cy, rad * 0.65f, p);
                    r.Ring(cx, cy, rad * 0.65f, rad * 0.58f, s);
                    r.FillCircle(cx - rad * 0.2f, cy + rad * 0.2f, rad * 0.12f, Raster.Lighten(p, 0.3f));
                    break;
                }

                case PlaceholderShape.Lightning:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(cx - w * 0.3f, 0, cx + w * 0.3f, h, Raster.Alpha(s, 0.25f));
                    r.Line(cx + 8, h, cx - 10, h * 0.62f, 6, p);
                    r.Line(cx - 10, h * 0.62f, cx + 10, h * 0.42f, 6, p);
                    r.Line(cx + 10, h * 0.42f, cx - 8, 0, 6, p);
                    r.Line(cx + 8, h, cx - 10, h * 0.62f, 2, Color.white);
                    r.Line(cx - 10, h * 0.62f, cx + 10, h * 0.42f, 2, Color.white);
                    r.Line(cx + 10, h * 0.42f, cx - 8, 0, 2, Color.white);
                    break;

                case PlaceholderShape.Orb:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillCircle(cx, cy, w * 0.48f, Raster.Alpha(p, 0.25f));
                    r.Ring(cx, cy, w * 0.42f, w * 0.34f, p);
                    r.FillCircle(cx, cy, w * 0.3f, s);
                    r.FillCircle(cx, cy, w * 0.18f, p);
                    r.FillCircle(cx - w * 0.1f, cy + w * 0.12f, w * 0.06f, Color.white);
                    break;

                case PlaceholderShape.Pad:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillEllipse(cx, 0, w * 0.5f, h, p);
                    r.FillEllipse(cx, 0, w * 0.34f, h * 0.55f, s);
                    r.FillRect(0, 0, w, h * 0.2f, dark);
                    break;

                case PlaceholderShape.Portal:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillEllipse(cx, cy, w * 0.5f, h * 0.5f, Raster.Alpha(p, 0.35f));
                    r.EllipseRing(cx, cy, w * 0.5f, h * 0.5f, 7, p);
                    r.EllipseRing(cx, cy, w * 0.36f, h * 0.36f, 3, Raster.Alpha(Color.white, 0.6f));
                    r.FillEllipse(cx, cy, w * 0.2f, h * 0.2f, s);
                    if (def.portalType == PortalType.Mount && !string.IsNullOrEmpty(def.portalMount))
                    {
                        var m = MountCatalog.Get(def.portalMount);
                        var mr = new Raster(w, w);
                        mr.Clear(new Color(0, 0, 0, 0));
                        BitmapFont.DrawCentered(mr, m.name.Substring(0, 1), Color.white, 0.3f);
                        for (int y = 0; y < w; y++)
                        for (int x = 0; x < w; x++)
                            r.Plot(x, Mathf.RoundToInt(cy - w / 2f) + y, mr.pixels[y * w + x]);
                    }
                    else if (def.portalType != PortalType.None && def.portalType != PortalType.Mount)
                    {
                        string code = PortalCode(def.portalType);
                        var mr = new Raster(w, w);
                        mr.Clear(new Color(0, 0, 0, 0));
                        BitmapFont.DrawCentered(mr, code, Color.white, 0.3f);
                        for (int y = 0; y < w; y++)
                        for (int x = 0; x < w; x++)
                            r.Plot(x, Mathf.RoundToInt(cy - w / 2f) + y, mr.pixels[y * w + x]);
                    }
                    break;

                case PlaceholderShape.Rune:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRoundRect(w * 0.1f, 0, w * 0.9f, h, w * 0.3f, s);
                    r.RoundRectOutline(w * 0.1f, 0, w * 0.9f, h, w * 0.3f, 3, p);
                    if (def.portalType == PortalType.Speed)
                    {
                        BitmapFont.DrawCentered(r, MountCatalog.SpeedLabels[(int)def.portalSpeed], p, 0.22f);
                    }
                    else
                    {
                        r.Line(cx, h * 0.2f, cx, h * 0.8f, 4, p);
                        r.Line(cx - w * 0.2f, h * 0.35f, cx + w * 0.2f, h * 0.65f, 4, p);
                        r.Line(cx + w * 0.2f, h * 0.35f, cx - w * 0.2f, h * 0.65f, 4, p);
                    }
                    break;

                case PlaceholderShape.Coin:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillCircle(cx, cy, w * 0.42f, p);
                    r.Ring(cx, cy, w * 0.42f, w * 0.36f, s);
                    r.Ring(cx, cy, w * 0.28f, w * 0.24f, s);
                    r.FillCircle(cx - w * 0.12f, cy + w * 0.12f, w * 0.06f, Color.white);
                    break;

                case PlaceholderShape.Gem:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillPolygon(new[] { new Vector2(cx, h * 0.05f), new Vector2(w * 0.1f, h * 0.6f), new Vector2(w * 0.3f, h * 0.9f), new Vector2(w * 0.7f, h * 0.9f), new Vector2(w * 0.9f, h * 0.6f) }, p);
                    r.FillPolygon(new[] { new Vector2(cx, h * 0.2f), new Vector2(w * 0.3f, h * 0.6f), new Vector2(w * 0.7f, h * 0.6f) }, s);
                    r.FillRect(w * 0.3f, h * 0.75f, w * 0.7f, h * 0.82f, Raster.Alpha(Color.white, 0.5f));
                    break;

                case PlaceholderShape.Key:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.Ring(w * 0.3f, h * 0.65f, w * 0.22f, w * 0.1f, p);
                    r.FillRect(w * 0.45f, h * 0.6f, w * 0.92f, h * 0.7f, p);
                    r.FillRect(w * 0.75f, h * 0.45f, w * 0.82f, h * 0.6f, p);
                    r.FillRect(w * 0.86f, h * 0.48f, w * 0.92f, h * 0.6f, p);
                    r.Ring(w * 0.3f, h * 0.65f, w * 0.22f, w * 0.19f, s);
                    break;

                case PlaceholderShape.Flag:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(w * 0.42f, 0, w * 0.5f, h, s);
                    r.FillTriangle(new Vector2(w * 0.5f, h), new Vector2(w * 0.5f, h * 0.65f), new Vector2(w * 0.98f, h * 0.82f), p);
                    if (def.kind == ObjectKind.Finish)
                    {
                        r.FillRect(w * 0.55f, h * 0.72f, w * 0.7f, h * 0.9f, Color.white);
                    }
                    r.FillCircle(w * 0.46f, h - 4, 6, p);
                    break;

                case PlaceholderShape.Gate:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(0, 0, w * 0.2f, h, s);
                    r.FillRect(w * 0.8f, 0, w, h, s);
                    r.FillEllipse(cx, h * 0.62f, w * 0.5f, h * 0.38f, s);
                    r.FillEllipse(cx, h * 0.55f, w * 0.32f, h * 0.3f, Raster.Alpha(p, 0.55f));
                    r.FillRect(w * 0.18f, 0, w * 0.82f, h * 0.55f, Raster.Alpha(p, 0.55f));
                    for (int i = 0; i < 4; i++)
                    {
                        float x = w * (0.26f + i * 0.16f);
                        r.FillRect(x, 0, x + 4, h * 0.8f, dark);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        float y = h * (0.1f + i * 0.18f);
                        r.FillRect(w * 0.2f, y, w * 0.8f, y + 4, dark);
                    }
                    if (def.kind == ObjectKind.Finish)
                    {
                        r.FillRect(w * 0.2f, h * 0.9f, w * 0.8f, h, p);
                        r.FillRect(w * 0.2f, h * 0.9f, w * 0.8f, h * 0.92f, dark);
                    }
                    break;

                case PlaceholderShape.Torch:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(cx - 4, 0, cx + 4, h * 0.55f, s);
                    r.FillRect(cx - 8, h * 0.45f, cx + 8, h * 0.55f, Raster.Darken(s, 0.3f));
                    r.FillCircle(cx, h * 0.72f, w * 0.34f, Raster.Alpha(p, 0.35f));
                    r.FillTriangle(new Vector2(cx - 10, h * 0.55f), new Vector2(cx + 10, h * 0.55f), new Vector2(cx, h * 0.98f), p);
                    r.FillTriangle(new Vector2(cx - 5, h * 0.58f), new Vector2(cx + 5, h * 0.58f), new Vector2(cx, h * 0.8f), Color.yellow);
                    break;

                case PlaceholderShape.Chain:
                {
                    r.Clear(new Color(0, 0, 0, 0));
                    int links = Mathf.Max(2, Mathf.RoundToInt(def.height * 4f));
                    float lh = h / (float)links;
                    for (int i = 0; i < links; i++)
                    {
                        float y0 = i * lh;
                        if (i % 2 == 0) r.EllipseRing(cx, y0 + lh / 2f, w * 0.3f, lh * 0.55f, 4, p);
                        else r.FillRoundRect(cx - 4, y0 - 2, cx + 4, y0 + lh + 2, 3, s);
                    }
                    break;
                }

                case PlaceholderShape.Skull:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillCircle(cx, h * 0.6f, w * 0.4f, p);
                    r.FillRect(w * 0.3f, h * 0.1f, w * 0.7f, h * 0.45f, p);
                    r.FillCircle(cx - w * 0.15f, h * 0.62f, w * 0.1f, s);
                    r.FillCircle(cx + w * 0.15f, h * 0.62f, w * 0.1f, s);
                    r.FillTriangle(new Vector2(cx - 4, h * 0.42f), new Vector2(cx + 4, h * 0.42f), new Vector2(cx, h * 0.52f), s);
                    for (int i = 0; i < 3; i++) r.FillRect(w * 0.36f + i * w * 0.1f, h * 0.1f, w * 0.36f + i * w * 0.1f + 2, h * 0.3f, s);
                    break;

                case PlaceholderShape.Barrel:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRoundRect(w * 0.08f, 0, w * 0.92f, h, w * 0.15f, p);
                    r.FillRect(w * 0.08f, h * 0.18f, w * 0.92f, h * 0.26f, s);
                    r.FillRect(w * 0.08f, h * 0.74f, w * 0.92f, h * 0.82f, s);
                    for (int i = 1; i < 4; i++) r.FillRect(w * 0.08f + i * w * 0.21f, 0, w * 0.08f + i * w * 0.21f + 2, h, Raster.Alpha(dark, 0.5f));
                    break;

                case PlaceholderShape.Crate:
                    r.Clear(p);
                    r.Border(5, s);
                    r.Line(0, 0, w, h, 6, s);
                    r.Line(0, h, w, 0, 6, s);
                    break;

                case PlaceholderShape.Tree:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(cx - w * 0.06f, 0, cx + w * 0.06f, h * 0.45f, s);
                    if (def.id.Contains("pine"))
                    {
                        r.FillTriangle(new Vector2(w * 0.05f, h * 0.3f), new Vector2(w * 0.95f, h * 0.3f), new Vector2(cx, h * 0.7f), p);
                        r.FillTriangle(new Vector2(w * 0.15f, h * 0.55f), new Vector2(w * 0.85f, h * 0.55f), new Vector2(cx, h * 0.88f), p);
                        r.FillTriangle(new Vector2(w * 0.25f, h * 0.75f), new Vector2(w * 0.75f, h * 0.75f), new Vector2(cx, h), Raster.Lighten(p, 0.1f));
                    }
                    else if (def.id.Contains("dead"))
                    {
                        r.Line(cx, h * 0.4f, w * 0.2f, h * 0.8f, 6, s);
                        r.Line(cx, h * 0.5f, w * 0.8f, h * 0.9f, 6, s);
                        r.Line(cx, h * 0.45f, cx + 4, h * 0.98f, 6, s);
                        r.Line(w * 0.2f, h * 0.8f, w * 0.1f, h * 0.95f, 4, s);
                    }
                    else
                    {
                        r.FillCircle(cx, h * 0.68f, w * 0.34f, p);
                        r.FillCircle(cx - w * 0.22f, h * 0.55f, w * 0.24f, p);
                        r.FillCircle(cx + w * 0.22f, h * 0.55f, w * 0.24f, p);
                        r.FillCircle(cx - w * 0.1f, h * 0.78f, w * 0.12f, Raster.Lighten(p, 0.25f));
                    }
                    break;

                case PlaceholderShape.Bush:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillEllipse(w * 0.3f, h * 0.4f, w * 0.28f, h * 0.4f, s);
                    r.FillEllipse(w * 0.7f, h * 0.4f, w * 0.28f, h * 0.4f, s);
                    r.FillEllipse(cx, h * 0.5f, w * 0.3f, h * 0.5f, p);
                    r.FillCircle(cx - w * 0.1f, h * 0.65f, h * 0.1f, Raster.Lighten(p, 0.3f));
                    break;

                case PlaceholderShape.Cloud:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillEllipse(cx, h * 0.4f, w * 0.48f, h * 0.35f, s);
                    r.FillEllipse(w * 0.3f, h * 0.5f, w * 0.22f, h * 0.42f, s);
                    r.FillEllipse(w * 0.62f, h * 0.55f, w * 0.26f, h * 0.45f, s);
                    r.FillEllipse(cx, h * 0.42f, w * 0.44f, h * 0.28f, p);
                    r.FillEllipse(w * 0.3f, h * 0.5f, w * 0.18f, h * 0.34f, p);
                    r.FillEllipse(w * 0.62f, h * 0.55f, w * 0.22f, h * 0.38f, p);
                    break;

                case PlaceholderShape.Window:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.7f, s);
                    r.FillEllipse(cx, h * 0.7f, w * 0.4f, h * 0.28f, s);
                    r.FillRect(w * 0.18f, h * 0.06f, w * 0.82f, h * 0.7f, p);
                    r.FillEllipse(cx, h * 0.7f, w * 0.32f, h * 0.22f, p);
                    r.FillRect(cx - 2, h * 0.06f, cx + 2, h * 0.92f, s);
                    r.FillRect(w * 0.18f, h * 0.5f, w * 0.82f, h * 0.5f + 4, s);
                    if (def.id.Contains("stained"))
                    {
                        r.FillRect(w * 0.18f, h * 0.06f, cx - 2, h * 0.5f, Raster.Darken(p, 0.2f));
                        r.FillRect(cx + 2, h * 0.5f + 4, w * 0.82f, h * 0.7f, new Color(0.3f, 0.4f, 0.8f));
                    }
                    break;

                case PlaceholderShape.Arch:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(0, 0, w * 0.22f, h * 0.6f, p);
                    r.FillRect(w * 0.78f, 0, w, h * 0.6f, p);
                    r.EllipseRing(cx, h * 0.55f, w * 0.5f, h * 0.45f, w * 0.22f, p);
                    r.FillRect(0, 0, w, h * 0.55f, new Color(0, 0, 0, 0));
                    r.FillRect(0, 0, w * 0.22f, h * 0.6f, p);
                    r.FillRect(w * 0.78f, 0, w, h * 0.6f, p);
                    r.RectOutline(0, 0, w * 0.22f, h * 0.6f, 3, s);
                    r.RectOutline(w * 0.78f, 0, w, h * 0.6f, 3, s);
                    r.EllipseRing(cx, h * 0.55f, w * 0.5f, h * 0.45f, 3, s);
                    r.EllipseRing(cx, h * 0.55f, w * 0.28f, h * 0.23f, 3, s);
                    r.FillRect(0, 0, w, h * 0.55f, new Color(0, 0, 0, 0));
                    r.FillRect(0, 0, w * 0.22f, h * 0.6f, p);
                    r.FillRect(w * 0.78f, 0, w, h * 0.6f, p);
                    r.RectOutline(0, 0, w * 0.22f, h * 0.6f, 3, s);
                    r.RectOutline(w * 0.78f, 0, w, h * 0.6f, 3, s);
                    break;

                case PlaceholderShape.Pillar:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(w * 0.25f, 0, w * 0.75f, h, p);
                    r.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.08f, p);
                    r.FillRect(w * 0.1f, h * 0.92f, w * 0.9f, h, p);
                    r.RectOutline(w * 0.25f, 0, w * 0.75f, h, 2, s);
                    r.RectOutline(w * 0.1f, 0, w * 0.9f, h * 0.08f, 2, s);
                    r.RectOutline(w * 0.1f, h * 0.92f, w * 0.9f, h, 2, s);
                    for (int i = 1; i < 4; i++) r.FillRect(w * 0.25f + i * w * 0.125f, h * 0.08f, w * 0.25f + i * w * 0.125f + 2, h * 0.92f, Raster.Alpha(s, 0.5f));
                    break;

                case PlaceholderShape.Crystal:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillPolygon(new[] { new Vector2(w * 0.5f, 0), new Vector2(w * 0.3f, h * 0.2f), new Vector2(w * 0.45f, h * 0.95f), new Vector2(w * 0.6f, h * 0.9f), new Vector2(w * 0.7f, h * 0.2f) }, p);
                    r.FillPolygon(new[] { new Vector2(w * 0.25f, 0), new Vector2(w * 0.08f, h * 0.15f), new Vector2(w * 0.18f, h * 0.6f), new Vector2(w * 0.35f, h * 0.55f) }, s);
                    r.FillPolygon(new[] { new Vector2(w * 0.78f, 0), new Vector2(w * 0.65f, h * 0.25f), new Vector2(w * 0.82f, h * 0.7f), new Vector2(w * 0.95f, h * 0.2f) }, s);
                    r.FillPolygon(new[] { new Vector2(w * 0.5f, h * 0.1f), new Vector2(w * 0.42f, h * 0.25f), new Vector2(w * 0.5f, h * 0.85f), new Vector2(w * 0.55f, h * 0.25f) }, Raster.Alpha(Color.white, 0.4f));
                    break;

                case PlaceholderShape.Statue:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(w * 0.1f, 0, w * 0.9f, h * 0.12f, s);
                    r.FillRect(w * 0.3f, h * 0.12f, w * 0.7f, h * 0.6f, p);
                    r.FillRect(w * 0.15f, h * 0.45f, w * 0.85f, h * 0.62f, p);
                    r.FillCircle(cx, h * 0.75f, w * 0.18f, p);
                    r.FillRect(cx - w * 0.2f, h * 0.7f, cx + w * 0.2f, h * 0.74f, s);
                    r.FillRect(w * 0.78f, h * 0.2f, w * 0.85f, h * 0.95f, s);
                    r.FillTriangle(new Vector2(w * 0.72f, h * 0.9f), new Vector2(w * 0.91f, h * 0.9f), new Vector2(w * 0.815f, h), s);
                    break;

                case PlaceholderShape.Vine:
                    r.Clear(new Color(0, 0, 0, 0));
                    if (def.id.Contains("cobweb"))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            float a = i * Mathf.PI / 8f;
                            r.Line(0, h, Mathf.Cos(a) * w, h - Mathf.Sin(a) * h, 1.5f, p);
                        }
                        for (int k = 1; k <= 4; k++) r.Ring(0, h, w * 0.24f * k, w * 0.24f * k - 1.5f, p);
                    }
                    else
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            float t = i / 20f;
                            float x = cx + Mathf.Sin(t * 12f) * w * 0.25f;
                            float y = h - t * h;
                            r.FillCircle(x, y, 3, s);
                            if (i % 3 == 0) r.FillEllipse(x + w * 0.2f, y, w * 0.18f, h * 0.03f, p);
                            if (i % 3 == 1) r.FillEllipse(x - w * 0.2f, y, w * 0.18f, h * 0.03f, p);
                        }
                    }
                    break;

                case PlaceholderShape.Mushroom:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(w * 0.38f, 0, w * 0.62f, h * 0.55f, s);
                    r.FillEllipse(cx, h * 0.55f, w * 0.48f, h * 0.42f, p);
                    r.FillRect(0, 0, w, h * 0.5f, new Color(0, 0, 0, 0));
                    r.FillRect(w * 0.38f, 0, w * 0.62f, h * 0.55f, s);
                    r.FillCircle(cx - w * 0.2f, h * 0.7f, w * 0.07f, s);
                    r.FillCircle(cx + w * 0.15f, h * 0.8f, w * 0.06f, s);
                    r.FillCircle(cx + w * 0.25f, h * 0.62f, w * 0.05f, s);
                    break;

                case PlaceholderShape.Stalactite:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillTriangle(new Vector2(w * 0.1f, 0), new Vector2(w * 0.9f, 0), new Vector2(cx, h), p);
                    r.FillTriangle(new Vector2(w * 0.5f, 0), new Vector2(w * 0.9f, 0), new Vector2(cx, h), Raster.Darken(p, 0.2f));
                    r.Line(w * 0.1f, 0, cx, h, 3, s);
                    r.Line(w * 0.9f, 0, cx, h, 3, s);
                    r.FillRect(0, 0, w, 4, s);
                    break;

                case PlaceholderShape.Trigger:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRoundRect(2, 2, w - 2, h - 2, 10, p);
                    r.RoundRectOutline(2, 2, w - 2, h - 2, 10, 3, Raster.Darken(p, 0.4f));
                    BitmapFont.DrawCentered(r, string.IsNullOrEmpty(def.code) ? "T" : def.code, Color.white, 0.2f);
                    break;

                case PlaceholderShape.StartMarker:
                    r.Clear(new Color(0, 0, 0, 0));
                    r.RoundRectOutline(2, 2, w - 2, h - 2, 8, 4, p);
                    r.FillTriangle(new Vector2(w * 0.3f, h * 0.25f), new Vector2(w * 0.3f, h * 0.75f), new Vector2(w * 0.8f, h * 0.5f), p);
                    break;

                case PlaceholderShape.Text:
                    r.Clear(new Color(0, 0, 0, 0));
                    BitmapFont.DrawCentered(r, "TEXT", p, 0.15f);
                    break;

                default:
                    r.Clear(p);
                    r.Border(3, s);
                    break;
            }
        }

        static string PortalCode(PortalType t)
        {
            switch (t)
            {
                case PortalType.GravityNormal: return "UP";
                case PortalType.GravityFlip: return "DN";
                case PortalType.SizeNormal: return "BIG";
                case PortalType.SizeMini: return "MINI";
                case PortalType.MirrorOn: return "MR";
                case PortalType.MirrorOff: return "MR-";
                case PortalType.DualOn: return "X2";
                case PortalType.DualOff: return "X1";
                case PortalType.Teleport: return "TP";
            }
            return "";
        }
    }
}
