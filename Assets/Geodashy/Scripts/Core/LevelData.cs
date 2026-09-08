using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// A key/value property on an object (trigger parameters, text, etc.).
    /// Stored as strings so the format stays flat and forward compatible.
    /// </summary>
    [Serializable]
    public class ObjectProp
    {
        public string key;
        public string value;

        public ObjectProp() { }
        public ObjectProp(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>A colour channel used by objects. Channel ids 1..999 are user channels.</summary>
    [Serializable]
    public class ColorChannel
    {
        public int id;
        public Color color = Color.white;
        public bool blending;
        public float opacity = 1f;
        public bool copyPlayerColor;

        public ColorChannel() { }
        public ColorChannel(int id, Color color)
        {
            this.id = id;
            this.color = color;
        }

        public ColorChannel Clone()
        {
            return new ColorChannel
            {
                id = id, color = color, blending = blending, opacity = opacity, copyPlayerColor = copyPlayerColor
            };
        }
    }

    public static class ColorChannelIds
    {
        public const int Default = 0;   // object keeps its own sprite colour
        public const int Background = 1000;
        public const int Ground = 1001;
        public const int Line = 1002;
        public const int Object = 1003;
        public const int Player1 = 1004;
        public const int Player2 = 1005;
        public const int Detail = 1006;

        public static string Name(int id)
        {
            switch (id)
            {
                case Default: return "Default";
                case Background: return "BG";
                case Ground: return "Ground";
                case Line: return "Line";
                case Object: return "Obj";
                case Player1: return "P1";
                case Player2: return "P2";
                case Detail: return "Detail";
                default: return id.ToString();
            }
        }
    }

    /// <summary>One placed object in a level.</summary>
    [Serializable]
    public class LevelObject
    {
        public int uid;
        public string type = "";
        public float x;
        public float y;
        public float rotation;
        public float scaleX = 1f;
        public float scaleY = 1f;
        public bool flipX;
        public bool flipY;
        /// <summary>-4..4. Negative = behind the player, positive = in front.</summary>
        public int zLayer;
        /// <summary>Fine ordering inside a z layer (-50..50).</summary>
        public int zOrder;
        /// <summary>Editor-only layer used for hiding/locking groups of objects while editing.</summary>
        public int editorLayer;
        public int baseColor = ColorChannelIds.Default;
        public int detailColor = ColorChannelIds.Default;
        public List<int> groups = new List<int>();
        public bool dontFade;
        public bool dontEnter;
        public bool noTouch;
        public bool highDetail;
        public List<ObjectProp> props = new List<ObjectProp>();

        public LevelObject Clone()
        {
            var o = new LevelObject
            {
                uid = uid,
                type = type,
                x = x,
                y = y,
                rotation = rotation,
                scaleX = scaleX,
                scaleY = scaleY,
                flipX = flipX,
                flipY = flipY,
                zLayer = zLayer,
                zOrder = zOrder,
                editorLayer = editorLayer,
                baseColor = baseColor,
                detailColor = detailColor,
                groups = new List<int>(groups),
                dontFade = dontFade,
                dontEnter = dontEnter,
                noTouch = noTouch,
                highDetail = highDetail,
                props = new List<ObjectProp>(props.Count)
            };
            foreach (var p in props) o.props.Add(new ObjectProp(p.key, p.value));
            return o;
        }

        public Vector2 Position
        {
            get => new Vector2(x, y);
            set { x = value.x; y = value.y; }
        }

        // ---- property helpers -------------------------------------------------

        public bool HasProp(string key)
        {
            for (int i = 0; i < props.Count; i++) if (props[i].key == key) return true;
            return false;
        }

        public string GetString(string key, string fallback = "")
        {
            for (int i = 0; i < props.Count; i++) if (props[i].key == key) return props[i].value ?? fallback;
            return fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            var s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            var s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return Mathf.RoundToInt(f);
            return fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            var s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            return s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }

        public Color GetColor(string key, Color fallback)
        {
            var s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            return ColorUtility.TryParseHtmlString(s.StartsWith("#") ? s : "#" + s, out var c) ? c : fallback;
        }

        public T GetEnum<T>(string key, T fallback) where T : struct
        {
            var s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            return Enum.TryParse<T>(s, true, out var v) ? v : fallback;
        }

        public void SetProp(string key, string value)
        {
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i].key == key)
                {
                    props[i].value = value;
                    return;
                }
            }
            props.Add(new ObjectProp(key, value));
        }

        public void SetProp(string key, float value) => SetProp(key, value.ToString("R", CultureInfo.InvariantCulture));
        public void SetProp(string key, int value) => SetProp(key, value.ToString(CultureInfo.InvariantCulture));
        public void SetProp(string key, bool value) => SetProp(key, value ? "1" : "0");
        public void SetProp(string key, Color value) => SetProp(key, "#" + ColorUtility.ToHtmlStringRGBA(value));

        public void RemoveProp(string key)
        {
            for (int i = props.Count - 1; i >= 0; i--) if (props[i].key == key) props.RemoveAt(i);
        }

        public bool InGroup(int group) => groups.Contains(group);

        public void AddGroup(int group)
        {
            if (group > 0 && !groups.Contains(group)) groups.Add(group);
        }
    }

    /// <summary>Global settings of a level.</summary>
    [Serializable]
    public class LevelSettings
    {
        public string startMount = "horse";
        public int startSpeed = (int)SpeedTier.Normal;
        public bool startGravityFlipped;
        public bool startMini;
        public bool startDual;
        public bool startMirror;

        public string backgroundTheme = "castle";
        /// <summary>Optional per-layer overrides ("" = use theme).</summary>
        public string bgFarOverride = "";
        public string bgMidOverride = "";
        public string bgNearOverride = "";
        public float parallaxFar = 0.10f;
        public float parallaxMid = 0.30f;
        public float parallaxNear = 0.60f;

        public string groundTheme = "stone";
        public float groundY = 0f;
        /// <summary>Room height for flying mounts (dragon, griffin, wisp), in blocks above ground.</summary>
        public float ceilingHeight = 10f;

        public Color backgroundColor = new Color(0.16f, 0.20f, 0.36f);
        public Color groundColor = new Color(0.26f, 0.24f, 0.30f);
        public Color lineColor = Color.white;
        public Color objectColor = Color.white;

        public string songId = "";
        public float songOffset = 0f;
        public float bpm = 120f;
        public bool fadeIn = true;
        public bool fadeOut = true;

        /// <summary>If no Finish object exists, the level ends this many blocks after the last object.</summary>
        public float finishPadding = 8f;
        public bool twoPlayerMode;

        public LevelSettings Clone() => (LevelSettings)MemberwiseClone();
    }

    /// <summary>A complete level: metadata, settings, colour channels and objects.</summary>
    [Serializable]
    public class LevelData
    {
        public const int CurrentFormatVersion = 1;

        public int formatVersion = CurrentFormatVersion;
        public string id = "";
        public string name = "Untitled Quest";
        public string author = "";
        public string description = "";
        public long createdUnix;
        public long modifiedUnix;
        public int nextUid = 1;
        public float editorCameraX;
        public float editorCameraY;
        public float editorZoom = 1f;
        public float playtestX = -1f;
        public float playtestY = -1f;

        public LevelSettings settings = new LevelSettings();
        public List<ColorChannel> colors = new List<ColorChannel>();
        public List<LevelObject> objects = new List<LevelObject>();

        public static LevelData CreateNew(string name = "Untitled Quest")
        {
            var data = new LevelData
            {
                id = Guid.NewGuid().ToString("N"),
                name = name,
                createdUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                modifiedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            data.EnsureDefaultColors();
            return data;
        }

        public void EnsureDefaultColors()
        {
            if (colors == null) colors = new List<ColorChannel>();
            Color[] defaults =
            {
                new Color(0.95f, 0.80f, 0.30f), new Color(0.45f, 0.80f, 0.95f), new Color(0.90f, 0.35f, 0.35f),
                new Color(0.45f, 0.85f, 0.45f), new Color(0.75f, 0.45f, 0.90f), new Color(0.95f, 0.55f, 0.20f),
                new Color(0.85f, 0.85f, 0.85f), new Color(0.35f, 0.35f, 0.40f), new Color(0.20f, 0.60f, 0.55f),
                new Color(0.90f, 0.60f, 0.75f)
            };
            for (int i = 1; i <= 10; i++)
            {
                if (GetColorChannel(i) == null) colors.Add(new ColorChannel(i, defaults[i - 1]));
            }
        }

        public ColorChannel GetColorChannel(int id)
        {
            for (int i = 0; i < colors.Count; i++) if (colors[i].id == id) return colors[i];
            return null;
        }

        public ColorChannel GetOrCreateColorChannel(int id)
        {
            var ch = GetColorChannel(id);
            if (ch != null) return ch;
            ch = new ColorChannel(id, Color.white);
            colors.Add(ch);
            return ch;
        }

        public Color ResolveColor(int channelId, Color fallback)
        {
            switch (channelId)
            {
                case ColorChannelIds.Default: return fallback;
                case ColorChannelIds.Background: return settings.backgroundColor;
                case ColorChannelIds.Ground: return settings.groundColor;
                case ColorChannelIds.Line: return settings.lineColor;
                case ColorChannelIds.Object: return settings.objectColor;
                case ColorChannelIds.Player1: return new Color(0.4f, 1f, 0.4f);
                case ColorChannelIds.Player2: return new Color(0.4f, 0.8f, 1f);
                case ColorChannelIds.Detail: return Color.white;
            }
            var ch = GetColorChannel(channelId);
            if (ch == null) return fallback;
            var c = ch.color;
            c.a = ch.opacity;
            return c;
        }

        public int AllocateUid() => nextUid++;

        public LevelObject FindByUid(int uid)
        {
            for (int i = 0; i < objects.Count; i++) if (objects[i].uid == uid) return objects[i];
            return null;
        }

        /// <summary>Largest x of any non-trigger object, or 0.</summary>
        public float GetLastObjectX()
        {
            float max = 0f;
            foreach (var o in objects)
            {
                var def = ObjectCatalog.Get(o.type);
                if (def != null && (def.kind == ObjectKind.Trigger || def.kind == ObjectKind.StartPos)) continue;
                if (o.x > max) max = o.x;
            }
            return max;
        }

        /// <summary>X where the level ends: explicit Finish object or last object + padding.</summary>
        public float GetFinishX()
        {
            float finish = -1f;
            foreach (var o in objects)
            {
                var def = ObjectCatalog.Get(o.type);
                if (def != null && def.kind == ObjectKind.Finish && (finish < 0 || o.x < finish)) finish = o.x;
            }
            if (finish >= 0f) return finish;
            return GetLastObjectX() + settings.finishPadding;
        }

        public LevelData DeepClone()
        {
            return LevelSerializer.FromJson(LevelSerializer.ToJson(this, false));
        }
    }
}
