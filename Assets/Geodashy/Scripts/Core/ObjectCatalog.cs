using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// The complete list of placeable object types. Art is looked up by id
    /// (Resources/Sprites/{id}.png) and falls back to a procedural placeholder.
    /// </summary>
    public static class ObjectCatalog
    {
        public const string CatBlocks = "Blocks";
        public const string CatSlopes = "Slopes";
        public const string CatHazards = "Hazards";
        public const string CatRunes = "Runes";
        public const string CatPads = "Pads";
        public const string CatPortals = "Portals";
        public const string CatCollectibles = "Loot";
        public const string CatDecor = "Decor";
        public const string CatTriggers = "Triggers";
        public const string CatSpecial = "Special";

        public static readonly string[] Categories =
        {
            CatBlocks, CatSlopes, CatHazards, CatRunes, CatPads, CatPortals, CatCollectibles, CatDecor, CatTriggers, CatSpecial
        };

        static readonly List<ObjectDefinition> all = new List<ObjectDefinition>();
        static readonly Dictionary<string, ObjectDefinition> byId = new Dictionary<string, ObjectDefinition>(StringComparer.OrdinalIgnoreCase);
        static bool built;

        public static IReadOnlyList<ObjectDefinition> All
        {
            get
            {
                EnsureBuilt();
                return all;
            }
        }

        public static ObjectDefinition Get(string id)
        {
            EnsureBuilt();
            if (string.IsNullOrEmpty(id)) return null;
            return byId.TryGetValue(id, out var d) ? d : null;
        }

        public static List<ObjectDefinition> InCategory(string category)
        {
            EnsureBuilt();
            var list = new List<ObjectDefinition>();
            foreach (var d in all) if (d.category == category) list.Add(d);
            return list;
        }

        public static List<ObjectDefinition> Search(string query)
        {
            EnsureBuilt();
            var list = new List<ObjectDefinition>();
            if (string.IsNullOrWhiteSpace(query)) return list;
            var q = query.Trim().ToLowerInvariant();
            foreach (var d in all)
            {
                if (d.name.ToLowerInvariant().Contains(q) || d.id.Contains(q) || d.category.ToLowerInvariant().Contains(q))
                {
                    list.Add(d);
                    continue;
                }
                foreach (var t in d.tags)
                {
                    if (t.Contains(q))
                    {
                        list.Add(d);
                        break;
                    }
                }
            }
            return list;
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c) ? c : Color.magenta;
        }

        static ObjectDefinition Add(string id, string name, string category, ObjectKind kind)
        {
            var d = new ObjectDefinition { id = id, name = name, category = category, kind = kind };
            all.Add(d);
            byId[id] = d;
            return d;
        }

        static void EnsureBuilt()
        {
            if (built) return;
            built = true;
            BuildBlocks();
            BuildSlopes();
            BuildHazards();
            BuildRunes();
            BuildPads();
            BuildPortals();
            BuildCollectibles();
            BuildDecor();
            BuildTriggers();
            BuildSpecial();
            foreach (var d in all)
            {
                if (!string.IsNullOrEmpty(d.deathVerb)) continue;
                d.deathVerb = d.IsSolidLike ? "Dashed against" : "Struck by";
            }
        }

        // ---------------------------------------------------------------------

        static void BuildBlocks()
        {
            // Family helper: full block, half block, wide, tall, outline
            void Family(string id, string name, PlaceholderShape shape, string c1, string c2, params string[] tags)
            {
                Add(id, name, CatBlocks, ObjectKind.Solid).Shape(shape).Colors(c1, c2).Z(-1).Tags(tags);
                Add(id + "_half", name + " Half", CatBlocks, ObjectKind.Solid).Size(1, 0.5f).Shape(PlaceholderShape.HalfBlock).Colors(c1, c2).Z(-1).Tags(tags);
                Add(id + "_wide", name + " 2x1", CatBlocks, ObjectKind.Solid).Size(2, 1).Shape(shape).Colors(c1, c2).Z(-1).Tags(tags);
                Add(id + "_tall", name + " 1x2", CatBlocks, ObjectKind.Solid).Size(1, 2).Shape(shape).Colors(c1, c2).Z(-1).Tags(tags);
                Add(id + "_big", name + " 2x2", CatBlocks, ObjectKind.Solid).Size(2, 2).Shape(shape).Colors(c1, c2).Z(-1).Tags(tags);
            }

            Family("castle_stone", "Castle Stone", PlaceholderShape.Bricks, "8f8c94", "5c5963", "castle", "stone", "brick");
            Family("castle_dark", "Dark Keep Stone", PlaceholderShape.Bricks, "4d4a5a", "2e2c38", "castle", "dungeon", "stone");
            Family("cobble", "Cobblestone", PlaceholderShape.Block, "9a9086", "6c6359", "village", "stone");
            Family("wood_plank", "Oak Planks", PlaceholderShape.Planks, "a9743d", "7a5028", "wood", "village", "tavern");
            Family("dark_wood", "Dark Oak", PlaceholderShape.Planks, "5f3f23", "3c2714", "wood", "dungeon");
            Family("moss_stone", "Mossy Stone", PlaceholderShape.Bricks, "6f8a63", "465c3e", "forest", "ruins", "stone");
            Family("ice_block", "Frost Block", PlaceholderShape.Block, "a9dcf5", "6fb2d8", "ice", "frost", "winter");
            Family("sand_block", "Desert Sandstone", PlaceholderShape.Bricks, "d9b97c", "b3904f", "desert", "sand");
            Family("crystal_block", "Crystal Block", PlaceholderShape.Block, "b48cf0", "7d55c8", "crystal", "cave", "magic");
            Family("obsidian", "Obsidian", PlaceholderShape.Block, "2a2333", "15111c", "volcano", "dark");
            Family("gold_brick", "Gilded Brick", PlaceholderShape.Bricks, "e0b64a", "a77f22", "gold", "treasure", "royal");
            Family("lava_rock", "Lava Rock", PlaceholderShape.Bricks, "6b3a2f", "3d1d16", "volcano", "fire");
            Family("marble", "Marble", PlaceholderShape.Block, "e8e4dc", "b9b3a8", "temple", "royal");
            Family("swamp_log", "Swamp Log", PlaceholderShape.Planks, "5a6a3b", "3b4726", "swamp", "wood");
            Family("cloud_block", "Sky Cloud", PlaceholderShape.Block, "f4f6ff", "c5cdea", "sky", "cloud");

            Add("outline_block", "Outline Block", CatBlocks, ObjectKind.Solid).Shape(PlaceholderShape.BlockOutline).Colors("ffffff", "000000").Z(-1).Tags("outline", "neon", "line");
            Add("outline_block_wide", "Outline Block 2x1", CatBlocks, ObjectKind.Solid).Size(2, 1).Shape(PlaceholderShape.BlockOutline).Colors("ffffff", "000000").Z(-1).Tags("outline");
            Add("outline_block_big", "Outline Block 2x2", CatBlocks, ObjectKind.Solid).Size(2, 2).Shape(PlaceholderShape.BlockOutline).Colors("ffffff", "000000").Z(-1).Tags("outline");
            Add("invisible_block", "Invisible Block", CatBlocks, ObjectKind.Solid).Shape(PlaceholderShape.BlockOutline).Colors("00000000", "ff00ff").Z(-1).Tags("invisible", "hidden")
                .Desc("Solid in play but never drawn. Shown as a magenta outline in the editor.");
            Add("platform_thin", "Stone Ledge", CatBlocks, ObjectKind.Solid).Size(2, 0.25f).Shape(PlaceholderShape.HalfBlock).Colors("8f8c94", "5c5963").Z(-1).Tags("platform", "ledge");
            Add("platform_wood", "Wooden Ledge", CatBlocks, ObjectKind.Solid).Size(3, 0.25f).Shape(PlaceholderShape.HalfBlock).Colors("a9743d", "7a5028").Z(-1).Tags("platform", "ledge");
        }

        static void BuildSlopes()
        {
            void Slopes(string id, string name, string c1, string c2, params string[] tags)
            {
                Add(id + "_slope45", name + " Slope 45", CatSlopes, ObjectKind.Slope).Shape(PlaceholderShape.Slope45).Collider(ColliderShape.SlopeUp).Colors(c1, c2).Z(-1).Tags(tags);
                Add(id + "_slope22", name + " Slope 22", CatSlopes, ObjectKind.Slope).Size(2, 1).Shape(PlaceholderShape.Slope22).Collider(ColliderShape.SlopeUp).Colors(c1, c2).Z(-1).Tags(tags);
            }
            Slopes("castle_stone", "Castle Stone", "8f8c94", "5c5963", "castle", "stone");
            Slopes("castle_dark", "Dark Keep", "4d4a5a", "2e2c38", "castle", "dungeon");
            Slopes("wood_plank", "Oak", "a9743d", "7a5028", "wood");
            Slopes("moss_stone", "Mossy", "6f8a63", "465c3e", "forest");
            Slopes("ice_block", "Frost", "a9dcf5", "6fb2d8", "ice");
            Slopes("sand_block", "Sandstone", "d9b97c", "b3904f", "desert");
            Slopes("crystal_block", "Crystal", "b48cf0", "7d55c8", "crystal");
            Slopes("obsidian", "Obsidian", "2a2333", "15111c", "volcano");
            Slopes("outline", "Outline", "ffffff", "000000", "outline");
        }

        static void BuildHazards()
        {
            Add("iron_spike", "Iron Spike", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Spike).Collider(ColliderShape.Triangle, 0.55f).Colors("c9ccd6", "5f6270").Z(1).Tags("spike", "iron", "castle").Death("Impaled by");
            Add("iron_spike_small", "Small Iron Spike", CatHazards, ObjectKind.Hazard).Size(1, 0.5f).Shape(PlaceholderShape.SpikeSmall).Collider(ColliderShape.Triangle, 0.55f).Colors("c9ccd6", "5f6270").Z(1).Tags("spike", "small").Death("Impaled by");
            Add("iron_spike_wide", "Pike Wall", CatHazards, ObjectKind.Hazard).Size(3, 1).Shape(PlaceholderShape.SpikeWide).Collider(ColliderShape.Triangle, 0.6f).Colors("c9ccd6", "5f6270").Z(1).Tags("spike", "pike", "wall").Death("Impaled on");
            Add("lance", "Lance", CatHazards, ObjectKind.Hazard).Size(1, 2).Shape(PlaceholderShape.Lance).Collider(ColliderShape.Triangle, 0.5f).Colors("e2d7a5", "8f8055").Z(1).Tags("spike", "tall", "joust").Death("Run through by");
            Add("wood_stake", "Wooden Stake", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Spike).Collider(ColliderShape.Triangle, 0.55f).Colors("a9743d", "5c3a1a").Z(1).Tags("spike", "wood", "forest").Death("Impaled by");
            Add("ice_spike", "Ice Shard", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Spike).Collider(ColliderShape.Triangle, 0.55f).Colors("d5f1ff", "7fc0e6").Z(1).Tags("spike", "ice").Death("Pierced by");
            Add("crystal_spike", "Crystal Shard", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Spike).Collider(ColliderShape.Triangle, 0.55f).Colors("d3b8ff", "8a5ee0").Z(1).Tags("spike", "crystal").Death("Pierced by");
            Add("bone_spike", "Bone Spike", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Spike).Collider(ColliderShape.Triangle, 0.55f).Colors("f0e9d2", "b8ad8c").Z(1).Tags("spike", "bone", "dungeon").Death("Impaled by");
            Add("thorns", "Thorn Bush", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Thorns).Collider(ColliderShape.Circle, 0.7f).Colors("3f6b2f", "204018").Z(1).Tags("bush", "forest", "thorn").Death("Torn apart by");
            Add("fire_pit", "Fire Pit", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Fire).Collider(ColliderShape.Box, 0.6f).Colors("ff8a2a", "ffd23a").Z(1).Tags("fire", "flame", "volcano").Death("Burned in");
            Add("fire_pit_wide", "Fire Trench", CatHazards, ObjectKind.Hazard).Size(3, 1).Shape(PlaceholderShape.Fire).Collider(ColliderShape.Box, 0.6f).Colors("ff8a2a", "ffd23a").Z(1).Tags("fire", "flame").Death("Burned in");
            Add("poison_bog", "Poison Bog", CatHazards, ObjectKind.Hazard).Size(2, 0.5f).Shape(PlaceholderShape.Bog).Collider(ColliderShape.Box, 0.7f).Colors("6fbf3a", "3b6d1e").Z(1).Tags("swamp", "poison").Death("Drowned in");
            Add("saw_blade", "Spinning Blade", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Saw).Collider(ColliderShape.Circle, 0.8f).Colors("d0d3dc", "6a6d78").Z(1).Tags("saw", "blade", "spin").Spins().Death("Shredded by");
            Add("saw_blade_big", "Great Blade", CatHazards, ObjectKind.Hazard).Size(2, 2).Shape(PlaceholderShape.Saw).Collider(ColliderShape.Circle, 0.8f).Colors("d0d3dc", "6a6d78").Z(1).Tags("saw", "blade", "spin").Spins().Death("Shredded by");
            Add("saw_blade_huge", "Colossal Blade", CatHazards, ObjectKind.Hazard).Size(3, 3).Shape(PlaceholderShape.Saw).Collider(ColliderShape.Circle, 0.8f).Colors("d0d3dc", "6a6d78").Z(1).Tags("saw", "blade", "spin").Spins().Death("Cleaved by");
            Add("mace", "Chain Mace", CatHazards, ObjectKind.Hazard).Shape(PlaceholderShape.Mace).Collider(ColliderShape.Circle, 0.75f).Colors("6e7180", "3b3d47").Z(1).Tags("mace", "spin", "castle").Spins().Death("Crushed by");
            Add("mace_big", "Great Mace", CatHazards, ObjectKind.Hazard).Size(2, 2).Shape(PlaceholderShape.Mace).Collider(ColliderShape.Circle, 0.75f).Colors("6e7180", "3b3d47").Z(1).Tags("mace", "spin").Spins().Death("Flattened by");
            Add("lightning_rune", "Lightning Rune", CatHazards, ObjectKind.Hazard).Size(1, 3).Shape(PlaceholderShape.Lightning).Collider(ColliderShape.Box, 0.5f).Colors("f5f0a0", "7fd0ff").Z(1).Tags("magic", "storm", "beam").Death("Smitten by");
            Add("stalactite_hazard", "Stalactite", CatHazards, ObjectKind.Hazard).Size(1, 2).Shape(PlaceholderShape.Stalactite).Collider(ColliderShape.Triangle, 0.5f).Colors("8e8a9a", "4c485a").Z(1).Tags("cave", "spike")
                .Desc("A hanging spike. Place it flipped vertically under a ceiling.").Death("Impaled by");
        }

        static void BuildRunes()
        {
            ObjectDefinition Orb(string id, string name, OrbType type, string c1, string c2, string desc)
            {
                var d = Add(id, name, CatRunes, ObjectKind.Orb).Shape(PlaceholderShape.Orb).Collider(ColliderShape.Ring, 1.3f).Colors(c1, c2).Z(1).Tags("orb", "rune", "click").Desc(desc);
                d.orbType = type;
                return d;
            }
            Orb("rune_wind", "Wind Rune", OrbType.Jump, "ffd83a", "8a6f00", "Click while touching: normal jump.");
            Orb("rune_feather", "Feather Rune", OrbType.SmallJump, "ff7fd1", "8a2f6a", "Click while touching: small hop.");
            Orb("rune_fire", "Fire Rune", OrbType.BigJump, "ff4a3a", "7a1a10", "Click while touching: big leap.");
            Orb("rune_gravity", "Gravity Rune", OrbType.GravityFlip, "3fa8ff", "0f3f7a", "Click while touching: flips gravity.");
            Orb("rune_storm", "Storm Rune", OrbType.GravityJump, "4fe06a", "146a2a", "Click while touching: flips gravity and jumps.");
            Orb("rune_void", "Void Rune", OrbType.Slam, "2b2b33", "9a9ab0", "Click while touching: slams you toward the ground.");
            Orb("rune_dash", "Dash Rune", OrbType.Dash, "5ff2d6", "12705f", "Click while touching: dash forward until you release.");
            Orb("rune_dash_gravity", "Dash Gravity Rune", OrbType.DashFlip, "7bff5f", "2a6a12", "Click while touching: gravity flipped dash.");
            Orb("rune_shadow", "Shadow Rune", OrbType.Teleport, "6a3cff", "2a1470", "Click while touching: snap to the opposite surface.");
            Add("rune_trigger", "Trigger Rune", CatRunes, ObjectKind.Orb).Shape(PlaceholderShape.Orb).Collider(ColliderShape.Ring, 1.3f).Colors("ffffff", "444444").Z(1).Tags("orb", "spawn")
                .Desc("Click while touching: spawns the target group.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("multi", "Multi Activate", PropType.Bool, "0"));
            byId["rune_trigger"].orbType = OrbType.None;
        }

        static void BuildPads()
        {
            ObjectDefinition Pad(string id, string name, PadType type, string c1, string c2, string desc)
            {
                var d = Add(id, name, CatPads, ObjectKind.Pad).Size(1, 0.4f).Shape(PlaceholderShape.Pad).Collider(ColliderShape.Box, 1f).Colors(c1, c2).Z(1).Tags("pad", "shroom", "spring").Desc(desc);
                d.padType = type;
                return d;
            }
            Pad("shroom_spring", "Spring Shroom", PadType.Jump, "ffd83a", "8a6f00", "Automatic normal jump on touch.");
            Pad("shroom_small", "Puff Shroom", PadType.SmallJump, "ff7fd1", "8a2f6a", "Automatic small hop on touch.");
            Pad("shroom_great", "Great Shroom", PadType.BigJump, "ff4a3a", "7a1a10", "Automatic big leap on touch.");
            Pad("lily_gravity", "Gravity Lily", PadType.GravityFlip, "3fa8ff", "0f3f7a", "Flips gravity on touch.");
            Pad("shadow_pad", "Shadow Pad", PadType.Teleport, "6a3cff", "2a1470", "Snaps you to the opposite surface on touch.");
        }

        static void BuildPortals()
        {
            ObjectDefinition Portal(string id, string name, PortalType type, string c1, string c2, string desc)
            {
                var d = Add(id, name, CatPortals, ObjectKind.Portal).Size(1, 3).Shape(PlaceholderShape.Portal).Collider(ColliderShape.Box, 0.9f).Colors(c1, c2).Z(1).Tags("portal", "gate").Desc(desc);
                d.portalType = type;
                return d;
            }
            foreach (var m in MountCatalog.All)
            {
                var d = Portal("portal_" + m.id, m.name + " Gate", PortalType.Mount, m.colorHex, m.accentHex, "Switch to the " + m.name + ". " + m.control);
                d.portalMount = m.id;
                d.tags = new[] { "portal", "mount", m.id, m.name.ToLowerInvariant() };
            }
            Portal("portal_gravity_normal", "Upright Gate", PortalType.GravityNormal, "fff08a", "9a8a20", "Sets gravity to normal.");
            Portal("portal_gravity_flip", "Inversion Gate", PortalType.GravityFlip, "3fa8ff", "0f3f7a", "Flips gravity.");
            string[] speedNames = { "Crawl", "Trot", "Canter", "Gallop", "Charge" };
            string[] speedColors = { "ff9a3a", "6ee0ff", "5fff7a", "ff5fd0", "ff4a4a" };
            for (int i = 0; i < 5; i++)
            {
                var d = Portal("portal_speed_" + i, speedNames[i] + " Rune (" + MountCatalog.SpeedLabels[i] + ")", PortalType.Speed, speedColors[i], "333333", "Sets the travel speed to " + MountCatalog.SpeedLabels[i] + ".");
                d.Size(1, 1.5f).Shape(PlaceholderShape.Rune);
                d.portalSpeed = (SpeedTier)i;
                d.tags = new[] { "speed", "portal", MountCatalog.SpeedLabels[i] };
            }
            Portal("portal_size_normal", "Growth Gate", PortalType.SizeNormal, "8fe07a", "2f6a20", "Returns the mount to normal size.");
            Portal("portal_size_mini", "Shrink Gate", PortalType.SizeMini, "ff8ad2", "7a2060", "Shrinks the mount (mini mode).");
            Portal("portal_mirror_on", "Mirror Gate", PortalType.MirrorOn, "c0f0ff", "3a7a90", "Mirrors the screen horizontally.");
            Portal("portal_mirror_off", "Unmirror Gate", PortalType.MirrorOff, "e0f8ff", "3a7a90", "Restores normal screen orientation.");
            Portal("portal_dual_on", "Twin Gate", PortalType.DualOn, "ffd0a0", "a06020", "Spawns a second, mirrored rider.");
            Portal("portal_dual_off", "Single Gate", PortalType.DualOff, "ffe8d0", "a06020", "Removes the second rider.");
            Portal("portal_teleport", "Waygate", PortalType.Teleport, "b070ff", "402080", "Teleports the rider vertically by Exit Offset.")
                .Prop(new PropDef("exitY", "Exit Offset Y", PropType.Float, "3").Range(-30, 30, 0.5f));
        }

        static void BuildCollectibles()
        {
            Add("gold_coin", "Gold Coin", CatCollectibles, ObjectKind.Collectible).Shape(PlaceholderShape.Coin).Collider(ColliderShape.Circle, 1.2f).Colors("ffd23a", "9a7a10").Z(2).Tags("coin", "secret", "gold");
            Add("gem", "Gem", CatCollectibles, ObjectKind.Collectible).Shape(PlaceholderShape.Gem).Collider(ColliderShape.Circle, 1.2f).Colors("6ad4ff", "1f6a90").Z(2).Tags("gem", "user coin", "crystal");
            Add("key", "Dungeon Key", CatCollectibles, ObjectKind.Collectible).Shape(PlaceholderShape.Key).Collider(ColliderShape.Circle, 1.2f).Colors("e8c460", "7a6020").Z(2).Tags("key", "item")
                .Desc("Opens every Locked Gate with the same Key ID. Also feeds Count triggers.")
                .Prop(new PropDef("itemId", "Key ID", PropType.Int, "1").Range(1, 999, 1));
            Add("locked_gate", "Locked Gate", CatCollectibles, ObjectKind.Solid).Size(2, 3).Shape(PlaceholderShape.Gate).Colors("4a4c56", "8a8d99").Z(-1).Tags("gate", "lock", "key", "door", "castle")
                .Desc("Solid until a Dungeon Key with the matching Key ID is collected, then it opens. Use it for shortcuts and secret routes.")
                .Death("Dashed against")
                .Prop(new PropDef("keyId", "Key ID", PropType.Int, "1").Range(1, 999, 1));
        }

        static void BuildDecor()
        {
            void Deco(string id, string name, PlaceholderShape shape, float w, float h, string c1, string c2, params string[] tags)
            {
                Add(id, name, CatDecor, ObjectKind.Decoration).Size(w, h).Shape(shape).Collider(ColliderShape.None).Colors(c1, c2).Z(2).Tags(tags);
            }
            Deco("banner_red", "Red Banner", PlaceholderShape.Banner, 1, 2, "b8302c", "e0b64a", "castle", "flag", "royal");
            Deco("banner_blue", "Blue Banner", PlaceholderShape.Banner, 1, 2, "2c4fb8", "e0b64a", "castle", "flag", "royal");
            Deco("banner_green", "Green Banner", PlaceholderShape.Banner, 1, 2, "2f8a3a", "e0b64a", "castle", "flag", "forest");
            Deco("torch", "Wall Torch", PlaceholderShape.Torch, 0.5f, 1, "ffb03a", "6a4a2a", "light", "fire", "castle", "dungeon");
            Deco("chain", "Iron Chain", PlaceholderShape.Chain, 0.5f, 2, "8a8d99", "4a4c56", "dungeon", "metal");
            Deco("skull", "Skull", PlaceholderShape.Skull, 0.75f, 0.75f, "f0e9d2", "6a6350", "bone", "dungeon", "cave");
            Deco("barrel", "Barrel", PlaceholderShape.Barrel, 1, 1, "8a5a2a", "5a3a1a", "tavern", "wood", "village");
            Deco("crate", "Crate", PlaceholderShape.Crate, 1, 1, "a9743d", "6a4020", "wood", "village");
            Deco("tree_oak", "Oak Tree", PlaceholderShape.Tree, 3, 4, "3f8a3a", "6a4a2a", "forest", "nature");
            Deco("tree_pine", "Pine Tree", PlaceholderShape.Tree, 2, 4, "2a6a3a", "5a3a1a", "forest", "winter", "nature");
            Deco("tree_dead", "Dead Tree", PlaceholderShape.Tree, 2, 3, "6a6a6a", "3a3a3a", "swamp", "spooky", "nature");
            Deco("bush", "Bush", PlaceholderShape.Bush, 1.5f, 1, "4a9a3a", "2a6a2a", "forest", "nature");
            Deco("cloud_small", "Small Cloud", PlaceholderShape.Cloud, 2, 1, "ffffff", "d0d8f0", "sky", "cloud");
            Deco("cloud_big", "Big Cloud", PlaceholderShape.Cloud, 4, 1.5f, "ffffff", "d0d8f0", "sky", "cloud");
            Deco("window", "Castle Window", PlaceholderShape.Window, 1, 1.5f, "3a2a5a", "9a9aa8", "castle", "glass");
            Deco("window_stained", "Stained Glass", PlaceholderShape.Window, 1, 2, "b83a5a", "e0b64a", "castle", "church", "glass");
            Deco("arch", "Stone Arch", PlaceholderShape.Arch, 3, 3, "8f8c94", "5c5963", "castle", "stone");
            Deco("pillar", "Marble Pillar", PlaceholderShape.Pillar, 1, 3, "e8e4dc", "b9b3a8", "temple", "column");
            Deco("crystal_cluster", "Crystal Cluster", PlaceholderShape.Crystal, 1.5f, 1.5f, "c8a0ff", "7d55c8", "cave", "crystal", "magic");
            Deco("rune_stone", "Rune Stone", PlaceholderShape.Rune, 1, 1.5f, "7a8a9a", "6ad4ff", "magic", "stone");
            Deco("statue_knight", "Knight Statue", PlaceholderShape.Statue, 1, 2, "9a9aa8", "5a5a68", "castle", "stone", "knight");
            Deco("vine", "Hanging Vine", PlaceholderShape.Vine, 0.5f, 2, "3f8a3a", "2a5a2a", "forest", "jungle", "ruins");
            Deco("mushroom_red", "Red Mushroom", PlaceholderShape.Mushroom, 1, 1, "d83a3a", "f0e0c0", "forest", "swamp");
            Deco("mushroom_glow", "Glow Mushroom", PlaceholderShape.Mushroom, 1, 1, "6ad4ff", "d0f0ff", "cave", "magic");
            Deco("stalactite", "Stalactite Decor", PlaceholderShape.Stalactite, 1, 2, "8e8a9a", "4c485a", "cave");
            Deco("flag_pole", "Flag Pole", PlaceholderShape.Flag, 1, 3, "b8302c", "6a4a2a", "castle", "flag");
            Deco("gate_deco", "Portcullis", PlaceholderShape.Gate, 2, 3, "4a4c56", "8a8d99", "castle", "gate");
            Deco("bricks_deco", "Brick Facade", PlaceholderShape.Bricks, 2, 2, "8f8c94", "5c5963", "castle", "wall", "background");
            Deco("planks_deco", "Plank Facade", PlaceholderShape.Planks, 2, 2, "a9743d", "7a5028", "wood", "wall", "background");
            Deco("cobweb", "Cobweb", PlaceholderShape.Vine, 1, 1, "e0e0e0", "9a9a9a", "dungeon", "spooky");
        }

        static void BuildTriggers()
        {
            ObjectDefinition Trig(string id, string name, TriggerType type, string code, string color, string desc)
            {
                var d = Add(id, name, CatTriggers, ObjectKind.Trigger).Shape(PlaceholderShape.Trigger).Collider(ColliderShape.None).Colors(color, "ffffff").Z(4).Tags("trigger", type.ToString().ToLowerInvariant()).Code(code).Desc(desc);
                d.triggerType = type;
                d.Prop(new PropDef("touch", "Touch Triggered", PropType.Bool, "0").Help("Fires when the player touches it instead of passing its x."));
                d.Prop(new PropDef("spawn", "Spawn Triggered", PropType.Bool, "0").Help("Only fires when spawned by a Spawn trigger / Trigger Rune."));
                d.Prop(new PropDef("multi", "Multi Trigger", PropType.Bool, "0").Help("Can fire more than once."));
                return d;
            }
            var easing = new PropDef("easing", "Easing", PropType.Easing, "Linear");

            Trig("trig_move", "Move", TriggerType.Move, "MV", "4fa3ff", "Moves a group by an offset over time.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("moveX", "Move X", PropType.Float, "0").Range(-500, 500, 0.5f))
                .Prop(new PropDef("moveY", "Move Y", PropType.Float, "0").Range(-500, 500, 0.5f))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "0.5").Range(0, 60, 0.1f))
                .Prop(easing)
                .Prop(new PropDef("lockX", "Lock to Player X", PropType.Bool, "0"))
                .Prop(new PropDef("lockY", "Lock to Player Y", PropType.Bool, "0"));
            Trig("trig_rotate", "Rotate", TriggerType.Rotate, "RT", "ff9a3a", "Rotates a group around a centre group.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("center", "Center Group", PropType.Group, "0").Help("0 = rotate each object around itself."))
                .Prop(new PropDef("degrees", "Degrees", PropType.Float, "360").Range(-3600, 3600, 5))
                .Prop(new PropDef("times", "Times", PropType.Float, "1").Range(0, 100, 0.5f))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "1").Range(0, 60, 0.1f))
                .Prop(easing)
                .Prop(new PropDef("lockRot", "Lock Object Rotation", PropType.Bool, "0"));
            Trig("trig_color", "Color", TriggerType.Color, "CL", "ff5fd0", "Fades a colour channel to a new colour.")
                .Prop(new PropDef("channel", "Channel", PropType.Int, "1").Range(0, 1006, 1).Help("1..999 user channels, 1000 BG, 1001 Ground, 1002 Line, 1003 Obj."))
                .Prop(new PropDef("color", "Color", PropType.Color, "#FFFFFFFF"))
                .Prop(new PropDef("opacity", "Opacity", PropType.Float, "1").Range(0, 1, 0.05f))
                .Prop(new PropDef("duration", "Fade Time", PropType.Float, "0.5").Range(0, 60, 0.1f))
                .Prop(new PropDef("blending", "Blending", PropType.Bool, "0"));
            Trig("trig_alpha", "Alpha", TriggerType.Alpha, "AL", "b0b0ff", "Fades a group's opacity.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("opacity", "Opacity", PropType.Float, "0").Range(0, 1, 0.05f))
                .Prop(new PropDef("duration", "Fade Time", PropType.Float, "0.5").Range(0, 60, 0.1f));
            Trig("trig_toggle", "Toggle", TriggerType.Toggle, "TG", "5fe08a", "Enables or disables a group (disabled objects have no collision and are hidden).")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("activate", "Activate", PropType.Bool, "1"));
            Trig("trig_spawn", "Spawn", TriggerType.Spawn, "SP", "7fffd0", "Fires all spawn-triggered triggers in a group.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("delay", "Delay", PropType.Float, "0").Range(0, 60, 0.05f));
            Trig("trig_pulse", "Pulse", TriggerType.Pulse, "PL", "ff7a7a", "Flashes a group or channel with a colour.")
                .Prop(new PropDef("mode", "Mode", PropType.Enum, "Group").Values("Group", "Channel"))
                .Prop(new PropDef("target", "Target Group / Channel", PropType.Int, "0").Range(0, 1006, 1))
                .Prop(new PropDef("color", "Color", PropType.Color, "#FFFFFFFF"))
                .Prop(new PropDef("fadeIn", "Fade In", PropType.Float, "0.1").Range(0, 10, 0.05f))
                .Prop(new PropDef("hold", "Hold", PropType.Float, "0.2").Range(0, 10, 0.05f))
                .Prop(new PropDef("fadeOut", "Fade Out", PropType.Float, "0.3").Range(0, 10, 0.05f))
                .Prop(new PropDef("exclusive", "Exclusive", PropType.Bool, "0"));
            Trig("trig_shake", "Shake", TriggerType.Shake, "SH", "ffd23a", "Shakes the camera.")
                .Prop(new PropDef("strength", "Strength", PropType.Float, "0.3").Range(0, 5, 0.05f))
                .Prop(new PropDef("interval", "Interval", PropType.Float, "0.03").Range(0, 1, 0.01f))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "0.5").Range(0, 60, 0.1f));
            Trig("trig_follow", "Follow", TriggerType.Follow, "FL", "c0ff5f", "Makes a group copy another group's movement.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("follow", "Follow Group", PropType.Group, "0"))
                .Prop(new PropDef("xMod", "X Mod", PropType.Float, "1").Range(-10, 10, 0.1f))
                .Prop(new PropDef("yMod", "Y Mod", PropType.Float, "1").Range(-10, 10, 0.1f))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "1").Range(0, 600, 0.1f));
            Trig("trig_stop", "Stop", TriggerType.Stop, "ST", "ff4a4a", "Stops every running trigger that targets a group.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"));
            Trig("trig_zoom", "Camera Zoom", TriggerType.CameraZoom, "CZ", "d0d0d0", "Zooms the play camera.")
                .Prop(new PropDef("zoom", "Zoom", PropType.Float, "1").Range(0.25f, 4, 0.05f))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "1").Range(0, 60, 0.1f))
                .Prop(easing);
            Trig("trig_offset", "Camera Offset", TriggerType.CameraOffset, "CO", "d0d0d0", "Offsets the play camera from the player.")
                .Prop(new PropDef("offsetX", "Offset X", PropType.Float, "0").Range(-20, 20, 0.5f))
                .Prop(new PropDef("offsetY", "Offset Y", PropType.Float, "0").Range(-20, 20, 0.5f))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "1").Range(0, 60, 0.1f))
                .Prop(easing);
            Trig("trig_static", "Camera Static", TriggerType.CameraStatic, "CS", "d0d0d0", "Locks the camera onto a group's position (or frees it when Target is 0).")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("duration", "Duration", PropType.Float, "1").Range(0, 60, 0.1f))
                .Prop(new PropDef("followPlayer", "Follow Player Y", PropType.Bool, "0"));
            Trig("trig_bg", "Background Switch", TriggerType.BackgroundSwitch, "BG", "8ad0ff", "Changes the parallax background theme.")
                .Prop(new PropDef("theme", "Theme", PropType.Theme, "castle"))
                .Prop(new PropDef("duration", "Fade Time", PropType.Float, "1").Range(0, 30, 0.1f));
            Trig("trig_ground", "Ground Switch", TriggerType.GroundSwitch, "GR", "c0a080", "Changes the ground theme.")
                .Prop(new PropDef("theme", "Ground Theme", PropType.Enum, "stone").Values(ThemeCatalog.GroundIds));
            Trig("trig_hide", "Hide Player", TriggerType.HidePlayer, "HP", "808080", "Hides the rider (used for cutscenes).");
            Trig("trig_show", "Show Player", TriggerType.ShowPlayer, "SW", "b0b0b0", "Shows the rider again.");
            Trig("trig_touch", "Touch", TriggerType.Touch, "TC", "ffb0ff", "After passing it, every click spawns the target group.")
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("holdMode", "Hold Mode", PropType.Bool, "0").Help("Fires on release too."))
                .Prop(new PropDef("toggleMode", "Toggle Mode", PropType.Enum, "Spawn").Values("Spawn", "ToggleOn", "ToggleOff"));
            Trig("trig_random", "Random", TriggerType.Random, "RN", "ffe0a0", "Spawns one of two groups at random.")
                .Prop(new PropDef("target1", "Group A", PropType.Group, "0"))
                .Prop(new PropDef("target2", "Group B", PropType.Group, "0"))
                .Prop(new PropDef("chance", "Chance A (%)", PropType.Float, "50").Range(0, 100, 1));
            Trig("trig_song", "Song", TriggerType.Song, "SG", "a0ffe0", "Plays a song from Resources/Songs.")
                .Prop(new PropDef("song", "Song ID", PropType.Text, ""))
                .Prop(new PropDef("offset", "Offset (s)", PropType.Float, "0").Range(0, 6000, 0.1f))
                .Prop(new PropDef("loop", "Loop", PropType.Bool, "0"));
            Trig("trig_reverse", "Reverse", TriggerType.Reverse, "RV", "ff80ff", "Reverses the travel direction.");
            Trig("trig_count", "Count", TriggerType.Count, "CT", "e0e0a0", "Activates a group when an item count is reached.")
                .Prop(new PropDef("itemId", "Item ID", PropType.Int, "1").Range(1, 999, 1))
                .Prop(new PropDef("count", "Count", PropType.Int, "1").Range(0, 9999, 1))
                .Prop(new PropDef("target", "Target Group", PropType.Group, "0"))
                .Prop(new PropDef("activate", "Activate", PropType.Bool, "1"));
        }

        static void BuildSpecial()
        {
            Add("start_pos", "Start Position", CatSpecial, ObjectKind.StartPos).Shape(PlaceholderShape.StartMarker).Collider(ColliderShape.None).Colors("5fff7a", "1a6a2a").Z(4).Tags("start", "playtest", "spawn")
                .Desc("Playtesting starts from the right-most start position left of the camera. Overrides mount, speed and gravity.")
                .Prop(new PropDef("mount", "Mount", PropType.Mount, "horse"))
                .Prop(new PropDef("speed", "Speed", PropType.Speed, "1"))
                .Prop(new PropDef("flipped", "Gravity Flipped", PropType.Bool, "0"))
                .Prop(new PropDef("mini", "Mini", PropType.Bool, "0"))
                .Prop(new PropDef("disabled", "Disabled", PropType.Bool, "0"));
            Add("finish_gate", "Finish Gate", CatSpecial, ObjectKind.Finish).Size(2, 4).Shape(PlaceholderShape.Gate).Collider(ColliderShape.None).Colors("e0b64a", "6a4a2a").Z(2).Tags("finish", "end", "castle", "gate")
                .Desc("The level ends when the rider reaches this gate. Without one, the level ends after the last object.");
            Add("finish_flag", "Finish Banner", CatSpecial, ObjectKind.Finish).Size(1, 3).Shape(PlaceholderShape.Flag).Collider(ColliderShape.None).Colors("e0b64a", "b8302c").Z(2).Tags("finish", "end", "flag");
            Add("text", "Text", CatSpecial, ObjectKind.Text).Size(3, 1).Shape(PlaceholderShape.Text).Collider(ColliderShape.None).Colors("ffffff", "000000").Z(3).Tags("text", "sign", "label")
                .Prop(new PropDef("text", "Text", PropType.Text, "HUZZAH"))
                .Prop(new PropDef("size", "Size", PropType.Float, "1").Range(0.25f, 8, 0.25f));
            Add("checkpoint", "Waystone", CatSpecial, ObjectKind.Decoration).Size(1, 1.5f).Shape(PlaceholderShape.Rune).Collider(ColliderShape.None).Colors("5fff7a", "1a6a2a").Z(2).Tags("checkpoint", "practice")
                .Desc("Decorative waystone. Squire mode raises real ones where you place checkpoints.");
        }
    }
}
