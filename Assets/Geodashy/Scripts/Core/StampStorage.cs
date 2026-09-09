using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>A reusable group of objects stored relative to its centre (a spike triple, a stair set, a gate with torches).</summary>
    [Serializable]
    public class Stamp
    {
        public string name = "Stamp";
        public float width = 1f;
        public float height = 1f;
        /// <summary>The most common object type, used for the palette tile.</summary>
        public string previewType = "";
        public List<LevelObject> objects = new List<LevelObject>();
        [NonSerialized] public string path;

        public static Stamp FromObjects(string name, IEnumerable<LevelObject> source)
        {
            var list = new List<LevelObject>(source);
            var s = new Stamp { name = name };
            if (list.Count == 0) return s;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            var counts = new Dictionary<string, int>();
            foreach (var o in list)
            {
                var def = ObjectCatalog.Get(o.type);
                float hw = def != null ? def.width * Mathf.Abs(o.scaleX) / 2f : 0.5f, hh = def != null ? def.height * Mathf.Abs(o.scaleY) / 2f : 0.5f;
                minX = Mathf.Min(minX, o.x - hw); maxX = Mathf.Max(maxX, o.x + hw);
                minY = Mathf.Min(minY, o.y - hh); maxY = Mathf.Max(maxY, o.y + hh);
                counts[o.type] = counts.TryGetValue(o.type, out var c) ? c + 1 : 1;
            }
            float cx = (minX + maxX) / 2f, cy = (minY + maxY) / 2f;
            s.width = Mathf.Max(0.5f, maxX - minX);
            s.height = Mathf.Max(0.5f, maxY - minY);
            int best = 0;
            foreach (var kv in counts) if (kv.Value > best) { best = kv.Value; s.previewType = kv.Key; }
            foreach (var o in list)
            {
                var c = o.Clone();
                c.x -= cx;
                c.y -= cy;
                c.groups = new List<int>();
                s.objects.Add(c);
            }
            return s;
        }

        /// <summary>Fresh clones placed around a centre.</summary>
        public List<LevelObject> Place(Vector2 center)
        {
            var result = new List<LevelObject>();
            foreach (var o in objects)
            {
                var c = o.Clone();
                c.x += center.x;
                c.y += center.y;
                result.Add(c);
            }
            return result;
        }
    }

    public static class StampStorage
    {
        public static string Directory_
        {
            get
            {
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "stamps");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static List<Stamp> List()
        {
            var list = new List<Stamp>();
            try
            {
                foreach (var file in Directory.GetFiles(Directory_, "*.json"))
                {
                    try
                    {
                        var s = JsonUtility.FromJson<Stamp>(File.ReadAllText(file));
                        if (s == null || s.objects == null) continue;
                        s.path = file;
                        list.Add(s);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Skipping unreadable stamp " + file + ": " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not list stamps: " + e.Message);
            }
            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        public static Stamp Save(Stamp s)
        {
            var name = LevelStorage.SafeFileName(string.IsNullOrWhiteSpace(s.name) ? "stamp" : s.name);
            var path = Path.Combine(Directory_, name + ".json");
            int n = 2;
            while (File.Exists(path) && s.path != path) path = Path.Combine(Directory_, name + "_" + n++ + ".json");
            s.path = path;
            File.WriteAllText(path, JsonUtility.ToJson(s));
            return s;
        }

        public static void Delete(Stamp s)
        {
            if (!string.IsNullOrEmpty(s.path) && File.Exists(s.path)) File.Delete(s.path);
        }
    }
}
