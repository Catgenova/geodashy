using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Levels as pasteable text: compact JSON, gzip, base64, with a version prefix. Songs are not included
    /// (the receiving side keeps the built-in song id; an imported song file has to be added again).
    /// </summary>
    public static class LevelShare
    {
        public const string Prefix = "LYRE1:";

        public static string Encode(LevelData level)
        {
            var json = LevelSerializer.ToJson(level, false);
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true)) gz.Write(bytes, 0, bytes.Length);
                return Prefix + Convert.ToBase64String(ms.ToArray());
            }
        }

        public static bool TryDecode(string code, out LevelData level, out string error)
        {
            level = null;
            error = "";
            if (string.IsNullOrWhiteSpace(code))
            {
                error = "Empty code";
                return false;
            }
            code = code.Trim();
            int at = code.IndexOf(Prefix, StringComparison.Ordinal);
            if (at < 0)
            {
                error = "Not a LyreFlyer share code (it should start with " + Prefix + ")";
                return false;
            }
            code = code.Substring(at + Prefix.Length);
            var sb = new StringBuilder(code.Length);
            foreach (var ch in code) if (!char.IsWhiteSpace(ch)) sb.Append(ch);
            try
            {
                var bytes = Convert.FromBase64String(sb.ToString());
                using (var ms = new MemoryStream(bytes))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                using (var outMs = new MemoryStream())
                {
                    gz.CopyTo(outMs);
                    var json = Encoding.UTF8.GetString(outMs.ToArray());
                    return LevelSerializer.TryFromJson(json, out level, out error);
                }
            }
            catch (Exception e)
            {
                error = "Could not read the code: " + e.Message;
                return false;
            }
        }
    }
}
