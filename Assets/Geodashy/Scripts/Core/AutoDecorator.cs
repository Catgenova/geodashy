using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Scatters theme-appropriate decorations over a level: tufts and moss on exposed block tops, ivy and
    /// torches on exposed sides, trees and props along the ground, hangings under overhangs. Everything it
    /// places goes on editor layer 9 so one click clears it again.
    /// </summary>
    public static class AutoDecorator
    {
        public const int Layer = 9;
        public static readonly string[] Themes = { "Castle", "Forest", "Cave", "Village", "Graveyard" };

        class Rng
        {
            uint s;
            public Rng(int seed) { s = (uint)seed * 2654435761u + 0x9E3779B9u; if (s == 0) s = 1; }
            public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }
            public bool Chance(float p) => Next() < p;
            public T Pick<T>(T[] a) => a[Mathf.Min(a.Length - 1, (int)(Next() * a.Length))];
        }

        static readonly Dictionary<string, string[]> Tops = new Dictionary<string, string[]>
        {
            { "Castle", new[] { "merlon_granite", "torch", "banner_red", "brazier", "skull", "crate" } },
            { "Forest", new[] { "grass_tuft", "grass_tuft", "wildflowers_yellow", "wildflowers_red", "bush", "mushroom_red", "fern" } },
            { "Cave", new[] { "crystal_cluster", "glow_moss", "stalagmite_deco", "mushroom_glow", "rubble" } },
            { "Village", new[] { "hay_round", "crate", "barrel", "window_box", "potted_plant", "lantern_standing" } },
            { "Graveyard", new[] { "tombstone_cross_mossy", "tombstone_round_slate", "skull", "fern", "lichen", "grass_tuft_dry" } },
        };
        static readonly Dictionary<string, string[]> Sides = new Dictionary<string, string[]>
        {
            { "Castle", new[] { "ivy_patch_1", "torch", "window_slit", "sconce" } },
            { "Forest", new[] { "ivy_patch_1", "ivy_hang_1", "lichen" } },
            { "Cave", new[] { "lichen", "moss_patch", "glow_moss" } },
            { "Village", new[] { "window_shutters_open", "lantern_hanging", "ivy_patch_1" } },
            { "Graveyard", new[] { "ivy_dead_patch_1", "cobweb", "lichen" } },
        };
        static readonly Dictionary<string, string[]> Ground = new Dictionary<string, string[]>
        {
            { "Castle", new[] { "statue_knight", "brazier", "banner_blue", "barrel", "rubble", "column_stump_granite" } },
            { "Forest", new[] { "tree_oak", "tree_pine", "bush", "tree_stump", "log", "boulder_mossy", "wildflowers_red" } },
            { "Cave", new[] { "stalagmite_deco", "crystal_cluster", "boulder_pile", "crystal_lamp" } },
            { "Village", new[] { "fence_wood", "hay_round", "well", "signpost", "wheelbarrow", "beehive", "tree_apple" } },
            { "Graveyard", new[] { "tombstone_tablet_granite", "tree_dead", "tree_yew", "obelisk_basalt", "fence_iron", "lantern_standing" } },
        };
        static readonly Dictionary<string, string[]> Hanging = new Dictionary<string, string[]>
        {
            { "Castle", new[] { "chain", "ivy_hang_2", "lantern_hanging" } },
            { "Forest", new[] { "ivy_hang_2", "ivy_hang_3", "moss_curtain" } },
            { "Cave", new[] { "stalactite", "moss_curtain", "glow_moss" } },
            { "Village", new[] { "lantern_hanging", "string_lights" } },
            { "Graveyard", new[] { "cobweb", "chain", "ivy_dead_hang_2" } },
        };

        public static List<LevelObject> Decorate(LevelData level, string theme, float density, int seed)
        {
            var result = new List<LevelObject>();
            if (!Tops.ContainsKey(theme)) theme = Themes[0];
            var rng = new Rng(seed);
            float groundY = level.settings.groundY;
            float finish = level.GetFinishX();
            var solids = new List<Rect>();
            var hazards = new List<Rect>();
            var occupied = new List<Rect>();
            foreach (var o in level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d == null || o.editorLayer == Layer) continue;
                var r = GeoMath.ObjectBounds(o, d);
                if (d.IsSolidLike) solids.Add(r);
                else if (d.kind == ObjectKind.Hazard) hazards.Add(r);
                occupied.Add(r);
            }
            bool Solid(float x, float y)
            {
                foreach (var r in solids) if (r.Contains(new Vector2(x, y))) return true;
                return false;
            }
            bool Free(Rect r, float pad)
            {
                var e = GeoMath.Expand(r, pad);
                foreach (var o in occupied) if (GeoMath.RectsOverlap(e, o)) return false;
                foreach (var h in hazards) if (GeoMath.RectsOverlap(GeoMath.Expand(e, 1f), h)) return false;
                return true;
            }
            void Place(string type, float x, float y)
            {
                var d = ObjectCatalog.Get(type);
                if (d == null) return;
                var r = new Rect(x - d.width / 2f, y - d.height / 2f, d.width, d.height);
                if (!Free(r, 0.1f)) return;
                result.Add(new LevelObject { type = type, x = x, y = y, zLayer = d.defaultZLayer, zOrder = d.defaultZOrder, editorLayer = Layer });
                occupied.Add(r);
            }
            // exposed block tops and sides
            foreach (var r in solids)
            {
                for (float x = r.xMin + 0.5f; x < r.xMax; x += 1f)
                {
                    if (!Solid(x, r.yMax + 0.5f) && rng.Chance(0.35f * density))
                    {
                        var type = rng.Pick(Tops[theme]);
                        var d = ObjectCatalog.Get(type);
                        if (d != null) Place(type, x, r.yMax + d.height / 2f);
                    }
                    if (!Solid(x, r.yMin - 0.5f) && r.yMin > groundY + 0.5f && rng.Chance(0.25f * density))
                    {
                        var type = rng.Pick(Hanging[theme]);
                        var d = ObjectCatalog.Get(type);
                        if (d != null) Place(type, x, r.yMin - d.height / 2f);
                    }
                }
                for (float y = r.yMin + 0.5f; y < r.yMax; y += 1f)
                {
                    if (!Solid(r.xMin - 0.5f, y) && rng.Chance(0.18f * density))
                    {
                        var type = rng.Pick(Sides[theme]);
                        var d = ObjectCatalog.Get(type);
                        if (d != null) Place(type, r.xMin - d.width / 2f, y);
                    }
                    if (!Solid(r.xMax + 0.5f, y) && rng.Chance(0.18f * density))
                    {
                        var type = rng.Pick(Sides[theme]);
                        var d = ObjectCatalog.Get(type);
                        if (d != null) Place(type, r.xMax + d.width / 2f, y);
                    }
                }
            }
            // ground props between the start and the finish
            for (float x = 3f; x < finish - 2f; x += 2f + rng.Next() * 4f / Mathf.Max(0.2f, density))
            {
                if (Solid(x, groundY + 0.5f) || !rng.Chance(0.7f * density)) continue;
                var type = rng.Pick(Ground[theme]);
                var d = ObjectCatalog.Get(type);
                if (d != null) Place(type, x, groundY + d.height / 2f);
            }
            return result;
        }
    }
}
