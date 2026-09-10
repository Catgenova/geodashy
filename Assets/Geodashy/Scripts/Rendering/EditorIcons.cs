using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>16 px glyph sprites for the editor's buttons, drawn on the fly and cached by id.</summary>
    public static class EditorIcons
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int N = 16;

        public static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache.TryGetValue(id, out var s) && s != null) return s;
            var r = new Raster(N, N) { punch = true };
            Draw(r, id);
            s = r.ToSprite(64f, null, null, FilterMode.Point);
            s.name = "icon_" + id;
            cache[id] = s;
            return s;
        }

        static readonly Color W = Color.white;

        static void Arrow(Raster r, float x0, float y0, float x1, float y1, float head)
        {
            r.Line(x0, y0, x1, y1, 1.6f, W);
            var d = new Vector2(x1 - x0, y1 - y0).normalized;
            var n = new Vector2(-d.y, d.x);
            var tip = new Vector2(x1, y1);
            r.FillTriangle(tip, tip - d * head + n * head * 0.7f, tip - d * head - n * head * 0.7f, W);
        }

        static void Draw(Raster r, string id)
        {
            switch (id)
            {
                case "build":   // hammer
                    r.Line(4, 3, 12, 11, 2.2f, W);
                    r.FillRect(9, 9, 15, 14, W);
                    r.FillRect(10, 8, 12, 15, W);
                    break;
                case "edit":   // arrow cursor
                    r.FillTriangle(new Vector2(3, 14), new Vector2(3, 3), new Vector2(11, 8), W);
                    r.Line(6.5f, 9, 9.5f, 14, 2f, W);
                    break;
                case "delete":   // cross
                    r.Line(3, 3, 13, 13, 2.2f, W);
                    r.Line(3, 13, 13, 3, 2.2f, W);
                    break;
                case "undo":
                    r.EllipseRing(9, 8, 5, 5, 1.7f, W);
                    r.FillRect(0, 6, 9, 10, Color.clear);
                    r.Set(4, 8, W); r.Set(4, 9, W);
                    r.FillTriangle(new Vector2(1, 8), new Vector2(6, 4), new Vector2(6, 12), W);
                    break;
                case "redo":
                    r.EllipseRing(7, 8, 5, 5, 1.7f, W);
                    r.FillRect(7, 6, 16, 10, Color.clear);
                    r.FillTriangle(new Vector2(15, 8), new Vector2(10, 4), new Vector2(10, 12), W);
                    break;
                case "play":
                    r.FillTriangle(new Vector2(4, 2), new Vector2(4, 14), new Vector2(14, 8), W);
                    break;
                case "marker":   // flag
                    r.FillRect(3, 2, 5, 15, W);
                    r.FillRect(5, 5, 14, 14, W);
                    r.FillTriangle(new Vector2(14.5f, 14.5f), new Vector2(14.5f, 4.5f), new Vector2(10.5f, 9.5f), Color.clear);
                    break;
                case "train":   // waystone / target
                    r.Ring(8, 8, 7, 5.5f, W);
                    r.FillCircle(8, 8, 2.2f, W);
                    break;
                case "save":   // floppy-ish scroll seal
                    r.FillRoundRect(2, 2, 14, 14, 2, W);
                    r.FillRect(5, 9, 11, 13, new Color(0, 0, 0, 0));
                    r.Set(5, 9, Color.clear);
                    r.FillRect(5, 10, 11, 13, Color.clear);
                    r.FillRect(4, 3, 12, 6, Color.clear);
                    r.FillRect(6, 3, 10, 6, W);
                    break;
                case "files":   // folder
                    r.FillRoundRect(1, 3, 15, 13, 1.5f, W);
                    r.FillRect(1, 12, 7, 15, W);
                    r.FillRect(2, 10, 14, 11, Color.clear);
                    break;
                case "settings":   // gear
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        r.Line(8, 8, 8 + Mathf.Cos(a) * 7f, 8 + Mathf.Sin(a) * 7f, 2.4f, W);
                    }
                    r.FillCircle(8, 8, 4.8f, W);
                    r.FillCircle(8, 8, 2.2f, Color.clear);
                    r.Set(8, 8, Color.clear);
                    break;
                case "help":
                    r.EllipseRing(8, 10, 4, 4, 1.8f, W);
                    r.FillRect(2, 2, 8, 11, Color.clear);
                    r.FillRect(2, 2, 14, 7, Color.clear);
                    r.Line(8, 6, 8, 3, 1.8f, W);
                    r.FillRect(7, 12, 9, 15, Color.clear);
                    r.FillRect(7, 1, 9, 3, W);
                    break;
                case "history":   // clock
                    r.Ring(8, 8, 7, 5.5f, W);
                    r.Line(8, 8, 8, 4, 1.5f, W);
                    r.Line(8, 8, 11, 9, 1.5f, W);
                    break;
                case "menu":   // castle gate
                    r.FillRect(2, 2, 14, 13, W);
                    r.FillRect(2, 13, 4, 15, W); r.FillRect(7, 13, 9, 15, W); r.FillRect(12, 13, 14, 15, W);
                    r.FillRect(6, 2, 10, 8, Color.clear);
                    r.FillCircle(8, 8, 2, Color.clear);
                    break;
                case "layers":
                    r.FillPolygon(new[] { new Vector2(8, 15), new Vector2(15, 11), new Vector2(8, 7), new Vector2(1, 11) }, W);
                    r.FillPolygon(new[] { new Vector2(8, 10), new Vector2(13, 7), new Vector2(8, 4), new Vector2(3, 7) }, Color.clear);
                    r.FillPolygon(new[] { new Vector2(8, 9), new Vector2(15, 5), new Vector2(8, 1), new Vector2(1, 5) }, W);
                    break;
                case "snap":   // magnet
                    r.EllipseRing(8, 7, 6, 6, 3f, W);
                    r.FillRect(0, 7, 16, 16, Color.clear);
                    r.FillRect(2, 7, 5, 13, W); r.FillRect(11, 7, 14, 13, W);
                    break;
                case "grid":
                    for (int i = 2; i <= 14; i += 4) { r.FillRect(i, 2, i + 1, 14, W); r.FillRect(2, i, 14, i + 1, W); }
                    break;
                case "hide":   // eye with slash
                    r.FillEllipse(8, 8, 7, 4, W);
                    r.FillCircle(8, 8, 2, Color.clear);
                    r.Line(2, 2, 14, 14, 1.6f, W);
                    break;
                case "show":
                    r.FillEllipse(8, 8, 7, 4, W);
                    r.FillCircle(8, 8, 2, Color.clear);
                    break;
                case "lock":
                    r.FillRoundRect(3, 1, 13, 8, 1, W);
                    r.EllipseRing(8, 9, 3.5f, 4, 1.6f, W);
                    r.FillRect(3, 0, 13, 9, Color.clear);
                    r.FillRoundRect(3, 1, 13, 8, 1, W);
                    r.FillRect(7, 3, 9, 6, Color.clear);
                    break;
                case "rotate_l":
                    r.EllipseRing(8, 8, 5.5f, 5.5f, 1.8f, W);
                    r.FillRect(0, 8, 8, 16, Color.clear);
                    r.FillTriangle(new Vector2(1, 9), new Vector2(6, 9), new Vector2(3.5f, 14), W);
                    break;
                case "rotate_r":
                    r.EllipseRing(8, 8, 5.5f, 5.5f, 1.8f, W);
                    r.FillRect(8, 8, 16, 16, Color.clear);
                    r.FillTriangle(new Vector2(15, 9), new Vector2(10, 9), new Vector2(12.5f, 14), W);
                    break;
                case "flip_h":
                    r.FillTriangle(new Vector2(2, 8), new Vector2(7, 3), new Vector2(7, 13), W);
                    r.FillTriangle(new Vector2(14, 8), new Vector2(9, 3), new Vector2(9, 13), new Color(1, 1, 1, 0.5f));
                    r.FillRect(7.5f, 1, 8.5f, 15, W);
                    break;
                case "flip_v":
                    r.FillTriangle(new Vector2(8, 14), new Vector2(3, 9), new Vector2(13, 9), W);
                    r.FillTriangle(new Vector2(8, 2), new Vector2(3, 7), new Vector2(13, 7), new Color(1, 1, 1, 0.5f));
                    r.FillRect(1, 7.5f, 15, 8.5f, W);
                    break;
                case "scale_up":
                    r.RectOutline(2, 2, 14, 14, 1.5f, W);
                    Arrow(r, 6, 6, 12, 12, 3f);
                    break;
                case "scale_down":
                    r.RectOutline(2, 2, 14, 14, 1.5f, W);
                    Arrow(r, 12, 12, 6, 6, 3f);
                    break;
                case "reset":
                    r.EllipseRing(8, 8, 5.5f, 5.5f, 1.6f, W);
                    r.FillRect(8, 8, 16, 16, Color.clear);
                    r.FillTriangle(new Vector2(14, 8), new Vector2(9, 8), new Vector2(11.5f, 13), W);
                    break;
                case "view":   // sliders
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 3 + i * 4.5f;
                        r.FillRect(2, y, 14, y + 1.2f, W);
                        r.FillCircle(4 + i * 4, y + 0.5f, 2, W);
                    }
                    break;
                case "props":   // list
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 3 + i * 4.5f;
                        r.FillRect(2, y, 4.5f, y + 2.5f, W);
                        r.FillRect(6, y + 0.5f, 14, y + 2f, W);
                    }
                    break;
                case "dock":
                    r.FillTriangle(new Vector2(3, 10), new Vector2(13, 10), new Vector2(8, 4), W);
                    r.FillRect(3, 12, 13, 14, W);
                    break;
                case "more":
                    r.FillCircle(3.5f, 8, 1.8f, W); r.FillCircle(8, 8, 1.8f, W); r.FillCircle(12.5f, 8, 1.8f, W);
                    break;
                case "search":
                    r.Ring(7, 9, 5, 3.2f, W);
                    r.Line(10, 6, 14.5f, 1.5f, 2.2f, W);
                    break;
                case "star":
                {
                    var pts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rad = i % 2 == 0 ? 7.5f : 3.2f;
                        pts[i] = new Vector2(8 + Mathf.Cos(a) * rad, 8 + Mathf.Sin(a) * rad);
                    }
                    r.FillPolygon(pts, W);
                    break;
                }
                case "copy":
                    r.RectOutline(2, 5, 11, 15, 1.5f, W);
                    r.RectOutline(5, 1, 14, 11, 1.5f, W);
                    break;
                case "paste":
                    r.FillRoundRect(3, 1, 13, 15, 1, W);
                    r.FillRect(5, 3, 11, 12, Color.clear);
                    r.FillRect(6, 0, 10, 2.5f, W);
                    r.FillRect(6, 5, 10, 6, W); r.FillRect(6, 8, 10, 9, W);
                    break;
                case "cut":   // scissors
                    r.Ring(4, 4, 3, 1.5f, W); r.Ring(4, 12, 3, 1.5f, W);
                    r.Line(6, 5, 14, 11, 1.5f, W); r.Line(6, 11, 14, 5, 1.5f, W);
                    break;
                case "dup":
                    r.FillRoundRect(1, 4, 10, 15, 1, W);
                    r.FillRoundRect(6, 1, 15, 12, 1, W);
                    r.FillRect(8, 3, 13, 10, Color.clear);
                    r.FillRect(9.5f, 4, 11.5f, 9, W); r.FillRect(8, 5.5f, 13, 7.5f, W);
                    break;
                case "plus":
                    r.FillRect(7, 2, 9, 14, W); r.FillRect(2, 7, 14, 9, W);
                    break;
                case "minus":
                    r.FillRect(2, 7, 14, 9, W);
                    break;
                case "check":
                    r.Line(2, 8, 6.5f, 3.5f, 2.4f, W); r.Line(6, 4, 14, 12, 2.4f, W);
                    break;
                case "close":
                    r.Line(4, 4, 12, 12, 2f, W); r.Line(4, 12, 12, 4, 2f, W);
                    break;
                case "bookmark":
                    r.FillRect(3, 1, 13, 15, W);
                    r.FillTriangle(new Vector2(3, 15.5f), new Vector2(13, 15.5f), new Vector2(8, 10), Color.clear);
                    break;
                case "music":
                    r.FillRect(6, 4, 7.5f, 14, W); r.FillRect(12, 2, 13.5f, 12, W);
                    r.FillRect(6, 12.5f, 13.5f, 14, W);
                    r.FillEllipse(4.5f, 4, 3, 2.2f, W); r.FillEllipse(10.5f, 2, 3, 2.2f, W);
                    break;
                case "camera":
                    r.FillRoundRect(1, 3, 15, 13, 1.5f, W);
                    r.FillRect(5, 13, 11, 15, W);
                    r.FillCircle(8, 8, 3.2f, Color.clear);
                    r.Ring(8, 8, 2.4f, 1.2f, W);
                    break;
                case "clip":   // film
                    r.FillRect(1, 3, 15, 13, W);
                    for (int x = 2; x < 15; x += 3) { r.FillRect(x, 4, x + 1.5f, 5.5f, Color.clear); r.FillRect(x, 10.5f, x + 1.5f, 12, Color.clear); }
                    break;
                case "pause":
                    r.FillRect(3, 2, 6.5f, 14, W); r.FillRect(9.5f, 2, 13, 14, W);
                    break;
                case "restart":
                    r.EllipseRing(8, 8, 5.5f, 5.5f, 1.8f, W);
                    r.FillRect(8, 0, 16, 8, Color.clear);
                    r.FillTriangle(new Vector2(8, 1), new Vector2(8, 6), new Vector2(13, 3.5f), W);
                    break;
                case "exit":
                    r.RectOutline(2, 1, 11, 15, 1.5f, W);
                    r.FillRect(9, 6, 12, 10, Color.clear);
                    Arrow(r, 6, 8, 15, 8, 3f);
                    break;
                case "info":
                    r.Ring(8, 8, 7, 5.6f, W);
                    r.FillRect(7, 6, 9, 12, W); r.FillCircle(8, 4.2f, 1.2f, W);
                    break;
                case "warn":
                    r.FillTriangle(new Vector2(8, 15), new Vector2(1, 2), new Vector2(15, 2), W);
                    r.FillRect(7, 6, 9, 11, Color.clear); r.FillCircle(8, 4, 1.1f, Color.clear);
                    break;
                case "trash":
                    r.FillRect(3, 11, 13, 13, W);
                    r.FillRect(4, 1, 12, 11, W);
                    r.FillRect(6, 3, 7, 9, Color.clear); r.FillRect(9, 3, 10, 9, Color.clear);
                    r.FillRect(6, 13, 10, 15, W);
                    break;
                case "path":
                    r.FillCircle(3, 12, 2, W); r.FillCircle(8, 5, 2, W); r.FillCircle(13, 11, 2, W);
                    r.Line(3, 12, 8, 5, 1.2f, W); r.Line(8, 5, 13, 11, 1.2f, W);
                    break;
                case "curve":
                    for (int i = 0; i < 14; i++)
                    {
                        float t0 = i / 14f, t1 = (i + 1) / 14f;
                        r.Line(2 + t0 * 12, 8 + Mathf.Sin(t0 * Mathf.PI * 2f) * 5f, 2 + t1 * 12, 8 + Mathf.Sin(t1 * Mathf.PI * 2f) * 5f, 1.6f, W);
                    }
                    break;
                case "swipe":
                    r.FillRect(2, 7, 11, 9, W);
                    r.FillTriangle(new Vector2(15, 8), new Vector2(10, 4), new Vector2(10, 12), W);
                    r.FillRect(2, 2, 5, 4, W); r.FillRect(2, 12, 5, 14, W);
                    break;
                default:
                    r.Ring(8, 8, 6, 4.5f, W);
                    break;
            }
        }
    }
}
