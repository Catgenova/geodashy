using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Small GIF89a writer: a fixed 6x7x6 colour cube (252 colours) with ordered dithering and a straightforward
    /// LZW encoder. Good enough for short gameplay clips; no dependencies.
    /// </summary>
    public static class GifEncoder
    {
        const int Rn = 6, Gn = 7, Bn = 6;
        static readonly float[,] Bayer =
        {
            { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 }
        };

        static byte[] PaletteBytes()
        {
            var p = new byte[256 * 3];
            int i = 0;
            for (int r = 0; r < Rn; r++)
            for (int g = 0; g < Gn; g++)
            for (int b = 0; b < Bn; b++)
            {
                p[i++] = (byte)(r * 255 / (Rn - 1));
                p[i++] = (byte)(g * 255 / (Gn - 1));
                p[i++] = (byte)(b * 255 / (Bn - 1));
            }
            return p;
        }

        static byte[] Quantize(Color32[] pixels, int w, int h)
        {
            var idx = new byte[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // GIF rows run top to bottom; Unity pixel arrays run bottom to top
                var c = pixels[(h - 1 - y) * w + x];
                float d = (Bayer[y & 3, x & 3] / 16f - 0.5f);
                int r = Mathf.Clamp(Mathf.RoundToInt(c.r / 255f * (Rn - 1) + d * 0.9f), 0, Rn - 1);
                int g = Mathf.Clamp(Mathf.RoundToInt(c.g / 255f * (Gn - 1) + d * 0.9f), 0, Gn - 1);
                int b = Mathf.Clamp(Mathf.RoundToInt(c.b / 255f * (Bn - 1) + d * 0.9f), 0, Bn - 1);
                idx[y * w + x] = (byte)((r * Gn + g) * Bn + b);
            }
            return idx;
        }

        /// <summary>Writes frames (all the same size) with a per-frame delay in hundredths of a second, looping forever.</summary>
        public static void Write(string path, List<Color32[]> frames, int width, int height, int delayCs)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(System.Text.Encoding.ASCII.GetBytes("GIF89a"));
                bw.Write((ushort)width);
                bw.Write((ushort)height);
                bw.Write((byte)0xF7);   // global colour table, 8 bits per colour, 256 entries
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write(PaletteBytes());
                // Netscape loop extension
                bw.Write((byte)0x21); bw.Write((byte)0xFF); bw.Write((byte)11);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("NETSCAPE2.0"));
                bw.Write((byte)3); bw.Write((byte)1); bw.Write((ushort)0); bw.Write((byte)0);
                foreach (var frame in frames)
                {
                    bw.Write((byte)0x21); bw.Write((byte)0xF9); bw.Write((byte)4);
                    bw.Write((byte)0);   // no transparency, no disposal
                    bw.Write((ushort)delayCs);
                    bw.Write((byte)0); bw.Write((byte)0);
                    bw.Write((byte)0x2C);
                    bw.Write((ushort)0); bw.Write((ushort)0);
                    bw.Write((ushort)width); bw.Write((ushort)height);
                    bw.Write((byte)0);   // no local colour table
                    var indices = Quantize(frame, width, height);
                    Lzw(bw, indices, 8);
                }
                bw.Write((byte)0x3B);
            }
        }

        static void Lzw(BinaryWriter bw, byte[] indices, int minCodeSize)
        {
            bw.Write((byte)minCodeSize);
            int clear = 1 << minCodeSize, eoi = clear + 1;
            var dict = new Dictionary<int, int>();
            int codeSize = minCodeSize + 1, next = eoi + 1;
            var block = new List<byte>(256);
            int bitBuffer = 0, bitCount = 0;
            void Emit(int code)
            {
                bitBuffer |= code << bitCount;
                bitCount += codeSize;
                while (bitCount >= 8)
                {
                    block.Add((byte)(bitBuffer & 0xFF));
                    bitBuffer >>= 8;
                    bitCount -= 8;
                    if (block.Count == 255)
                    {
                        bw.Write((byte)255);
                        bw.Write(block.ToArray());
                        block.Clear();
                    }
                }
            }
            Emit(clear);
            int prefix = indices.Length > 0 ? indices[0] : 0;
            for (int i = 1; i < indices.Length; i++)
            {
                int k = indices[i];
                int key = (prefix << 8) | k;
                if (dict.TryGetValue(key, out int code))
                {
                    prefix = code;
                    continue;
                }
                Emit(prefix);
                if (next < 4096)
                {
                    dict[key] = next++;
                    if (next > (1 << codeSize) && codeSize < 12) codeSize++;
                }
                else
                {
                    Emit(clear);
                    dict.Clear();
                    codeSize = minCodeSize + 1;
                    next = eoi + 1;
                }
                prefix = k;
            }
            Emit(prefix);
            Emit(eoi);
            if (bitCount > 0) block.Add((byte)(bitBuffer & 0xFF));
            if (block.Count > 0)
            {
                bw.Write((byte)block.Count);
                bw.Write(block.ToArray());
            }
            bw.Write((byte)0);
        }
    }
}
