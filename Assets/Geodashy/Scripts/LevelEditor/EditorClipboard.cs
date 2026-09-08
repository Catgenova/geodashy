using System;
using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Editing
{
    /// <summary>Holds copied objects (relative to their bounding-box centre) and mirrors them to the OS clipboard as JSON.</summary>
    public class EditorClipboard
    {
        [Serializable]
        class Payload
        {
            public string magic = "geodashy-objects";
            public List<LevelObject> objects = new List<LevelObject>();
        }

        readonly List<LevelObject> items = new List<LevelObject>();
        public Vector2 anchor;

        public bool HasItems => items.Count > 0;
        public int Count => items.Count;

        public void Copy(IEnumerable<LevelObject> objects)
        {
            items.Clear();
            var list = new List<LevelObject>(objects);
            if (list.Count == 0) return;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var o in list)
            {
                minX = Mathf.Min(minX, o.x);
                minY = Mathf.Min(minY, o.y);
                maxX = Mathf.Max(maxX, o.x);
                maxY = Mathf.Max(maxY, o.y);
            }
            anchor = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            foreach (var o in list)
            {
                var c = o.Clone();
                c.x -= anchor.x;
                c.y -= anchor.y;
                items.Add(c);
            }
            try
            {
                var p = new Payload();
                p.objects = items;
                GUIUtility.systemCopyBuffer = JsonUtility.ToJson(p);
            }
            catch (Exception)
            {
                // clipboard may be unavailable on some platforms; the in-memory copy still works
            }
        }

        /// <summary>Tries to read objects that another editor instance copied to the OS clipboard.</summary>
        public bool TryImportFromSystem()
        {
            try
            {
                var s = GUIUtility.systemCopyBuffer;
                if (string.IsNullOrEmpty(s) || !s.Contains("geodashy-objects")) return false;
                var p = JsonUtility.FromJson<Payload>(s);
                if (p == null || p.objects == null || p.objects.Count == 0) return false;
                items.Clear();
                items.AddRange(p.objects);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Returns fresh clones positioned around the given centre.</summary>
        public List<LevelObject> Paste(Vector2 center, float grid)
        {
            var result = new List<LevelObject>();
            foreach (var o in items)
            {
                var c = o.Clone();
                c.x += center.x;
                c.y += center.y;
                result.Add(c);
            }
            return result;
        }
    }
}
