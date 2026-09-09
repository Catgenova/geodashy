using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// A short procedural level seeded by the date, so everyone gets the same fresh run each day. Built from
    /// hand-tuned pattern blocks (spike rows, gaps, rune hops, pillars, shroom launches, a mount section) laid
    /// on the beat; the seed also picks the theme, song and mount.
    /// </summary>
    public static class DailyQuest
    {
        public static string TodayKey => DateTime.UtcNow.ToString("yyyyMMdd");
        public static string IdFor(string dayKey) => "daily_" + dayKey;

        class Rng
        {
            uint s;
            public Rng(int seed) { s = (uint)seed * 2654435761u + 0x9E3779B9u; if (s == 0) s = 1; }
            public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }
            public int Int(int n) => n <= 0 ? 0 : Mathf.Min(n - 1, (int)(Next() * n));
            public float Range(float a, float b) => a + (b - a) * Next();
            public bool Chance(float p) => Next() < p;
        }

        public static LevelData Generate(string dayKey)
        {
            int seed = 0;
            foreach (var ch in dayKey) seed = seed * 31 + ch;
            var rng = new Rng(seed);
            var level = LevelData.CreateNew("Daily Quest " + dayKey.Substring(0, 4) + "-" + dayKey.Substring(4, 2) + "-" + dayKey.Substring(6, 2));
            level.id = IdFor(dayKey);
            level.author = "The Realm";
            level.description = "Today's quest, the same for every rider. Champion only: no waystones, one clean run.";
            string[] themes = ThemeCatalog.BackgroundIds;
            level.settings.backgroundTheme = themes[rng.Int(themes.Length)];
            level.settings.backgroundColor = ThemeCatalog.GetBackground(level.settings.backgroundTheme).skyBottom;
            string[] grounds = ThemeCatalog.GroundIds;
            level.settings.groundTheme = grounds[rng.Int(grounds.Length)];
            var songs = LevelStorage.StarterSongIds();
            if (songs.Length > 0) level.settings.songId = songs[rng.Int(songs.Length)];
            level.settings.bpm = 120f;
            level.settings.difficultyTag = rng.Chance(0.5f) ? "normal" : "hard";
            string[] blocks = { "castle_stone", "cobble", "moss_stone", "sand_block", "marble", "ice_block", "obsidian" };
            string block = blocks[rng.Int(blocks.Length)];
            string[] spikes = { "iron_spike", "wood_stake", "ice_spike", "bone_spike" };
            string spike = spikes[rng.Int(spikes.Length)];

            int uid = 1;
            LevelObject Put(string type, float x, float y, float rot = 0f, int z = 0)
            {
                var o = new LevelObject { uid = uid++, type = type, x = x, y = y, rotation = rot, zLayer = z };
                level.objects.Add(o);
                return o;
            }
            float beat = MountCatalog.Speed(1) * 60f / level.settings.bpm;   // one beat in blocks at normal speed
            float x = 8f;
            int sections = 10 + rng.Int(5);
            bool onDragon = false;
            for (int s = 0; s < sections; s++)
            {
                int pattern = rng.Int(onDragon ? 3 : 7);
                if (onDragon)
                {
                    switch (pattern)
                    {
                        case 0:   // pillar pair
                            Put(block + "_tall", x, 1f); Put(block + "_tall", x, 8f);
                            Put(spike, x, 2.5f); Put(spike, x, 6.5f, 180f);
                            x += beat * 3f;
                            break;
                        case 1:   // stalactite ceiling run
                            for (int i = 0; i < 4; i++) Put("stalactite_hazard", x + i * beat, 9.5f);
                            for (int i = 0; i < 4; i++) if (rng.Chance(0.5f)) Put(spike, x + i * beat + beat / 2f, 0.5f);
                            x += beat * 5f;
                            break;
                        default:  // coins in a wave
                            for (int i = 0; i < 6; i++) Put("gold_coin", x + i * beat * 0.5f, 3.5f + Mathf.Sin(i * 0.9f) * 2f);
                            x += beat * 4f;
                            break;
                    }
                    if (s == sections - 3) { Put("portal_horse", x, 2f); onDragon = false; x += beat * 2f; }
                    continue;
                }
                switch (pattern)
                {
                    case 0:   // spike row on the ground
                    {
                        int n = 1 + rng.Int(3);
                        for (int i = 0; i < n; i++) Put(spike, x + i, 0.5f);
                        x += n + beat * 2f;
                        break;
                    }
                    case 1:   // step up, step down
                        Put(block, x, 0.5f); Put(block + "_wide", x + 2f, 1.5f); Put(spike, x + 2f, 2.5f);
                        Put(block, x + 4f, 0.5f);
                        x += 6f + beat;
                        break;
                    case 2:   // rune hop over a pit of spikes
                        for (int i = 0; i < 4; i++) Put(spike, x + 1f + i, 0.5f);
                        Put("rune_wind", x + 2.5f, 3f);
                        Put("gold_coin", x + 2.5f, 4.5f);
                        x += 6f + beat;
                        break;
                    case 3:   // shroom launch onto a ledge
                        Put("shroom_spring", x, 0.5f);
                        Put(block + "_wide", x + 3f, 3f); Put(spike, x + 3f, 4f);
                        Put("gem", x + 3.5f, 5.5f);
                        x += 6f + beat * 2f;
                        break;
                    case 4:   // pillar with wall spikes
                        Put(block + "_tall", x, 1f); Put(spike, x - 1f, 0.5f); Put(spike, x + 1f, 0.5f);
                        x += 3f + beat * 2f;
                        break;
                    case 5:   // speed change and a long spike stretch
                        Put("portal_speed_2", x, 2f);
                        for (int i = 0; i < 5; i++) if (i % 2 == 0) Put(spike, x + 3f + i * 1.5f, 0.5f);
                        Put("portal_speed_1", x + 11f, 2f);
                        x += 14f;
                        break;
                    default:  // dragon section starts
                        Put("portal_dragon", x, 2f);
                        onDragon = true;
                        x += beat * 2f;
                        break;
                }
                if (rng.Chance(0.35f)) Put("torch", x - beat, 2.5f, 0f, 2);
                if (rng.Chance(0.3f)) Put("banner_red", x - beat * 0.5f, 3.5f, 0f, 2);
            }
            if (onDragon) { Put("portal_horse", x, 2f); x += beat * 2f; }
            Put("finish_gate", x + 4f, 2f);
            level.nextUid = uid;
            return level;
        }
    }
}
