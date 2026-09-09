using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Castle decoration shelves: stonework, ivy and plants, roofs and windows, furnishings, lights, yard and
    /// siege gear. Entries are generated from small tables (material × form) so the palette gets hundreds of
    /// distinct pieces while the art stays in a handful of parametrised routines (see DecorArt).
    /// </summary>
    public static partial class ObjectCatalog
    {
        struct MatInfo
        {
            public DecoMaterial m;
            public string id, name;
            public MatInfo(DecoMaterial m, string id, string name) { this.m = m; this.id = id; this.name = name; }
        }

        static readonly MatInfo[] StoneMats =
        {
            new MatInfo(DecoMaterial.Marble, "marble", "Marble"),
            new MatInfo(DecoMaterial.Slate, "slate", "Slate"),
            new MatInfo(DecoMaterial.Granite, "granite", "Granite"),
            new MatInfo(DecoMaterial.Sandstone, "sandstone", "Sandstone"),
            new MatInfo(DecoMaterial.Limestone, "limestone", "Limestone"),
            new MatInfo(DecoMaterial.Basalt, "basalt", "Basalt"),
            new MatInfo(DecoMaterial.Cobble, "cobble", "Cobblestone"),
            new MatInfo(DecoMaterial.MossyStone, "mossy", "Mossy Stone"),
        };

        static ObjectDefinition DecoItem(string id, string name, string category, PlaceholderShape shape, float w, float h, int variant, DecoMaterial material, string c1, string c2, params string[] tags)
        {
            return Add(id, name, category, ObjectKind.Decoration).Size(w, h).Shape(shape).Collider(ColliderShape.None).Colors(c1, c2).Z(2).Variant(variant).Material(material).Tags(tags);
        }

        static void BuildCastleDecor()
        {
            BuildStonework();
            BuildPlants();
            BuildRoofsAndWindows();
            BuildFurnishings();
            BuildLights();
            BuildYard();
        }

        // ---------------------------------------------------------------- stonework

        static void BuildStonework()
        {
            string[] stoneTags = { "castle", "stone", "wall", "background" };
            // wall facades: every stone material, plus plaster, brick, lead and iron plate, in three sizes
            var facades = new System.Collections.Generic.List<MatInfo>(StoneMats)
            {
                new MatInfo(DecoMaterial.Plaster, "plaster", "Plaster"),
                new MatInfo(DecoMaterial.Brick, "redbrick", "Red Brick"),
                new MatInfo(DecoMaterial.Lead, "lead", "Lead Plate"),
                new MatInfo(DecoMaterial.Iron, "ironplate", "Iron Plate"),
            };
            foreach (var f in facades)
            {
                DecoItem("facade_" + f.id, f.name + " Wall", CatStone, PlaceholderShape.Facade, 1, 1, 0, f.m, "8f8c94", "5c5963", stoneTags).Desc("A " + f.name.ToLowerInvariant() + " facing with no collision. Tile it behind the level for a castle interior or exterior.");
                DecoItem("facade_" + f.id + "_2", f.name + " Wall 2×2", CatStone, PlaceholderShape.Facade, 2, 2, 0, f.m, "8f8c94", "5c5963", stoneTags);
                DecoItem("facade_" + f.id + "_strip", f.name + " Course 3×1", CatStone, PlaceholderShape.Facade, 3, 1, 0, f.m, "8f8c94", "5c5963", stoneTags);
            }

            // columns
            string[] colNames = { "Plain", "Doric", "Ionic", "Corinthian", "Twisted" };
            string[] colIds = { "plain", "doric", "ionic", "corinthian", "twisted" };
            var colMats = new[] { StoneMats[0], StoneMats[2], StoneMats[3], StoneMats[1] };
            for (int c = 0; c < colNames.Length; c++)
            foreach (var m in colMats)
                DecoItem("column_" + colIds[c] + "_" + m.id, colNames[c] + " " + m.name + " Column", CatStone, PlaceholderShape.Column, 1, 3, c, m.m, "e8e4dc", "b9b3a8", "column", "pillar", "temple", "castle", "stone");
            foreach (var m in colMats)
            {
                DecoItem("column_broken_" + m.id, "Broken " + m.name + " Column", CatStone, PlaceholderShape.Column, 1, 2, 5, m.m, "e8e4dc", "b9b3a8", "column", "ruin", "stone");
                DecoItem("pilaster_" + m.id, m.name + " Pilaster", CatStone, PlaceholderShape.Column, 1, 3, 6, m.m, "e8e4dc", "b9b3a8", "column", "wall", "stone");
            }

            // arches
            string[] archNames = { "Round", "Gothic", "Keystone", "Ruined" };
            string[] archIds = { "round", "gothic", "keystone", "ruined" };
            var archMats = new[] { StoneMats[0], StoneMats[2], StoneMats[3], StoneMats[7] };
            for (int a = 0; a < archNames.Length; a++)
            foreach (var m in archMats)
                DecoItem("arch_" + archIds[a] + "_" + m.id, archNames[a] + " " + m.name + " Arch", CatStone, PlaceholderShape.ArchDeco, 3, 3, a, m.m, "8f8c94", "5c5963", "arch", "castle", "stone", "gate");

            // battlements
            var batMats = new[] { StoneMats[2], StoneMats[3], StoneMats[0], StoneMats[1], StoneMats[7] };
            foreach (var m in batMats)
            {
                DecoItem("merlon_" + m.id, m.name + " Merlon", CatStone, PlaceholderShape.Battlement, 1, 1, 0, m.m, "8f8c94", "5c5963", "battlement", "castle", "wall", "tower");
                DecoItem("crenel_" + m.id, m.name + " Crenellation 3×1", CatStone, PlaceholderShape.Battlement, 3, 1, 1, m.m, "8f8c94", "5c5963", "battlement", "castle", "wall", "tower");
                DecoItem("parapet_" + m.id, m.name + " Parapet", CatStone, PlaceholderShape.Battlement, 3, 0.5f, 4, m.m, "8f8c94", "5c5963", "battlement", "castle", "wall");
            }
            DecoItem("corbel_granite", "Granite Corbel", CatStone, PlaceholderShape.Battlement, 1, 0.5f, 2, DecoMaterial.Granite, "8f8c94", "5c5963", "bracket", "castle", "wall");
            DecoItem("corbel_sandstone", "Sandstone Corbel", CatStone, PlaceholderShape.Battlement, 1, 0.5f, 2, DecoMaterial.Sandstone, "8f8c94", "5c5963", "bracket", "castle", "wall");
            DecoItem("machicolation_granite", "Granite Machicolation", CatStone, PlaceholderShape.Battlement, 2, 1, 3, DecoMaterial.Granite, "8f8c94", "5c5963", "battlement", "castle", "tower");
            DecoItem("machicolation_sandstone", "Sandstone Machicolation", CatStone, PlaceholderShape.Battlement, 2, 1, 3, DecoMaterial.Sandstone, "8f8c94", "5c5963", "battlement", "castle", "tower");

            // carved reliefs
            string[] reliefNames = { "Shield", "Lion", "Gargoyle", "Grotesque", "Sun", "Fleur-de-lis", "Crossed Swords", "Crown" };
            string[] reliefIds = { "shield", "lion", "gargoyle", "grotesque", "sun", "fleur", "swords", "crown" };
            for (int i = 0; i < reliefNames.Length; i++)
            {
                DecoItem("relief_" + reliefIds[i] + "_marble", reliefNames[i] + " Relief (Marble)", CatStone, PlaceholderShape.Relief, 1, 1, i, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "relief", "carving", "heraldry", "castle", "stone");
                DecoItem("relief_" + reliefIds[i] + "_sandstone", reliefNames[i] + " Relief (Sandstone)", CatStone, PlaceholderShape.Relief, 1, 1, i, DecoMaterial.Sandstone, "d2b184", "8a6a40", "relief", "carving", "heraldry", "castle", "stone");
            }

            // monuments and garden stone
            DecoItem("obelisk_marble", "Marble Obelisk", CatStone, PlaceholderShape.Monument, 1, 4, 0, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "monument", "garden", "stone");
            DecoItem("obelisk_basalt", "Basalt Obelisk", CatStone, PlaceholderShape.Monument, 1, 4, 0, DecoMaterial.Basalt, "3a3b40", "1a1a20", "monument", "garden", "stone", "dark");
            DecoItem("plinth_marble", "Marble Plinth", CatStone, PlaceholderShape.Monument, 1, 1, 1, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "pedestal", "statue", "stone");
            DecoItem("plinth_granite", "Granite Plinth", CatStone, PlaceholderShape.Monument, 1, 1, 1, DecoMaterial.Granite, "8c8a86", "5c5963", "pedestal", "statue", "stone");
            DecoItem("urn_marble", "Marble Urn", CatStone, PlaceholderShape.Monument, 1, 1.5f, 2, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "urn", "garden", "stone");
            DecoItem("urn_marble_ivy", "Marble Urn with Ivy", CatStone, PlaceholderShape.Monument, 1, 1.5f, 2, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "urn", "garden", "ivy", "stone");
            DecoItem("urn_clay", "Clay Urn", CatStone, PlaceholderShape.Monument, 1, 1.5f, 2, DecoMaterial.Terracotta, "c2643a", "8a3a20", "urn", "garden", "pottery");
            DecoItem("sarcophagus", "Sarcophagus", CatStone, PlaceholderShape.Monument, 2, 1, 3, DecoMaterial.Limestone, "d9d2bd", "8a8070", "tomb", "crypt", "dungeon", "stone");
            DecoItem("sarcophagus_marble", "Marble Sarcophagus", CatStone, PlaceholderShape.Monument, 2, 1, 3, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "tomb", "crypt", "stone");
            string[] tombMats = { "granite", "slate", "mossy" };
            DecoMaterial[] tombM = { DecoMaterial.Granite, DecoMaterial.Slate, DecoMaterial.MossyStone };
            for (int i = 0; i < 3; i++)
            {
                DecoItem("tombstone_round_" + tombMats[i], "Rounded Tombstone (" + StoneMats[i == 0 ? 2 : (i == 1 ? 1 : 7)].name + ")", CatStone, PlaceholderShape.Monument, 1, 1.5f, 4, tombM[i], "8c8a86", "5c5963", "grave", "tomb", "graveyard", "spooky", "stone");
                DecoItem("tombstone_cross_" + tombMats[i], "Cross Tombstone (" + StoneMats[i == 0 ? 2 : (i == 1 ? 1 : 7)].name + ")", CatStone, PlaceholderShape.Monument, 1, 1.5f, 5, tombM[i], "8c8a86", "5c5963", "grave", "tomb", "graveyard", "spooky", "stone");
                DecoItem("tombstone_tablet_" + tombMats[i], "Tablet Tombstone (" + StoneMats[i == 0 ? 2 : (i == 1 ? 1 : 7)].name + ")", CatStone, PlaceholderShape.Monument, 1, 1.5f, 6, tombM[i], "8c8a86", "5c5963", "grave", "tomb", "graveyard", "spooky", "stone");
            }
            DecoItem("sundial", "Sundial", CatStone, PlaceholderShape.Monument, 1, 1.5f, 7, DecoMaterial.Sandstone, "d2b184", "8a6a40", "garden", "time", "stone");
            DecoItem("birdbath", "Birdbath", CatStone, PlaceholderShape.Monument, 1, 1.5f, 8, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "garden", "water", "stone");
            DecoItem("fountain", "Marble Fountain", CatStone, PlaceholderShape.Monument, 2, 2, 9, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "garden", "water", "courtyard", "stone");
            DecoItem("fountain_granite", "Granite Fountain", CatStone, PlaceholderShape.Monument, 2, 2, 9, DecoMaterial.Granite, "8c8a86", "5c5963", "garden", "water", "courtyard", "stone");
            DecoItem("well", "Village Well", CatStone, PlaceholderShape.Monument, 1.5f, 1.5f, 10, DecoMaterial.Cobble, "8a847c", "5c5963", "village", "water", "courtyard");
            DecoItem("bench_stone", "Stone Bench", CatStone, PlaceholderShape.Monument, 2, 0.75f, 11, DecoMaterial.Granite, "8c8a86", "5c5963", "garden", "seat", "stone");
            DecoItem("bench_marble", "Marble Bench", CatStone, PlaceholderShape.Monument, 2, 0.75f, 11, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "garden", "seat", "stone");
            DecoItem("stairs_deco_2", "Stone Stairs 2×1", CatStone, PlaceholderShape.Monument, 2, 1, 12, DecoMaterial.Granite, "8c8a86", "5c5963", "stairs", "steps", "castle", "background");
            DecoItem("stairs_deco_3", "Stone Stairs 3×1.5", CatStone, PlaceholderShape.Monument, 3, 1.5f, 12, DecoMaterial.Sandstone, "d2b184", "8a6a40", "stairs", "steps", "castle", "background");
            DecoItem("stairs_deco_marble", "Marble Stairs 2×1", CatStone, PlaceholderShape.Monument, 2, 1, 12, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "stairs", "steps", "temple", "background");
            DecoItem("column_stump_marble", "Marble Column Stump", CatStone, PlaceholderShape.Monument, 1, 1, 13, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "ruin", "column", "stone");
            DecoItem("column_stump_granite", "Granite Column Stump", CatStone, PlaceholderShape.Monument, 1, 1, 13, DecoMaterial.Granite, "8c8a86", "5c5963", "ruin", "column", "stone");
            DecoItem("boulder_granite", "Granite Boulder", CatStone, PlaceholderShape.Monument, 1.5f, 1, 14, DecoMaterial.Granite, "8c8a86", "5c5963", "rock", "cave", "nature", "stone");
            DecoItem("boulder_mossy", "Mossy Boulder", CatStone, PlaceholderShape.Monument, 1.5f, 1, 14, DecoMaterial.MossyStone, "7f8a7a", "4a5a4a", "rock", "forest", "nature", "stone");
            DecoItem("boulder_basalt", "Basalt Boulder", CatStone, PlaceholderShape.Monument, 1.5f, 1, 14, DecoMaterial.Basalt, "3a3b40", "1a1a20", "rock", "cave", "dark", "stone");
            DecoItem("boulder_pile", "Boulder Pile", CatStone, PlaceholderShape.Monument, 2, 1.5f, 15, DecoMaterial.Granite, "8c8a86", "5c5963", "rock", "cave", "nature", "stone");
            DecoItem("boulder_pile_mossy", "Mossy Boulder Pile", CatStone, PlaceholderShape.Monument, 2, 1.5f, 15, DecoMaterial.MossyStone, "7f8a7a", "4a5a4a", "rock", "forest", "ruins", "stone");
            DecoItem("rubble", "Rubble", CatStone, PlaceholderShape.Monument, 2, 0.75f, 16, DecoMaterial.Granite, "8c8a86", "5c5963", "ruin", "debris", "stone");
            DecoItem("rubble_sandstone", "Sandstone Rubble", CatStone, PlaceholderShape.Monument, 2, 0.75f, 16, DecoMaterial.Sandstone, "d2b184", "8a6a40", "ruin", "debris", "stone");
            DecoItem("stalagmite_deco", "Stalagmite", CatStone, PlaceholderShape.Monument, 1, 2, 17, DecoMaterial.Slate, "5a6470", "2c3440", "cave", "stone");
            DecoItem("stalagmite_basalt", "Basalt Stalagmite", CatStone, PlaceholderShape.Monument, 1, 2, 17, DecoMaterial.Basalt, "3a3b40", "1a1a20", "cave", "dark", "stone");
            DecoItem("flagstone", "Cracked Flagstone", CatStone, PlaceholderShape.Monument, 1, 1, 18, DecoMaterial.Slate, "5a6470", "2c3440", "floor", "paving", "castle", "background");
            DecoItem("flagstone_sandstone", "Sandstone Flagstone", CatStone, PlaceholderShape.Monument, 1, 1, 18, DecoMaterial.Sandstone, "d2b184", "8a6a40", "floor", "paving", "castle", "background");
        }

        // ---------------------------------------------------------------- ivy & plants

        static void BuildPlants()
        {
            // ivy: three palettes
            string[] ivyIds = { "ivy", "ivy_autumn", "ivy_dead" };
            string[] ivyNames = { "Ivy", "Autumn Ivy", "Dead Ivy" };
            string[] ivyC1 = { "4f9a4a", "d86a2a", "8a6a4a" };
            string[] ivyC2 = { "2a5a2a", "8a3a1a", "4a3a2a" };
            for (int k = 0; k < 3; k++)
            {
                string t = ivyIds[k] == "ivy" ? "ivy" : ivyIds[k].Replace("ivy_", "");
                DecoItem(ivyIds[k] + "_hang_1", ivyNames[k] + " Strand 1", CatPlants, PlaceholderShape.Ivy, 0.5f, 1, 0, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "castle", "ruins", t);
                DecoItem(ivyIds[k] + "_hang_2", ivyNames[k] + " Strand 2", CatPlants, PlaceholderShape.Ivy, 0.5f, 2, 0, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "castle", "ruins", t);
                DecoItem(ivyIds[k] + "_hang_3", ivyNames[k] + " Strand 3", CatPlants, PlaceholderShape.Ivy, 0.5f, 3, 0, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "castle", "ruins", t);
                DecoItem(ivyIds[k] + "_curtain", ivyNames[k] + " Curtain 2×2", CatPlants, PlaceholderShape.Ivy, 2, 2, 0, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "castle", "ruins", t);
                DecoItem(ivyIds[k] + "_patch_1", ivyNames[k] + " Patch", CatPlants, PlaceholderShape.Ivy, 1, 1, 1, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "wall", t);
                DecoItem(ivyIds[k] + "_patch_2", ivyNames[k] + " Patch 2×2", CatPlants, PlaceholderShape.Ivy, 2, 2, 1, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "wall", t);
                DecoItem(ivyIds[k] + "_patch_3", ivyNames[k] + " Patch 3×2", CatPlants, PlaceholderShape.Ivy, 3, 2, 1, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "wall", t);
                DecoItem(ivyIds[k] + "_corner_l", ivyNames[k] + " Corner (left)", CatPlants, PlaceholderShape.Ivy, 1, 1, 2, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "wall", t);
                DecoItem(ivyIds[k] + "_corner_r", ivyNames[k] + " Corner (right)", CatPlants, PlaceholderShape.Ivy, 1, 1, 3, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "wall", t);
                DecoItem(ivyIds[k] + "_creep", ivyNames[k] + " Creeper 3×½", CatPlants, PlaceholderShape.Ivy, 3, 0.5f, 4, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "ground", t);
                DecoItem(ivyIds[k] + "_swag", ivyNames[k] + " Swag 3×1", CatPlants, PlaceholderShape.Ivy, 3, 1, 5, DecoMaterial.None, ivyC1[k], ivyC2[k], "ivy", "plant", "arch", "garland", t);
            }
            DecoItem("ivy_wall_2", "Ivy-clad Wall 2×2", CatPlants, PlaceholderShape.Facade, 2, 2, 0, DecoMaterial.Ivy, "3f8a3a", "2a5a2a", "ivy", "wall", "background");
            DecoItem("ivy_wall_1", "Ivy-clad Wall", CatPlants, PlaceholderShape.Facade, 1, 1, 0, DecoMaterial.Ivy, "3f8a3a", "2a5a2a", "ivy", "wall", "background");

            // foliage (variant table: see DecorArt.DrawFoliage)
            DecoItem("moss_patch", "Moss Patch", CatPlants, PlaceholderShape.Foliage, 1, 0.5f, 0, DecoMaterial.None, "5f8a3a", "3a5a2a", "moss", "plant", "cave", "ruins");
            DecoItem("moss_patch_2", "Moss Patch 2×½", CatPlants, PlaceholderShape.Foliage, 2, 0.5f, 0, DecoMaterial.None, "5f8a3a", "3a5a2a", "moss", "plant", "cave", "ruins");
            DecoItem("moss_curtain", "Moss Curtain", CatPlants, PlaceholderShape.Foliage, 1, 2, 1, DecoMaterial.None, "5f8a3a", "3a5a2a", "moss", "plant", "cave", "swamp");
            DecoItem("lichen", "Lichen", CatPlants, PlaceholderShape.Foliage, 1, 1, 2, DecoMaterial.None, "a8c080", "6a8a5a", "lichen", "plant", "stone", "ruins");
            DecoItem("fern", "Fern", CatPlants, PlaceholderShape.Foliage, 1, 1, 3, DecoMaterial.None, "3f8a3a", "2a5a2a", "fern", "plant", "forest");
            DecoItem("fern_big", "Big Fern", CatPlants, PlaceholderShape.Foliage, 2, 1.5f, 3, DecoMaterial.None, "3f8a3a", "2a5a2a", "fern", "plant", "forest", "jungle");
            DecoItem("thistle", "Thistle", CatPlants, PlaceholderShape.Foliage, 0.5f, 1, 4, DecoMaterial.None, "6a9a5a", "3a5a2a", "thistle", "plant", "highland", "flower");
            DecoItem("reeds", "Reeds", CatPlants, PlaceholderShape.Foliage, 1, 1.5f, 5, DecoMaterial.None, "8aa04a", "4a6a2a", "reeds", "plant", "swamp", "river");
            DecoItem("briar", "Briar", CatPlants, PlaceholderShape.Foliage, 1.5f, 1, 6, DecoMaterial.None, "3f8a3a", "4a3a2a", "briar", "thorn", "plant", "forest");
            DecoItem("nettles", "Nettles", CatPlants, PlaceholderShape.Foliage, 1, 1, 7, DecoMaterial.None, "4a9a3a", "2a5a2a", "nettle", "plant", "forest");
            DecoItem("wheat_sheaf", "Wheat Sheaf", CatPlants, PlaceholderShape.Foliage, 1, 1.5f, 8, DecoMaterial.None, "e0c060", "8a6a2a", "wheat", "farm", "harvest", "village");
            DecoItem("heather", "Heather", CatPlants, PlaceholderShape.Foliage, 1.5f, 1, 9, DecoMaterial.None, "b070c0", "3a5a2a", "heather", "plant", "highland", "flower");
            string[] flowerIds = { "red", "blue", "yellow", "white", "pink" };
            string[] flowerC = { "d83a3a", "3a6ad8", "f0d040", "f4efe4", "ff8ac8" };
            for (int i = 0; i < flowerIds.Length; i++)
                DecoItem("wildflowers_" + flowerIds[i], "Wildflowers (" + flowerIds[i] + ")", CatPlants, PlaceholderShape.Foliage, 1, 1, 10, DecoMaterial.None, flowerC[i], "3a7a2a", "flower", "plant", "meadow", "garden", flowerIds[i]);
            DecoItem("roses_red", "Climbing Roses (red)", CatPlants, PlaceholderShape.Foliage, 1, 2, 11, DecoMaterial.None, "d83a3a", "2a5a2a", "rose", "flower", "plant", "garden", "castle");
            DecoItem("roses_white", "Climbing Roses (white)", CatPlants, PlaceholderShape.Foliage, 1, 2, 11, DecoMaterial.None, "f4efe4", "2a5a2a", "rose", "flower", "plant", "garden", "castle");
            DecoItem("roses_pink", "Climbing Roses (pink)", CatPlants, PlaceholderShape.Foliage, 1, 2, 11, DecoMaterial.None, "ff8ac8", "2a5a2a", "rose", "flower", "plant", "garden", "castle");
            DecoItem("wisteria", "Wisteria", CatPlants, PlaceholderShape.Foliage, 2, 2, 12, DecoMaterial.None, "9a70e0", "4a3a2a", "wisteria", "flower", "plant", "garden");
            DecoItem("grapevine", "Grapevine", CatPlants, PlaceholderShape.Foliage, 2, 1.5f, 13, DecoMaterial.None, "5a9a3a", "4a3a2a", "grape", "vine", "plant", "vineyard");
            DecoItem("hops", "Hop Vine", CatPlants, PlaceholderShape.Foliage, 1, 2, 14, DecoMaterial.None, "8ac05a", "3a5a2a", "hops", "vine", "plant", "tavern");
            DecoItem("topiary_ball", "Topiary Ball", CatPlants, PlaceholderShape.Foliage, 1, 1.5f, 15, DecoMaterial.None, "3f8a3a", "2a5a2a", "topiary", "hedge", "garden", "plant");
            DecoItem("topiary_cone", "Topiary Cone", CatPlants, PlaceholderShape.Foliage, 1, 2, 16, DecoMaterial.None, "3f8a3a", "2a5a2a", "topiary", "hedge", "garden", "plant");
            DecoItem("topiary_spiral", "Topiary Spiral", CatPlants, PlaceholderShape.Foliage, 1, 2, 17, DecoMaterial.None, "3f8a3a", "2a5a2a", "topiary", "hedge", "garden", "plant");
            DecoItem("hedge_2", "Hedge 2×1", CatPlants, PlaceholderShape.Foliage, 2, 1, 18, DecoMaterial.None, "3f8a3a", "2a5a2a", "hedge", "garden", "plant");
            DecoItem("hedge_3", "Hedge 3×1", CatPlants, PlaceholderShape.Foliage, 3, 1, 18, DecoMaterial.None, "3f8a3a", "2a5a2a", "hedge", "garden", "plant");
            DecoItem("hedge_tall", "Tall Hedge 2×2", CatPlants, PlaceholderShape.Foliage, 2, 2, 18, DecoMaterial.None, "3f8a3a", "2a5a2a", "hedge", "garden", "maze", "plant");
            DecoItem("hedge_arch", "Hedge Arch", CatPlants, PlaceholderShape.Foliage, 3, 3, 19, DecoMaterial.None, "3f8a3a", "2a5a2a", "hedge", "garden", "arch", "plant");
            DecoItem("potted_plant", "Potted Plant", CatPlants, PlaceholderShape.Foliage, 1, 1.5f, 20, DecoMaterial.None, "4f9a4a", "2a5a2a", "pot", "plant", "garden", "interior");
            DecoItem("window_box", "Window Box", CatPlants, PlaceholderShape.Foliage, 1, 1, 21, DecoMaterial.None, "d83a3a", "3a7a2a", "flower", "window", "village", "plant");
            DecoItem("grass_tuft", "Grass Tuft", CatPlants, PlaceholderShape.Foliage, 1, 0.5f, 22, DecoMaterial.None, "6ab04a", "3a7a2a", "grass", "plant", "meadow");
            DecoItem("grass_tuft_dry", "Dry Grass Tuft", CatPlants, PlaceholderShape.Foliage, 1, 0.5f, 22, DecoMaterial.None, "c0b060", "8a7a3a", "grass", "plant", "autumn", "dry");
            DecoItem("tree_stump", "Tree Stump", CatPlants, PlaceholderShape.Foliage, 1, 0.75f, 23, DecoMaterial.None, "c9a06a", "6a4a2a", "stump", "wood", "forest");
            DecoItem("log", "Fallen Log", CatPlants, PlaceholderShape.Foliage, 2, 0.75f, 24, DecoMaterial.None, "c9a06a", "6a4a2a", "log", "wood", "forest");
            DecoItem("branch", "Fallen Branch", CatPlants, PlaceholderShape.Foliage, 1.5f, 1, 25, DecoMaterial.None, "4f9a4a", "6a4a2a", "branch", "wood", "forest");
            DecoItem("lily_pads", "Lily Pads", CatPlants, PlaceholderShape.Foliage, 1.5f, 0.75f, 26, DecoMaterial.None, "4f9a4a", "2a6a2a", "lily", "pond", "water", "swamp", "plant");

            // trees
            DecoItem("tree_willow", "Willow Tree", CatPlants, PlaceholderShape.TreeDeco, 3, 4, 0, DecoMaterial.None, "7fb06a", "6a4a2a", "tree", "willow", "river", "nature");
            DecoItem("tree_birch", "Birch Tree", CatPlants, PlaceholderShape.TreeDeco, 2, 4, 1, DecoMaterial.None, "a0d070", "3a3a3a", "tree", "birch", "forest", "nature");
            DecoItem("tree_birch_autumn", "Autumn Birch", CatPlants, PlaceholderShape.TreeDeco, 2, 4, 1, DecoMaterial.None, "f0c040", "3a3a3a", "tree", "birch", "autumn", "nature");
            DecoItem("tree_apple", "Apple Tree", CatPlants, PlaceholderShape.TreeDeco, 3, 3, 2, DecoMaterial.None, "4f9a4a", "6a4a2a", "tree", "apple", "orchard", "nature");
            DecoItem("tree_yew", "Yew Tree", CatPlants, PlaceholderShape.TreeDeco, 2, 4, 3, DecoMaterial.None, "2a5a3a", "5a3a1a", "tree", "yew", "graveyard", "nature");
            DecoItem("tree_hawthorn", "Hawthorn", CatPlants, PlaceholderShape.TreeDeco, 2, 2, 4, DecoMaterial.None, "5a9a4a", "5a3a1a", "tree", "hawthorn", "hedgerow", "nature");
            DecoItem("tree_cypress", "Cypress", CatPlants, PlaceholderShape.TreeDeco, 1, 4, 5, DecoMaterial.None, "2f6a3a", "5a3a1a", "tree", "cypress", "garden", "nature");
            DecoItem("tree_cypress_tall", "Tall Cypress", CatPlants, PlaceholderShape.TreeDeco, 1, 6, 5, DecoMaterial.None, "2f6a3a", "5a3a1a", "tree", "cypress", "garden", "nature");
        }

        // ---------------------------------------------------------------- roofs & windows

        static void BuildRoofsAndWindows()
        {
            var roofMats = new[]
            {
                new MatInfo(DecoMaterial.Slate, "slate", "Slate"),
                new MatInfo(DecoMaterial.Terracotta, "tile", "Clay Tile"),
                new MatInfo(DecoMaterial.Thatch, "thatch", "Thatch"),
                new MatInfo(DecoMaterial.Lead, "lead", "Lead"),
                new MatInfo(DecoMaterial.Copper, "copper", "Verdigris Copper"),
                new MatInfo(DecoMaterial.Oak, "shingle", "Oak Shingle"),
            };
            foreach (var m in roofMats)
            {
                DecoItem("roof_" + m.id + "_r", m.name + " Roof (rising right)", CatRoofs, PlaceholderShape.Roof, 2, 1, 0, m.m, "5a6470", "2c3440", "roof", "castle", "village", "house");
                DecoItem("roof_" + m.id + "_l", m.name + " Roof (rising left)", CatRoofs, PlaceholderShape.Roof, 2, 1, 1, m.m, "5a6470", "2c3440", "roof", "castle", "village", "house");
                DecoItem("roof_" + m.id + "_gable", m.name + " Gable 3×1", CatRoofs, PlaceholderShape.Roof, 3, 1, 2, m.m, "5a6470", "2c3440", "roof", "castle", "village", "house");
                DecoItem("roof_" + m.id + "_flat", m.name + " Roof Strip 2×½", CatRoofs, PlaceholderShape.Roof, 2, 0.5f, 3, m.m, "5a6470", "2c3440", "roof", "castle", "village", "house");
            }
            DecoItem("tower_cap_slate", "Slate Tower Cap", CatRoofs, PlaceholderShape.TowerCap, 2, 2, 0, DecoMaterial.Slate, "5a6470", "2c3440", "tower", "roof", "castle", "spire");
            DecoItem("tower_cap_copper", "Copper Tower Cap", CatRoofs, PlaceholderShape.TowerCap, 2, 2, 0, DecoMaterial.Copper, "6fae8f", "3a6a4a", "tower", "roof", "castle", "spire");
            DecoItem("tower_cap_tile", "Tiled Tower Cap", CatRoofs, PlaceholderShape.TowerCap, 2, 2, 0, DecoMaterial.Terracotta, "c2643a", "8a3a20", "tower", "roof", "castle", "spire");
            DecoItem("tower_cap_tall", "Tall Slate Spire", CatRoofs, PlaceholderShape.TowerCap, 2, 4, 0, DecoMaterial.Slate, "5a6470", "2c3440", "tower", "roof", "castle", "spire");
            DecoItem("onion_dome_copper", "Copper Onion Dome", CatRoofs, PlaceholderShape.TowerCap, 2, 2, 1, DecoMaterial.Copper, "6fae8f", "3a6a4a", "dome", "roof", "tower", "eastern");
            DecoItem("onion_dome_gold", "Gilded Onion Dome", CatRoofs, PlaceholderShape.TowerCap, 2, 2, 1, DecoMaterial.Gold, "e0b64a", "8a6a20", "dome", "roof", "tower", "royal");
            DecoItem("dome_lead", "Lead Dome", CatRoofs, PlaceholderShape.TowerCap, 2, 1, 2, DecoMaterial.Lead, "6c6f78", "3a3a40", "dome", "roof", "temple");
            DecoItem("dome_marble", "Marble Dome", CatRoofs, PlaceholderShape.TowerCap, 3, 1.5f, 2, DecoMaterial.Marble, "e8e4dc", "b9b3a8", "dome", "roof", "temple");
            DecoItem("chimney_stone", "Stone Chimney", CatRoofs, PlaceholderShape.TowerCap, 0.5f, 1.5f, 3, DecoMaterial.Granite, "8c8a86", "5c5963", "chimney", "roof", "house", "smoke");
            DecoItem("chimney_brick", "Brick Chimney", CatRoofs, PlaceholderShape.TowerCap, 0.5f, 1.5f, 3, DecoMaterial.Brick, "9a5a48", "5a2a20", "chimney", "roof", "house", "smoke");
            DecoItem("finial", "Gilded Finial", CatRoofs, PlaceholderShape.TowerCap, 0.5f, 1, 4, DecoMaterial.None, "e0b64a", "4a4c56", "finial", "roof", "spire", "gold");
            DecoItem("weathervane", "Weathervane", CatRoofs, PlaceholderShape.TowerCap, 1, 1, 5, DecoMaterial.None, "4a4c56", "e0b64a", "weathervane", "roof", "cockerel");
            DecoItem("dormer", "Dormer Window", CatRoofs, PlaceholderShape.TowerCap, 1, 1, 6, DecoMaterial.Slate, "5a6470", "2c3440", "dormer", "roof", "window", "house");

            // windows
            DecoItem("window_slit", "Arrow Slit", CatRoofs, PlaceholderShape.WindowDeco, 0.5f, 1, 0, DecoMaterial.Granite, "1a1420", "9a9aa8", "window", "castle", "arrow", "tower");
            DecoItem("window_slit_sandstone", "Sandstone Arrow Slit", CatRoofs, PlaceholderShape.WindowDeco, 0.5f, 1, 0, DecoMaterial.Sandstone, "1a1420", "8a6a40", "window", "castle", "arrow", "tower");
            DecoItem("window_round", "Round Window", CatRoofs, PlaceholderShape.WindowDeco, 1, 1, 1, DecoMaterial.Granite, "3a2a5a", "9a9aa8", "window", "castle", "glass");
            DecoItem("window_round_lit", "Round Window (lit)", CatRoofs, PlaceholderShape.WindowDeco, 1, 1, 1, DecoMaterial.Granite, "ffd27a", "9a9aa8", "window", "castle", "glass", "night", "light");
            DecoItem("window_gothic", "Gothic Window", CatRoofs, PlaceholderShape.WindowDeco, 1, 2, 2, DecoMaterial.Granite, "3a2a5a", "9a9aa8", "window", "castle", "church", "glass");
            DecoItem("window_gothic_marble", "Gothic Window (Marble)", CatRoofs, PlaceholderShape.WindowDeco, 1, 2, 2, DecoMaterial.Marble, "3a2a5a", "b9b3a8", "window", "temple", "church", "glass");
            DecoItem("window_gothic_lit", "Gothic Window (lit)", CatRoofs, PlaceholderShape.WindowDeco, 1, 2, 9, DecoMaterial.Granite, "ffd27a", "9a9aa8", "window", "castle", "night", "light", "glass");
            DecoItem("window_gothic_tall", "Tall Gothic Window", CatRoofs, PlaceholderShape.WindowDeco, 1, 3, 2, DecoMaterial.Granite, "3a2a5a", "9a9aa8", "window", "cathedral", "church", "glass");
            DecoItem("window_rose", "Rose Window", CatRoofs, PlaceholderShape.WindowDeco, 2, 2, 3, DecoMaterial.Granite, "b83a5a", "4a4c56", "window", "cathedral", "church", "stained", "glass");
            DecoItem("window_trefoil", "Trefoil Window", CatRoofs, PlaceholderShape.WindowDeco, 1, 1, 4, DecoMaterial.Granite, "3a2a5a", "9a9aa8", "window", "church", "gothic", "glass");
            DecoItem("window_quatrefoil", "Quatrefoil Window", CatRoofs, PlaceholderShape.WindowDeco, 1, 1, 5, DecoMaterial.Sandstone, "3a2a5a", "8a6a40", "window", "church", "gothic", "glass");
            DecoItem("window_shutters_open", "Shuttered Window (open)", CatRoofs, PlaceholderShape.WindowDeco, 1.5f, 1.5f, 6, DecoMaterial.Plaster, "3a2a5a", "9a9aa8", "window", "village", "house", "shutter");
            DecoItem("window_shutters_closed", "Shuttered Window (closed)", CatRoofs, PlaceholderShape.WindowDeco, 1.5f, 1.5f, 7, DecoMaterial.Plaster, "3a2a5a", "9a9aa8", "window", "village", "house", "shutter");
            DecoItem("window_barred", "Barred Window", CatRoofs, PlaceholderShape.WindowDeco, 1, 1, 8, DecoMaterial.Granite, "14101c", "4a4c56", "window", "dungeon", "prison", "bars");
            DecoItem("window_barred_basalt", "Barred Window (Basalt)", CatRoofs, PlaceholderShape.WindowDeco, 1, 1, 8, DecoMaterial.Basalt, "14101c", "4a4c56", "window", "dungeon", "prison", "bars", "dark");
            DecoItem("window_oriel", "Oriel Window", CatRoofs, PlaceholderShape.WindowDeco, 2, 2, 10, DecoMaterial.Sandstone, "3a2a5a", "8a6a40", "window", "bay", "castle", "manor");
            string[] stainIds = { "red", "blue", "green", "purple", "gold" };
            string[] stainC = { "b83a5a", "2c4fb8", "2f8a3a", "6a3cff", "e0b64a" };
            for (int i = 0; i < stainIds.Length; i++)
                DecoItem("window_stained_" + stainIds[i], "Stained Glass (" + stainIds[i] + ")", CatRoofs, PlaceholderShape.WindowDeco, 1, 2, 11, DecoMaterial.Granite, stainC[i], "4a4c56", "window", "stained", "church", "glass", stainIds[i]);

            // doors
            DecoItem("door_oak", "Oak Door", CatRoofs, PlaceholderShape.Door, 1, 2, 0, DecoMaterial.Oak, "a9743d", "5a3a1a", "door", "castle", "village", "house", "wood");
            DecoItem("door_dark", "Dark Oak Door", CatRoofs, PlaceholderShape.Door, 1, 2, 0, DecoMaterial.DarkWood, "5a3a1a", "3a2a1a", "door", "castle", "wood");
            DecoItem("door_banded", "Iron-banded Door", CatRoofs, PlaceholderShape.Door, 1, 2, 1, DecoMaterial.Oak, "a9743d", "4a4c56", "door", "castle", "keep", "iron", "wood");
            DecoItem("door_gothic", "Gothic Door", CatRoofs, PlaceholderShape.Door, 1, 2, 2, DecoMaterial.DarkWood, "5a3a1a", "4a4c56", "door", "church", "castle", "wood");
            DecoItem("door_double", "Double Doors", CatRoofs, PlaceholderShape.Door, 2, 3, 3, DecoMaterial.Oak, "a9743d", "4a4c56", "door", "hall", "castle", "gate", "wood");
            DecoItem("door_dungeon", "Dungeon Door", CatRoofs, PlaceholderShape.Door, 1, 2, 4, DecoMaterial.DarkWood, "5a3a1a", "4a4c56", "door", "dungeon", "prison", "iron", "wood");
            DecoItem("trapdoor", "Trapdoor", CatRoofs, PlaceholderShape.Door, 1, 1, 5, DecoMaterial.Oak, "a9743d", "4a4c56", "door", "trap", "cellar", "floor", "wood");
        }

        // ---------------------------------------------------------------- furnishings

        static void BuildFurnishings()
        {
            DecoItem("throne_red", "Throne (red)", CatFurnish, PlaceholderShape.Furniture, 1.5f, 2, 0, DecoMaterial.DarkWood, "b8302c", "e0b64a", "throne", "royal", "hall", "seat");
            DecoItem("throne_blue", "Throne (blue)", CatFurnish, PlaceholderShape.Furniture, 1.5f, 2, 0, DecoMaterial.DarkWood, "2c4fb8", "e0b64a", "throne", "royal", "hall", "seat");
            DecoItem("throne_purple", "Throne (purple)", CatFurnish, PlaceholderShape.Furniture, 1.5f, 2, 0, DecoMaterial.DarkWood, "6a3cff", "e0b64a", "throne", "royal", "hall", "seat");
            DecoItem("table_oak", "Oak Table", CatFurnish, PlaceholderShape.Furniture, 2, 1, 1, DecoMaterial.Oak, "a9743d", "b8302c", "table", "hall", "tavern", "wood");
            DecoItem("table_long", "Long Feast Table 4×1", CatFurnish, PlaceholderShape.Furniture, 4, 1, 1, DecoMaterial.DarkWood, "5a3a1a", "e0b64a", "table", "feast", "hall", "wood");
            DecoItem("chair_oak", "Oak Chair", CatFurnish, PlaceholderShape.Furniture, 1, 1.5f, 2, DecoMaterial.Oak, "b8302c", "5a3a1a", "chair", "seat", "hall", "tavern");
            DecoItem("chair_dark", "Dark Chair", CatFurnish, PlaceholderShape.Furniture, 1, 1.5f, 2, DecoMaterial.DarkWood, "2c4fb8", "3a2a1a", "chair", "seat", "hall");
            DecoItem("bookshelf", "Bookshelf", CatFurnish, PlaceholderShape.Furniture, 1.5f, 2, 3, DecoMaterial.Oak, "a9743d", "5a3a1a", "books", "library", "study", "shelf");
            DecoItem("bookshelf_tall", "Tall Bookshelf 1.5×3", CatFurnish, PlaceholderShape.Furniture, 1.5f, 3, 3, DecoMaterial.DarkWood, "5a3a1a", "3a2a1a", "books", "library", "study", "shelf");
            DecoItem("chest_closed", "Treasure Chest", CatFurnish, PlaceholderShape.Furniture, 1, 0.75f, 4, DecoMaterial.Oak, "a9743d", "4a4c56", "chest", "treasure", "loot", "storage");
            DecoItem("chest_open", "Open Treasure Chest", CatFurnish, PlaceholderShape.Furniture, 1, 1, 5, DecoMaterial.Oak, "a9743d", "4a4c56", "chest", "treasure", "loot", "gold");
            DecoItem("bed", "Bed", CatFurnish, PlaceholderShape.Furniture, 2, 1.5f, 6, DecoMaterial.Oak, "b8302c", "5a3a1a", "bed", "bedroom", "inn");
            DecoItem("bed_blue", "Bed (blue)", CatFurnish, PlaceholderShape.Furniture, 2, 1.5f, 6, DecoMaterial.DarkWood, "2c4fb8", "3a2a1a", "bed", "bedroom", "inn");
            DecoItem("wardrobe", "Wardrobe", CatFurnish, PlaceholderShape.Furniture, 1, 2, 7, DecoMaterial.DarkWood, "5a3a1a", "3a2a1a", "wardrobe", "bedroom", "storage");
            DecoItem("wine_rack", "Wine Rack", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1.5f, 8, DecoMaterial.Oak, "a9743d", "5a3a1a", "wine", "cellar", "tavern", "storage");
            DecoItem("cauldron", "Cauldron", CatFurnish, PlaceholderShape.Furniture, 1, 1, 9, DecoMaterial.None, "5fff7a", "4a4c56", "cauldron", "witch", "kitchen", "magic");
            DecoItem("cauldron_red", "Bubbling Cauldron (red)", CatFurnish, PlaceholderShape.Furniture, 1, 1, 9, DecoMaterial.None, "ff4a4a", "4a4c56", "cauldron", "witch", "kitchen", "magic");
            DecoItem("anvil", "Anvil", CatFurnish, PlaceholderShape.Furniture, 1, 0.75f, 10, DecoMaterial.None, "4a4c56", "2a2a30", "anvil", "smith", "forge", "iron");
            DecoItem("spinning_wheel", "Spinning Wheel", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1.5f, 11, DecoMaterial.Oak, "a9743d", "5a3a1a", "spinning", "cottage", "craft");
            DecoItem("loom", "Loom", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1.5f, 12, DecoMaterial.Oak, "b8302c", "e0b64a", "loom", "weaving", "cottage", "craft");
            DecoItem("desk", "Scribe's Desk", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1.5f, 13, DecoMaterial.Oak, "a9743d", "5a3a1a", "desk", "study", "library", "scroll");
            DecoItem("altar", "Altar", CatFurnish, PlaceholderShape.Furniture, 2, 1.5f, 14, DecoMaterial.Marble, "b8302c", "e0b64a", "altar", "church", "chapel", "candle");
            DecoItem("altar_blue", "Altar (blue)", CatFurnish, PlaceholderShape.Furniture, 2, 1.5f, 14, DecoMaterial.Marble, "2c4fb8", "e0b64a", "altar", "church", "chapel", "candle");
            string[] clothIds = { "red", "blue", "green", "purple", "gold" };
            string[] clothC = { "b8302c", "2c4fb8", "2f8a3a", "6a3cff", "e0b64a" };
            string[] trimC = { "e0b64a", "e0b64a", "e0b64a", "e0b64a", "b8302c" };
            for (int i = 0; i < clothIds.Length; i++)
            {
                DecoItem("rug_" + clothIds[i], "Rug (" + clothIds[i] + ")", CatFurnish, PlaceholderShape.Furniture, 2, 0.5f, 15, DecoMaterial.None, clothC[i], trimC[i], "rug", "carpet", "hall", "floor", clothIds[i]);
                DecoItem("carpet_" + clothIds[i], "Long Carpet 4×½ (" + clothIds[i] + ")", CatFurnish, PlaceholderShape.Furniture, 4, 0.5f, 15, DecoMaterial.None, clothC[i], trimC[i], "rug", "carpet", "hall", "floor", clothIds[i]);
            }
            DecoItem("painting", "Painting", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1, 16, DecoMaterial.None, "e0b64a", "8a6a20", "painting", "art", "hall", "wall");
            DecoItem("painting_tall", "Tall Portrait", CatFurnish, PlaceholderShape.Furniture, 1, 1.5f, 16, DecoMaterial.None, "e0b64a", "8a6a20", "painting", "portrait", "art", "hall", "wall");
            DecoItem("mirror", "Mirror", CatFurnish, PlaceholderShape.Furniture, 1, 1.5f, 17, DecoMaterial.None, "bcd6e8", "e0b64a", "mirror", "bedroom", "wall");
            DecoItem("treasure_pile", "Treasure Pile", CatFurnish, PlaceholderShape.Furniture, 2, 1, 18, DecoMaterial.None, "e0b64a", "8a6a20", "treasure", "gold", "loot", "hoard", "dragon");
            DecoItem("treasure_pile_big", "Treasure Hoard 3×1.5", CatFurnish, PlaceholderShape.Furniture, 3, 1.5f, 18, DecoMaterial.None, "e0b64a", "8a6a20", "treasure", "gold", "loot", "hoard", "dragon");
            DecoItem("coin_sacks", "Coin Sacks", CatFurnish, PlaceholderShape.Furniture, 1, 1, 19, DecoMaterial.None, "c9b27a", "8a5a2a", "sack", "gold", "loot", "vault");
            DecoItem("potion_shelf", "Potion Shelf", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1, 20, DecoMaterial.Oak, "a9743d", "5a3a1a", "potion", "alchemy", "shelf", "magic");
            DecoItem("scroll_shelf", "Scroll Shelf", CatFurnish, PlaceholderShape.Furniture, 1.5f, 1.5f, 21, DecoMaterial.Oak, "a9743d", "5a3a1a", "scroll", "library", "shelf", "study");
            DecoItem("crown_cushion", "Crown on a Cushion", CatFurnish, PlaceholderShape.Furniture, 1, 1, 23, DecoMaterial.None, "b8302c", "e0b64a", "crown", "royal", "throne", "gold");
            DecoItem("bone_cage", "Hanging Cage", CatFurnish, PlaceholderShape.Furniture, 1, 2, 24, DecoMaterial.None, "4a4c56", "f0e9d2", "cage", "dungeon", "bones", "spooky");

            // tapestries and heraldic cloth
            string[] tapNames = { "Striped", "Lion", "Cross", "Chevron", "Fleur-de-lis", "Tree", "Dragon", "Starry" };
            string[] tapIds = { "stripes", "lion", "cross", "chevron", "fleur", "tree", "dragon", "stars" };
            string[] tapCloth = { "b8302c", "2c4fb8", "2f8a3a", "6a3cff", "e0b64a" };
            for (int t = 0; t < tapNames.Length; t++)
            for (int c = 0; c < 3; c++)
            {
                string colour = c == 0 ? "red" : (c == 1 ? "blue" : (t % 2 == 0 ? "green" : "purple"));
                string cloth = c == 0 ? tapCloth[0] : (c == 1 ? tapCloth[1] : (t % 2 == 0 ? tapCloth[2] : tapCloth[3]));
                DecoItem("tapestry_" + tapIds[t] + "_" + colour, tapNames[t] + " Tapestry (" + colour + ")", CatFurnish, PlaceholderShape.Tapestry, 1, 2, t, DecoMaterial.None, cloth, "e0b64a", "tapestry", "hall", "heraldry", "wall", "cloth", colour);
            }
            DecoItem("tapestry_grand", "Grand Tapestry 2×3", CatFurnish, PlaceholderShape.Tapestry, 2, 3, 6, DecoMaterial.None, "b8302c", "e0b64a", "tapestry", "hall", "heraldry", "wall", "dragon");
            DecoItem("tapestry_grand_blue", "Grand Tapestry 2×3 (blue)", CatFurnish, PlaceholderShape.Tapestry, 2, 3, 1, DecoMaterial.None, "2c4fb8", "e0b64a", "tapestry", "hall", "heraldry", "wall", "lion");

            // armoury
            string[] shieldIds = { "red_gold", "blue_gold", "green_white", "black_red", "purple_gold" };
            string[] shieldA = { "b8302c", "2c4fb8", "2f8a3a", "2a2a30", "6a3cff" };
            string[] shieldB = { "e0b64a", "e0b64a", "f4efe4", "b8302c", "e0b64a" };
            for (int i = 0; i < shieldIds.Length; i++)
            {
                DecoItem("shield_wall_" + shieldIds[i], "Wall Shield (" + shieldIds[i].Replace("_", " & ") + ")", CatFurnish, PlaceholderShape.Armoury, 1, 1, 0, DecoMaterial.None, shieldA[i], shieldB[i], "shield", "heraldry", "armoury", "wall");
                DecoItem("shield_swords_" + shieldIds[i], "Shield & Crossed Swords (" + shieldIds[i].Replace("_", " & ") + ")", CatFurnish, PlaceholderShape.Armoury, 1.5f, 1.5f, 1, DecoMaterial.None, shieldA[i], shieldB[i], "shield", "sword", "heraldry", "armoury", "wall");
            }
            DecoItem("weapon_rack", "Weapon Rack", CatFurnish, PlaceholderShape.Armoury, 1.5f, 2, 2, DecoMaterial.None, "b8bcc8", "5a3a1a", "weapon", "sword", "armoury", "barracks");
            DecoItem("armour_stand", "Armour Stand", CatFurnish, PlaceholderShape.Armoury, 1, 2, 3, DecoMaterial.None, "b8302c", "b8bcc8", "armour", "knight", "armoury", "hall");
            DecoItem("bow_rack", "Bow Rack", CatFurnish, PlaceholderShape.Armoury, 1.5f, 1.5f, 4, DecoMaterial.None, "a9743d", "5a3a1a", "bow", "archer", "armoury", "barracks");
            DecoItem("spear_rack", "Spear Rack", CatFurnish, PlaceholderShape.Armoury, 1.5f, 2, 5, DecoMaterial.None, "a9743d", "5a3a1a", "spear", "armoury", "barracks");
            DecoItem("shield_wall_row", "Shield Row 3×1", CatFurnish, PlaceholderShape.Armoury, 3, 1, 6, DecoMaterial.None, "b8302c", "e0b64a", "shield", "heraldry", "armoury", "hall", "wall");
            DecoItem("helmet_stand", "Helmet on a Stand", CatFurnish, PlaceholderShape.Armoury, 1, 1.5f, 7, DecoMaterial.None, "b8302c", "b8bcc8", "helmet", "knight", "armoury");
            DecoItem("battle_axe", "Wall Axe", CatFurnish, PlaceholderShape.Armoury, 1, 1, 8, DecoMaterial.None, "b8bcc8", "a9743d", "axe", "weapon", "armoury", "wall");
            DecoItem("banner_shield_red", "Banner Shield (red)", CatFurnish, PlaceholderShape.Armoury, 1, 2, 9, DecoMaterial.None, "b8302c", "e0b64a", "banner", "shield", "heraldry", "wall");
            DecoItem("banner_shield_blue", "Banner Shield (blue)", CatFurnish, PlaceholderShape.Armoury, 1, 2, 9, DecoMaterial.None, "2c4fb8", "e0b64a", "banner", "shield", "heraldry", "wall");
            DecoItem("banner_shield_green", "Banner Shield (green)", CatFurnish, PlaceholderShape.Armoury, 1, 2, 9, DecoMaterial.None, "2f8a3a", "f4efe4", "banner", "shield", "heraldry", "wall");
        }

        // ---------------------------------------------------------------- lights

        static void BuildLights()
        {
            DecoItem("brazier", "Brazier", CatLights, PlaceholderShape.Lantern, 1, 1.5f, 0, DecoMaterial.None, "ffb03a", "4a4c56", "fire", "light", "castle", "brazier");
            DecoItem("brazier_blue", "Blue-flame Brazier", CatLights, PlaceholderShape.Lantern, 1, 1.5f, 0, DecoMaterial.None, "6ad4ff", "4a4c56", "fire", "light", "magic", "brazier");
            DecoItem("brazier_green", "Green-flame Brazier", CatLights, PlaceholderShape.Lantern, 1, 1.5f, 0, DecoMaterial.None, "5fff7a", "4a4c56", "fire", "light", "magic", "brazier", "witch");
            DecoItem("lantern_hanging", "Hanging Lantern", CatLights, PlaceholderShape.Lantern, 0.5f, 1, 1, DecoMaterial.None, "ffd27a", "4a4c56", "lantern", "light", "iron", "castle");
            DecoItem("lantern_hanging_blue", "Hanging Lantern (blue)", CatLights, PlaceholderShape.Lantern, 0.5f, 1, 1, DecoMaterial.None, "6ad4ff", "4a4c56", "lantern", "light", "iron", "magic");
            DecoItem("lantern_standing", "Standing Lantern", CatLights, PlaceholderShape.Lantern, 0.5f, 1, 2, DecoMaterial.None, "ffd27a", "4a4c56", "lantern", "light", "iron", "village");
            DecoItem("lantern_paper", "Paper Lantern", CatLights, PlaceholderShape.Lantern, 0.5f, 1, 3, DecoMaterial.None, "ff8a4a", "b8302c", "lantern", "light", "festival", "paper");
            DecoItem("lantern_paper_white", "Paper Lantern (white)", CatLights, PlaceholderShape.Lantern, 0.5f, 1, 3, DecoMaterial.None, "fff4d0", "b8302c", "lantern", "light", "festival", "paper");
            DecoItem("candles", "Candles", CatLights, PlaceholderShape.Lantern, 1, 1, 4, DecoMaterial.None, "ffb03a", "f4efe4", "candle", "light", "chapel", "table");
            DecoItem("candelabra", "Candelabra", CatLights, PlaceholderShape.Furniture, 1, 1.5f, 22, DecoMaterial.None, "ffb03a", "e0b64a", "candle", "light", "hall", "gold");
            DecoItem("sconce", "Wall Sconce", CatLights, PlaceholderShape.Lantern, 0.5f, 1, 5, DecoMaterial.None, "ffb03a", "4a4c56", "candle", "light", "wall", "castle", "sconce");
            DecoItem("chandelier", "Chandelier", CatLights, PlaceholderShape.Lantern, 2, 1.5f, 6, DecoMaterial.None, "ffb03a", "4a4c56", "chandelier", "light", "hall", "ceiling");
            DecoItem("chandelier_grand", "Grand Chandelier 3×2", CatLights, PlaceholderShape.Lantern, 3, 2, 6, DecoMaterial.None, "ffb03a", "e0b64a", "chandelier", "light", "hall", "ceiling", "gold");
            DecoItem("campfire", "Campfire", CatLights, PlaceholderShape.Lantern, 1, 1, 7, DecoMaterial.None, "ffb03a", "6a4a2a", "fire", "camp", "light", "forest");
            DecoItem("lamp_post", "Lamp Post", CatLights, PlaceholderShape.Lantern, 1, 3, 8, DecoMaterial.None, "ffd27a", "4a4c56", "lamp", "light", "street", "village", "iron");
            DecoItem("glow_moss", "Glow Moss", CatLights, PlaceholderShape.Lantern, 1.5f, 1, 9, DecoMaterial.None, "6ad4ff", "2a6a8a", "moss", "light", "cave", "magic", "glow");
            DecoItem("glow_moss_green", "Glow Moss (green)", CatLights, PlaceholderShape.Lantern, 1.5f, 1, 9, DecoMaterial.None, "5fff7a", "2a6a3a", "moss", "light", "cave", "magic", "glow");
            DecoItem("crystal_lamp", "Crystal Lamp", CatLights, PlaceholderShape.Lantern, 0.75f, 1.5f, 10, DecoMaterial.None, "c8a0ff", "4a4c56", "crystal", "light", "magic", "lamp");
            DecoItem("crystal_lamp_blue", "Crystal Lamp (blue)", CatLights, PlaceholderShape.Lantern, 0.75f, 1.5f, 10, DecoMaterial.None, "6ad4ff", "4a4c56", "crystal", "light", "magic", "lamp");
            DecoItem("oil_lamp", "Oil Lamp", CatLights, PlaceholderShape.Lantern, 1, 0.75f, 11, DecoMaterial.None, "ffb03a", "a8702e", "lamp", "light", "table", "bronze");
            DecoItem("beacon", "Beacon Fire", CatLights, PlaceholderShape.Lantern, 1.5f, 2, 12, DecoMaterial.None, "ffb03a", "4a4c56", "beacon", "fire", "light", "tower", "signal");
            DecoItem("string_lights", "Festival Lights 3×1", CatLights, PlaceholderShape.Lantern, 3, 1, 13, DecoMaterial.None, "ffd27a", "5a3a1a", "festival", "light", "garland", "village");
            DecoItem("string_lights_long", "Festival Lights 5×1", CatLights, PlaceholderShape.Lantern, 5, 1, 13, DecoMaterial.None, "ffd27a", "5a3a1a", "festival", "light", "garland", "village");
        }

        // ---------------------------------------------------------------- yard & siege

        static void BuildYard()
        {
            DecoItem("hay_round", "Round Hay Bale", CatYard, PlaceholderShape.Yard, 1, 1, 0, DecoMaterial.None, "d8b85a", "8a6a2a", "hay", "farm", "village", "straw");
            DecoItem("hay_square", "Hay Bale", CatYard, PlaceholderShape.Yard, 1, 0.75f, 1, DecoMaterial.None, "d8b85a", "8a6a2a", "hay", "farm", "village", "straw");
            DecoItem("hay_stack", "Hay Stack 2×1.5", CatYard, PlaceholderShape.Yard, 2, 1.5f, 1, DecoMaterial.None, "d8b85a", "8a6a2a", "hay", "farm", "village", "straw");
            DecoItem("cart_wheel", "Cart Wheel", CatYard, PlaceholderShape.Yard, 1, 1, 2, DecoMaterial.None, "a9743d", "4a4c56", "wheel", "cart", "village", "wood");
            DecoItem("wagon_hay", "Hay Wagon", CatYard, PlaceholderShape.Yard, 2, 1.5f, 3, DecoMaterial.None, "d8b85a", "a9743d", "wagon", "cart", "hay", "village");
            DecoItem("wagon_apples", "Apple Wagon", CatYard, PlaceholderShape.Yard, 2, 1.5f, 3, DecoMaterial.None, "d83a3a", "a9743d", "wagon", "cart", "orchard", "village");
            DecoItem("trough", "Water Trough", CatYard, PlaceholderShape.Yard, 1.5f, 0.75f, 4, DecoMaterial.None, "4a86c0", "a9743d", "trough", "water", "stable", "village");
            DecoItem("rain_barrel", "Rain Barrel", CatYard, PlaceholderShape.Yard, 1, 1.25f, 5, DecoMaterial.None, "4a86c0", "a9743d", "barrel", "water", "village");
            DecoItem("bell_bronze", "Bronze Bell", CatYard, PlaceholderShape.Yard, 1, 1.25f, 6, DecoMaterial.None, "c99b3a", "4a4c56", "bell", "tower", "church", "bronze");
            DecoItem("bell_big", "Great Bell 2×2", CatYard, PlaceholderShape.Yard, 2, 2, 6, DecoMaterial.None, "c99b3a", "4a4c56", "bell", "tower", "church", "bronze");
            DecoItem("signpost", "Signpost", CatYard, PlaceholderShape.Yard, 1, 2, 7, DecoMaterial.None, "a9743d", "5a3a1a", "sign", "road", "village", "crossroads");
            DecoItem("milestone", "Milestone", CatYard, PlaceholderShape.Yard, 0.5f, 1, 8, DecoMaterial.Limestone, "d9d2bd", "8a8070", "milestone", "road", "stone");
            DecoItem("stocks", "Stocks", CatYard, PlaceholderShape.Yard, 1.5f, 1.5f, 9, DecoMaterial.None, "a9743d", "5a3a1a", "stocks", "punishment", "village", "square");
            DecoItem("training_dummy", "Training Dummy", CatYard, PlaceholderShape.Yard, 1, 2, 10, DecoMaterial.None, "d8b85a", "5a3a1a", "dummy", "training", "barracks", "yard");
            DecoItem("archery_target", "Archery Target", CatYard, PlaceholderShape.Yard, 1, 1.5f, 11, DecoMaterial.None, "d8b85a", "b8302c", "target", "archery", "training", "yard");
            DecoItem("firewood", "Firewood Pile", CatYard, PlaceholderShape.Yard, 1.5f, 1, 12, DecoMaterial.None, "a9743d", "5a3a1a", "wood", "log", "village", "winter");
            DecoItem("crate_stack", "Crate Stack", CatYard, PlaceholderShape.Yard, 1.5f, 1.5f, 13, DecoMaterial.None, "a9743d", "5a3a1a", "crate", "storage", "dock", "wood");
            DecoItem("sacks", "Grain Sacks", CatYard, PlaceholderShape.Yard, 1.5f, 1, 14, DecoMaterial.None, "c9b27a", "8a5a2a", "sack", "grain", "storage", "mill");
            DecoItem("scarecrow", "Scarecrow", CatYard, PlaceholderShape.Yard, 1, 2, 15, DecoMaterial.None, "8a5a3a", "b8302c", "scarecrow", "farm", "field", "autumn");
            DecoItem("wheelbarrow", "Wheelbarrow", CatYard, PlaceholderShape.Yard, 1.5f, 1, 16, DecoMaterial.None, "a9743d", "5a3a1a", "wheelbarrow", "farm", "garden", "wood");
            DecoItem("ladder", "Ladder", CatYard, PlaceholderShape.Yard, 0.5f, 2, 17, DecoMaterial.None, "a9743d", "5a3a1a", "ladder", "wood", "village");
            DecoItem("ladder_tall", "Tall Ladder", CatYard, PlaceholderShape.Yard, 0.5f, 4, 17, DecoMaterial.None, "a9743d", "5a3a1a", "ladder", "wood", "siege");
            DecoItem("rope_coil", "Rope Coil", CatYard, PlaceholderShape.Yard, 1, 1, 18, DecoMaterial.None, "c9b27a", "8a5a2a", "rope", "dock", "storage");
            DecoItem("bucket", "Bucket", CatYard, PlaceholderShape.Yard, 0.5f, 0.5f, 19, DecoMaterial.None, "4a86c0", "a9743d", "bucket", "well", "water", "village");
            DecoItem("grindstone", "Grindstone", CatYard, PlaceholderShape.Yard, 1, 1, 20, DecoMaterial.None, "8f8c94", "5a3a1a", "grindstone", "smith", "forge", "stone");
            DecoItem("beehive", "Beehive", CatYard, PlaceholderShape.Yard, 1, 1, 21, DecoMaterial.None, "d8b85a", "8a6a2a", "beehive", "garden", "farm", "honey");
            DecoItem("dovecote", "Dovecote", CatYard, PlaceholderShape.Yard, 1, 2, 22, DecoMaterial.Plaster, "efe7d6", "5a6470", "dovecote", "birds", "garden", "farm");
            DecoItem("chopping_block", "Chopping Block", CatYard, PlaceholderShape.Yard, 1, 1, 23, DecoMaterial.None, "a9743d", "5a3a1a", "axe", "wood", "village", "yard");

            DecoItem("fence_wood", "Wooden Fence 2×1", CatYard, PlaceholderShape.Fence, 2, 1, 0, DecoMaterial.None, "a9743d", "5a3a1a", "fence", "village", "farm", "wood");
            DecoItem("fence_wood_long", "Wooden Fence 4×1", CatYard, PlaceholderShape.Fence, 4, 1, 0, DecoMaterial.None, "a9743d", "5a3a1a", "fence", "village", "farm", "wood");
            DecoItem("fence_iron", "Iron Railing 2×1", CatYard, PlaceholderShape.Fence, 2, 1, 1, DecoMaterial.None, "4a4c56", "2a2a30", "fence", "railing", "iron", "graveyard", "castle");
            DecoItem("fence_iron_long", "Iron Railing 4×1", CatYard, PlaceholderShape.Fence, 4, 1, 1, DecoMaterial.None, "4a4c56", "2a2a30", "fence", "railing", "iron", "graveyard", "castle");
            DecoItem("fence_gate", "Wooden Gate", CatYard, PlaceholderShape.Fence, 1.5f, 1, 2, DecoMaterial.None, "a9743d", "5a3a1a", "gate", "fence", "village", "farm", "wood");
            DecoItem("wall_low_cobble", "Low Cobble Wall 2×½", CatYard, PlaceholderShape.Fence, 2, 0.5f, 3, DecoMaterial.Cobble, "8a847c", "5c5963", "wall", "garden", "village", "stone");
            DecoItem("wall_low_mossy", "Low Mossy Wall 2×½", CatYard, PlaceholderShape.Fence, 2, 0.5f, 3, DecoMaterial.MossyStone, "7f8a7a", "4a5a4a", "wall", "garden", "ruins", "stone");
            DecoItem("wall_low_long", "Low Cobble Wall 4×½", CatYard, PlaceholderShape.Fence, 4, 0.5f, 3, DecoMaterial.Cobble, "8a847c", "5c5963", "wall", "garden", "village", "stone");
            DecoItem("wattle_fence", "Wattle Fence 2×1", CatYard, PlaceholderShape.Fence, 2, 1, 4, DecoMaterial.None, "a9743d", "5a3a1a", "fence", "wattle", "farm", "village");

            DecoItem("ballista", "Ballista", CatYard, PlaceholderShape.Siege, 2, 1.5f, 0, DecoMaterial.None, "a9743d", "4a4c56", "siege", "ballista", "war", "weapon");
            DecoItem("catapult", "Catapult", CatYard, PlaceholderShape.Siege, 2, 2, 1, DecoMaterial.None, "a9743d", "4a4c56", "siege", "catapult", "trebuchet", "war");
            DecoItem("siege_ladder", "Siege Ladder", CatYard, PlaceholderShape.Siege, 1, 4, 2, DecoMaterial.None, "a9743d", "4a4c56", "siege", "ladder", "war", "wood");
            DecoItem("battering_ram", "Battering Ram", CatYard, PlaceholderShape.Siege, 3, 2, 3, DecoMaterial.None, "a9743d", "4a4c56", "siege", "ram", "war", "gate");
            DecoItem("tent_red", "Tent (red)", CatYard, PlaceholderShape.Siege, 2, 1.5f, 4, DecoMaterial.None, "b8302c", "e0b64a", "tent", "camp", "war", "army");
            DecoItem("tent_blue", "Tent (blue)", CatYard, PlaceholderShape.Siege, 2, 1.5f, 4, DecoMaterial.None, "2c4fb8", "e0b64a", "tent", "camp", "war", "army");
            DecoItem("tent_green", "Tent (green)", CatYard, PlaceholderShape.Siege, 2, 1.5f, 4, DecoMaterial.None, "2f8a3a", "f4efe4", "tent", "camp", "war", "army");
            DecoItem("tent_canvas", "Canvas Tent", CatYard, PlaceholderShape.Siege, 2, 1.5f, 4, DecoMaterial.None, "d9d2bd", "8a6a40", "tent", "camp", "travel", "market");
            DecoItem("pavilion_red", "Pavilion (red & gold)", CatYard, PlaceholderShape.Siege, 3, 2.5f, 5, DecoMaterial.None, "b8302c", "e0b64a", "pavilion", "tent", "tournament", "royal");
            DecoItem("pavilion_blue", "Pavilion (blue & white)", CatYard, PlaceholderShape.Siege, 3, 2.5f, 5, DecoMaterial.None, "2c4fb8", "f4efe4", "pavilion", "tent", "tournament", "royal");
            DecoItem("pavilion_purple", "Pavilion (purple & gold)", CatYard, PlaceholderShape.Siege, 3, 2.5f, 5, DecoMaterial.None, "6a3cff", "e0b64a", "pavilion", "tent", "tournament", "royal");
            DecoItem("siege_tower", "Siege Tower", CatYard, PlaceholderShape.Siege, 2, 4, 6, DecoMaterial.None, "5a3a1a", "4a4c56", "siege", "tower", "war", "wood");
            DecoItem("mantlet", "Mantlet", CatYard, PlaceholderShape.Siege, 1.5f, 1.5f, 7, DecoMaterial.None, "a9743d", "4a4c56", "siege", "shield", "war", "wood");
            DecoItem("supply_cart", "Supply Cart", CatYard, PlaceholderShape.Siege, 2, 1.5f, 8, DecoMaterial.None, "a9743d", "4a4c56", "cart", "barrel", "camp", "supply");
        }
    }
}
