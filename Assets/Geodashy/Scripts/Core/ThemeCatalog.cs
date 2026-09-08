using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>One of the three parallax layers of a background theme.</summary>
    public class BackgroundLayerDefinition
    {
        public string id;
        public string name;
        /// <summary>Procedural style used for the placeholder art.</summary>
        public string style;
        public Color skyTop;
        public Color skyBottom;
        public Color silhouette;
        public float heightBlocks = 12f;
        public float widthBlocks = 24f;
        public int seed;
    }

    public class BackgroundTheme
    {
        public string id;
        public string name;
        public string far;
        public string mid;
        public string near;
        public Color skyTop;
        public Color skyBottom;
    }

    public class GroundTheme
    {
        public string id;
        public string name;
        public Color top;
        public Color body;
        public Color line;
        public string style;
    }

    /// <summary>Background and ground themes. Real art goes in Resources/Backgrounds/{layerId}.png and Resources/Backgrounds/ground_{id}.png.</summary>
    public static class ThemeCatalog
    {
        public static readonly List<BackgroundLayerDefinition> Layers = new List<BackgroundLayerDefinition>();
        public static readonly List<BackgroundTheme> Backgrounds = new List<BackgroundTheme>();
        public static readonly List<GroundTheme> Grounds = new List<GroundTheme>();

        static ThemeCatalog()
        {
            // ---- layers -------------------------------------------------------
            Layer("mountains_far", "Distant Mountains", "mountains", "3a3f66", 14, 32, 11);
            Layer("mountains_snow", "Snowy Peaks", "mountains", "9fb4d8", 14, 32, 12);
            Layer("hills_mid", "Rolling Hills", "hills", "3f6a4a", 10, 24, 21);
            Layer("hills_dark", "Dark Hills", "hills", "24303a", 10, 24, 22);
            Layer("castle_mid", "Castle Skyline", "castle", "4a4460", 12, 28, 31);
            Layer("castle_ruins", "Ruined Keep", "castle", "3a4a44", 12, 28, 32);
            Layer("forest_near", "Forest Edge", "forest", "2c4a34", 9, 20, 41);
            Layer("forest_dark", "Haunted Wood", "forest", "1c2420", 9, 20, 42);
            Layer("pillars_near", "Stone Pillars", "pillars", "5a5470", 12, 20, 51);
            Layer("dungeon_walls", "Dungeon Walls", "pillars", "2a2630", 12, 20, 52);
            Layer("cave_far", "Cavern Depths", "cave", "2a1f3a", 14, 32, 61);
            Layer("crystal_mid", "Crystal Growth", "crystals", "5a3a8a", 10, 24, 62);
            Layer("dunes_far", "Dunes", "hills", "c9a462", 10, 32, 71);
            Layer("desert_ruins", "Desert Ruins", "castle", "a67f45", 12, 28, 72);
            Layer("clouds_far", "High Clouds", "clouds", "ffffff", 14, 36, 81);
            Layer("clouds_near", "Low Clouds", "clouds", "e8ecf8", 8, 24, 82);
            Layer("volcano_far", "Volcanic Ridge", "mountains", "4a1f1a", 14, 32, 91);
            Layer("lava_mid", "Lava Flows", "hills", "7a2a1a", 10, 24, 92);
            Layer("swamp_trees", "Swamp Trees", "forest", "2a3a2a", 9, 20, 101);

            // ---- background themes (far, mid, near) -----------------------
            Theme("castle", "Castle Realm", "mountains_far", "castle_mid", "forest_near", "1e2447", "5a4f8a");
            Theme("forest", "Enchanted Forest", "mountains_far", "hills_mid", "forest_near", "233a55", "6f9a7a");
            Theme("dungeon", "Dungeon", "cave_far", "dungeon_walls", "pillars_near", "14111c", "2a2438");
            Theme("cavern", "Crystal Cavern", "cave_far", "crystal_mid", "pillars_near", "1a1030", "3a2060");
            Theme("desert", "Desert Kingdom", "dunes_far", "desert_ruins", "clouds_near", "f0c070", "f8e0b0");
            Theme("sky", "Sky Realm", "clouds_far", "mountains_snow", "clouds_near", "4a8ad8", "a8d0f8");
            Theme("volcano", "Dragon's Peak", "volcano_far", "lava_mid", "pillars_near", "2a0a0a", "6a2a1a");
            Theme("frost", "Frozen North", "mountains_snow", "hills_dark", "forest_dark", "9fb4d8", "e0eaf8");
            Theme("swamp", "Witch Marsh", "hills_dark", "swamp_trees", "forest_dark", "1a2a20", "3a4a30");
            Theme("haunted", "Haunted Wood", "mountains_far", "castle_ruins", "forest_dark", "101018", "2a2038");

            // ---- ground themes ----------------------------------------------
            Ground("stone", "Castle Stone", "8f8c94", "5c5963", "ffffff", "bricks");
            Ground("dark_stone", "Dark Keep", "4d4a5a", "2e2c38", "b0b0ff", "bricks");
            Ground("grass", "Grassland", "5fa54a", "5a3a1a", "ffffff", "grass");
            Ground("wood", "Tavern Floor", "a9743d", "7a5028", "ffe0a0", "planks");
            Ground("sand", "Desert Sand", "e0c080", "b3904f", "ffffff", "sand");
            Ground("ice", "Frozen Lake", "c0e8ff", "6fb2d8", "ffffff", "ice");
            Ground("lava", "Lava Rock", "6b3a2f", "3d1d16", "ff9a3a", "lava");
            Ground("crystal", "Crystal Bed", "b48cf0", "7d55c8", "ffffff", "crystal");
            Ground("swamp", "Bog", "5a6a3b", "3b4726", "a0ff80", "swamp");
            Ground("cloud", "Cloud Floor", "f4f6ff", "c5cdea", "ffffff", "cloud");
        }

        static void Layer(string id, string name, string style, string silhouette, float height, float width, int seed)
        {
            Layers.Add(new BackgroundLayerDefinition
            {
                id = id, name = name, style = style, silhouette = ObjectCatalog.Hex(silhouette),
                heightBlocks = height, widthBlocks = width, seed = seed
            });
        }

        static void Theme(string id, string name, string far, string mid, string near, string skyTop, string skyBottom)
        {
            Backgrounds.Add(new BackgroundTheme
            {
                id = id, name = name, far = far, mid = mid, near = near,
                skyTop = ObjectCatalog.Hex(skyTop), skyBottom = ObjectCatalog.Hex(skyBottom)
            });
        }

        static void Ground(string id, string name, string top, string body, string line, string style)
        {
            Grounds.Add(new GroundTheme
            {
                id = id, name = name, top = ObjectCatalog.Hex(top), body = ObjectCatalog.Hex(body), line = ObjectCatalog.Hex(line), style = style
            });
        }

        public static BackgroundTheme GetBackground(string id)
        {
            foreach (var t in Backgrounds) if (t.id == id) return t;
            return Backgrounds[0];
        }

        public static BackgroundLayerDefinition GetLayer(string id)
        {
            foreach (var l in Layers) if (l.id == id) return l;
            return Layers[0];
        }

        public static GroundTheme GetGround(string id)
        {
            foreach (var g in Grounds) if (g.id == id) return g;
            return Grounds[0];
        }

        public static string[] BackgroundIds
        {
            get
            {
                var ids = new string[Backgrounds.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = Backgrounds[i].id;
                return ids;
            }
        }

        public static string[] LayerIds
        {
            get
            {
                var ids = new string[Layers.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = Layers[i].id;
                return ids;
            }
        }

        public static string[] GroundIds
        {
            get
            {
                var ids = new string[Grounds.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = Grounds[i].id;
                return ids;
            }
        }
    }
}
