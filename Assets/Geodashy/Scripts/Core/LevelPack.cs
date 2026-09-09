using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Several levels bundled into one pasteable code ("LYREPACK1:…"); imported levels carry the pack name.</summary>
    public static class LevelPack
    {
        public const string Prefix = "LYREPACK1:";

        [Serializable]
        class Bundle
        {
            public string name = "";
            public List<string> levels = new List<string>();
        }

        public static string Encode(string name, List<LevelData> levels)
        {
            var b = new Bundle { name = name };
            foreach (var l in levels) b.levels.Add(LevelSerializer.ToJson(l, false));
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(b));
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true)) gz.Write(bytes, 0, bytes.Length);
                return Prefix + Convert.ToBase64String(ms.ToArray());
            }
        }

        public static bool TryDecode(string code, out string name, out List<LevelData> levels, out string error)
        {
            name = "";
            levels = new List<LevelData>();
            error = "";
            if (string.IsNullOrWhiteSpace(code)) { error = "Empty code"; return false; }
            int at = code.IndexOf(Prefix, StringComparison.Ordinal);
            if (at < 0) { error = "Not a level pack code (it should start with " + Prefix + ")"; return false; }
            var sb = new StringBuilder();
            foreach (var ch in code.Substring(at + Prefix.Length)) if (!char.IsWhiteSpace(ch)) sb.Append(ch);
            try
            {
                var bytes = Convert.FromBase64String(sb.ToString());
                using (var ms = new MemoryStream(bytes))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                using (var outMs = new MemoryStream())
                {
                    gz.CopyTo(outMs);
                    var b = JsonUtility.FromJson<Bundle>(Encoding.UTF8.GetString(outMs.ToArray()));
                    if (b == null || b.levels == null) { error = "Empty pack"; return false; }
                    name = b.name ?? "";
                    foreach (var json in b.levels)
                    {
                        if (!LevelSerializer.TryFromJson(json, out var d, out var err)) { error = err; return false; }
                        d.pack = name;
                        levels.Add(d);
                    }
                    return levels.Count > 0;
                }
            }
            catch (Exception e)
            {
                error = "Could not read the pack: " + e.Message;
                return false;
            }
        }
    }
}
