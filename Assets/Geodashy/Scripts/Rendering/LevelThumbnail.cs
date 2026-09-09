using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Small schematic picture of a level: sky, ground line, blocks in their colours, hazards red, loot gold.</summary>
    public static class LevelThumbnail
    {
        public const int Width = 256, Height = 96;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static readonly Dictionary<string, Sprite> fileCache = new Dictionary<string, Sprite>();

        /// <summary>Thumbnail for a level on disk; the JSON is only parsed when the file changed since the last look.</summary>
        public static Sprite Get(LevelFileInfo info)
        {
            if (info == null) return null;
            string key = info.id + ":" + info.modified.Ticks + ":" + info.objectCount;
            if (fileCache.TryGetValue(key, out var s) && s != null) return s;
            try
            {
                s = Get(LevelStorage.Load(info.path));
            }
            catch (System.Exception)
            {
                s = null;
            }
            fileCache[key] = s;
            return s;
        }

        public static Sprite Get(LevelData level)
        {
            if (level == null) return null;
            string key = level.id + ":" + level.modifiedUnix + ":" + level.objects.Count;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = Render(level);
            cache[key] = s;
            return s;
        }

        static Sprite Render(LevelData level)
        {
            var r = new Raster(Width, Height);
            var theme = ThemeCatalog.GetBackground(level.settings.backgroundTheme);
            r.GradientV(level.settings.backgroundColor, theme.skyTop);
            float finish = Mathf.Max(10f, level.GetFinishX() + 4f);
            float groundY = level.settings.groundY;
            float top = groundY + Mathf.Max(8f, level.settings.ceilingHeight + 1f);
            float sx = Width / finish, sy = Height / (top - groundY + 1.5f);
            float X(float x) => x * sx;
            float Y(float y) => (y - groundY + 1.5f) * sy;
            var ground = ThemeCatalog.GetGround(level.settings.groundTheme);
            r.FillRect(0, 0, Width, Y(groundY), ground.body);
            r.FillRect(0, Y(groundY) - 1, Width, Y(groundY) + 1, ground.top);
            foreach (var o in level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d == null || d.kind == ObjectKind.Trigger || d.kind == ObjectKind.StartPos) continue;
                var b = GeoMath.ObjectBounds(o, d);
                Color c;
                switch (d.kind)
                {
                    case ObjectKind.Hazard: c = new Color(1f, 0.3f, 0.3f); break;
                    case ObjectKind.Collectible: c = new Color(1f, 0.85f, 0.3f); break;
                    case ObjectKind.Orb:
                    case ObjectKind.Pad:
                    case ObjectKind.Portal: c = d.primaryColor; break;
                    case ObjectKind.Decoration: c = new Color(d.primaryColor.r, d.primaryColor.g, d.primaryColor.b, 0.45f); break;
                    case ObjectKind.Finish: c = new Color(1f, 0.9f, 0.4f); break;
                    default: c = level.ResolveColor(o.baseColor, d.primaryColor); break;
                }
                float x0 = X(b.xMin), x1 = Mathf.Max(X(b.xMax), x0 + 1f), y0 = Y(b.yMin), y1 = Mathf.Max(Y(b.yMax), y0 + 1f);
                if (d.kind == ObjectKind.Collectible) r.FillCircle((x0 + x1) / 2f, (y0 + y1) / 2f, 1.5f, c);
                else r.FillRect(x0, y0, x1, y1, c);
            }
            r.Border(1, new Color(0, 0, 0, 0.5f));
            var sprite = r.ToSprite(64f);
            sprite.name = "thumb_" + level.id;
            return sprite;
        }
    }
}
