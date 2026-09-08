using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>A 3x5 pixel font for placeholder text, trigger codes and portal labels.</summary>
    public static class BitmapFont
    {
        public const int GlyphW = 3;
        public const int GlyphH = 5;
        public const int Advance = 4;
        public const int LineHeight = 6;

        static readonly Dictionary<char, string[]> glyphs = new Dictionary<char, string[]>
        {
            { 'A', new[] { "010", "101", "111", "101", "101" } },
            { 'B', new[] { "110", "101", "110", "101", "110" } },
            { 'C', new[] { "011", "100", "100", "100", "011" } },
            { 'D', new[] { "110", "101", "101", "101", "110" } },
            { 'E', new[] { "111", "100", "110", "100", "111" } },
            { 'F', new[] { "111", "100", "110", "100", "100" } },
            { 'G', new[] { "011", "100", "101", "101", "011" } },
            { 'H', new[] { "101", "101", "111", "101", "101" } },
            { 'I', new[] { "111", "010", "010", "010", "111" } },
            { 'J', new[] { "001", "001", "001", "101", "010" } },
            { 'K', new[] { "101", "101", "110", "101", "101" } },
            { 'L', new[] { "100", "100", "100", "100", "111" } },
            { 'M', new[] { "101", "111", "111", "101", "101" } },
            { 'N', new[] { "110", "101", "101", "101", "101" } },
            { 'O', new[] { "010", "101", "101", "101", "010" } },
            { 'P', new[] { "110", "101", "110", "100", "100" } },
            { 'Q', new[] { "010", "101", "101", "010", "001" } },
            { 'R', new[] { "110", "101", "110", "101", "101" } },
            { 'S', new[] { "011", "100", "010", "001", "110" } },
            { 'T', new[] { "111", "010", "010", "010", "010" } },
            { 'U', new[] { "101", "101", "101", "101", "111" } },
            { 'V', new[] { "101", "101", "101", "101", "010" } },
            { 'W', new[] { "101", "101", "111", "111", "101" } },
            { 'X', new[] { "101", "101", "010", "101", "101" } },
            { 'Y', new[] { "101", "101", "010", "010", "010" } },
            { 'Z', new[] { "111", "001", "010", "100", "111" } },
            { '0', new[] { "111", "101", "101", "101", "111" } },
            { '1', new[] { "010", "110", "010", "010", "111" } },
            { '2', new[] { "111", "001", "111", "100", "111" } },
            { '3', new[] { "111", "001", "111", "001", "111" } },
            { '4', new[] { "101", "101", "111", "001", "001" } },
            { '5', new[] { "111", "100", "111", "001", "111" } },
            { '6', new[] { "111", "100", "111", "101", "111" } },
            { '7', new[] { "111", "001", "001", "001", "001" } },
            { '8', new[] { "111", "101", "111", "101", "111" } },
            { '9', new[] { "111", "101", "111", "001", "111" } },
            { '.', new[] { "000", "000", "000", "000", "010" } },
            { ',', new[] { "000", "000", "000", "010", "100" } },
            { '!', new[] { "010", "010", "010", "000", "010" } },
            { '?', new[] { "111", "001", "011", "000", "010" } },
            { '-', new[] { "000", "000", "111", "000", "000" } },
            { '+', new[] { "000", "010", "111", "010", "000" } },
            { ':', new[] { "000", "010", "000", "010", "000" } },
            { '\'', new[] { "010", "010", "000", "000", "000" } },
            { '/', new[] { "001", "001", "010", "100", "100" } },
            { '(', new[] { "010", "100", "100", "100", "010" } },
            { ')', new[] { "010", "001", "001", "001", "010" } },
            { '<', new[] { "001", "010", "100", "010", "001" } },
            { '>', new[] { "100", "010", "001", "010", "100" } },
            { '=', new[] { "000", "111", "000", "111", "000" } },
            { '*', new[] { "101", "010", "111", "010", "101" } },
            { '#', new[] { "101", "111", "101", "111", "101" } },
            { ' ', new[] { "000", "000", "000", "000", "000" } },
        };

        /// <summary>Measures a string in font pixels (unscaled).</summary>
        public static Vector2Int Measure(string text)
        {
            if (string.IsNullOrEmpty(text)) return new Vector2Int(GlyphW, GlyphH);
            var lines = text.Split('\n');
            int maxW = 1;
            foreach (var line in lines)
            {
                int w = Mathf.Max(0, line.Length * Advance - 1);
                if (w > maxW) maxW = w;
            }
            return new Vector2Int(maxW, lines.Length * LineHeight - 1);
        }

        /// <summary>Draws text with the top-left corner at (x, y) in raster pixels; y grows upward.</summary>
        public static void Draw(Raster r, string text, int x, int topY, int scale, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            var lines = text.ToUpperInvariant().Split('\n');
            for (int li = 0; li < lines.Length; li++)
            {
                int lineTop = topY - li * LineHeight * scale;
                int cx = x;
                foreach (var ch in lines[li])
                {
                    if (!glyphs.TryGetValue(ch, out var rows)) rows = glyphs['?'];
                    for (int row = 0; row < GlyphH; row++)
                    {
                        for (int col = 0; col < GlyphW; col++)
                        {
                            if (rows[row][col] != '1') continue;
                            int px = cx + col * scale;
                            int py = lineTop - (row + 1) * scale;
                            r.FillRect(px, py, px + scale, py + scale, color);
                        }
                    }
                    cx += Advance * scale;
                }
            }
        }

        /// <summary>Draws text centred in the raster at the largest integer scale that fits within the given margin.</summary>
        public static void DrawCentered(Raster r, string text, Color color, float marginFraction = 0.15f, int maxScale = 64)
        {
            var m = Measure(text);
            int availW = Mathf.RoundToInt(r.width * (1f - marginFraction * 2f));
            int availH = Mathf.RoundToInt(r.height * (1f - marginFraction * 2f));
            int scale = Mathf.Clamp(Mathf.Min(availW / Mathf.Max(1, m.x), availH / Mathf.Max(1, m.y)), 1, maxScale);
            int w = m.x * scale, h = m.y * scale;
            int x = (r.width - w) / 2;
            int top = (r.height + h) / 2;
            Draw(r, text, x, top, scale, color);
        }
    }
}
