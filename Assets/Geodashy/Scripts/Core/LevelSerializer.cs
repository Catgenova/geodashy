using System;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>JSON (de)serialisation of levels with light validation and migration.</summary>
    public static class LevelSerializer
    {
        public static string ToJson(LevelData data, bool pretty = true)
        {
            data.formatVersion = LevelData.CurrentFormatVersion;
            return JsonUtility.ToJson(data, pretty);
        }

        public static LevelData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Empty level JSON");
            var data = JsonUtility.FromJson<LevelData>(json);
            if (data == null) throw new ArgumentException("Could not parse level JSON");
            Sanitize(data);
            return data;
        }

        public static bool TryFromJson(string json, out LevelData data, out string error)
        {
            try
            {
                data = FromJson(json);
                error = null;
                return true;
            }
            catch (Exception e)
            {
                data = null;
                error = e.Message;
                return false;
            }
        }

        /// <summary>Copies the pre-layer-settings fields into far/mid/near the first time an old level is loaded.</summary>
        public static void MigrateParallax(LevelSettings s)
        {
            if (s.far == null) s.far = new ParallaxLayerSettings();
            if (s.mid == null) s.mid = new ParallaxLayerSettings();
            if (s.near == null) s.near = new ParallaxLayerSettings();
            if (s.far.parallax < 0f)
            {
                s.far.parallax = s.parallaxFar;
                if (!string.IsNullOrEmpty(s.bgFarOverride)) s.far.layerId = s.bgFarOverride;
            }
            if (s.mid.parallax < 0f)
            {
                s.mid.parallax = s.parallaxMid;
                if (!string.IsNullOrEmpty(s.bgMidOverride)) s.mid.layerId = s.bgMidOverride;
            }
            if (s.near.parallax < 0f)
            {
                s.near.parallax = s.parallaxNear;
                if (!string.IsNullOrEmpty(s.bgNearOverride)) s.near.layerId = s.bgNearOverride;
            }
            for (int i = 0; i < 3; i++)
            {
                var l = s.Layer(i);
                if (l.scale <= 0.01f) l.scale = 1f;
                if (l.tint.a <= 0f && l.tint.r <= 0f && l.tint.g <= 0f && l.tint.b <= 0f) l.tint = Color.white;
            }
        }

        /// <summary>Repairs missing fields, unknown types and uid collisions.</summary>
        public static void Sanitize(LevelData data)
        {
            if (data.settings == null) data.settings = new LevelSettings();
            if (data.objects == null) data.objects = new System.Collections.Generic.List<LevelObject>();
            if (data.colors == null) data.colors = new System.Collections.Generic.List<ColorChannel>();
            if (string.IsNullOrEmpty(data.id)) data.id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(data.name)) data.name = "Untitled Quest";
            if (data.editorZoom <= 0.01f) data.editorZoom = 1f;
            data.EnsureDefaultColors();
            MigrateParallax(data.settings);

            var seen = new System.Collections.Generic.HashSet<int>();
            int maxUid = 0;
            for (int i = data.objects.Count - 1; i >= 0; i--)
            {
                var o = data.objects[i];
                if (o == null || ObjectCatalog.Get(o.type) == null)
                {
                    data.objects.RemoveAt(i);
                    continue;
                }
                if (o.groups == null) o.groups = new System.Collections.Generic.List<int>();
                if (o.props == null) o.props = new System.Collections.Generic.List<ObjectProp>();
                if (Mathf.Approximately(o.scaleX, 0f)) o.scaleX = 1f;
                if (Mathf.Approximately(o.scaleY, 0f)) o.scaleY = 1f;
                o.zLayer = Mathf.Clamp(o.zLayer, -4, 4);
                o.zOrder = Mathf.Clamp(o.zOrder, -50, 50);
                if (o.uid <= 0 || seen.Contains(o.uid)) o.uid = -1; // fix below
                else
                {
                    seen.Add(o.uid);
                    if (o.uid > maxUid) maxUid = o.uid;
                }
            }
            foreach (var o in data.objects)
            {
                if (o.uid == -1) o.uid = ++maxUid;
            }
            if (data.nextUid <= maxUid) data.nextUid = maxUid + 1;
        }
    }
}
