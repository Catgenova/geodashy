using System;
using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Automatic scenery along the ground line (torches, banners, bushes...) chosen per background theme.
    /// Purely decorative, deterministic per position, skipped where the level has objects nearby.
    /// </summary>
    public class GroundProps : MonoBehaviour
    {
        public const int Sorting = -4500;
        public const float Spacing = 7f;

        public Camera targetCamera;
        public LevelSettings settings;
        /// <summary>Returns true when a level object sits close to this x, so no prop is placed there.</summary>
        public Func<float, bool> isOccupied;

        readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();

        static readonly Dictionary<string, string[]> propsByTheme = new Dictionary<string, string[]>
        {
            { "castle", new[] { "torch", "banner_red", "statue_knight", "barrel", "banner_blue", "crate" } },
            { "forest", new[] { "bush", "tree_oak", "mushroom_red", "tree_pine", "bush", "rune_stone" } },
            { "dungeon", new[] { "chain", "skull", "torch", "cobweb", "barrel", "chain" } },
            { "cavern", new[] { "crystal_cluster", "mushroom_glow", "rune_stone", "crystal_cluster", "skull" } },
            { "desert", new[] { "barrel", "rune_stone", "crate", "pillar", "skull" } },
            { "sky", new[] { "cloud_small", "flag_pole", "banner_blue", "pillar" } },
            { "volcano", new[] { "skull", "rune_stone", "torch", "crystal_cluster" } },
            { "frost", new[] { "tree_pine", "rune_stone", "tree_pine", "crate" } },
            { "swamp", new[] { "mushroom_red", "tree_dead", "bush", "skull", "mushroom_glow" } },
            { "haunted", new[] { "tree_dead", "skull", "cobweb", "banner_red", "statue_knight" } },
        };

        public static GroundProps Create(Transform parent, Camera cam, LevelSettings settings, Func<float, bool> isOccupied)
        {
            var go = new GameObject("Ground Scenery");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<GroundProps>();
            p.targetCamera = cam;
            p.settings = settings;
            p.isOccupied = isOccupied;
            return p;
        }

        static int Hash(int k)
        {
            unchecked
            {
                int h = k * 73856093 ^ 19349663;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        void LateUpdate()
        {
            if (targetCamera == null || settings == null) return;
            if (!settings.groundProps)
            {
                foreach (var sr in pool) sr.enabled = false;
                return;
            }
            if (!propsByTheme.TryGetValue(settings.backgroundTheme, out var props)) props = propsByTheme["castle"];
            float halfW = targetCamera.orthographicSize * targetCamera.aspect;
            float camX = targetCamera.transform.position.x;
            float groundY = settings.groundY;
            int k0 = Mathf.FloorToInt((camX - halfW - 4f) / Spacing);
            int k1 = Mathf.CeilToInt((camX + halfW + 4f) / Spacing);
            int used = 0;
            for (int k = k0; k <= k1; k++)
            {
                int h = Hash(k);
                if (k < 1) continue; // keep the spawn area clean
                if ((h & 7) == 0) continue; // occasional gaps
                var def = ObjectCatalog.Get(props[(h >> 3) % props.Length]);
                if (def == null) continue;
                float x = k * Spacing + ((h >> 8) % 300) / 100f - 1.5f;
                if (isOccupied != null && isOccupied(x)) continue;
                SpriteRenderer sr;
                if (used < pool.Count) sr = pool[used];
                else
                {
                    sr = SpriteLibrary.CreateRenderer("prop", transform, null, Sorting);
                    pool.Add(sr);
                }
                used++;
                sr.enabled = true;
                sr.sprite = SpriteLibrary.ForObject(def);
                sr.flipX = ((h >> 20) & 1) == 1;
                float scale = 0.8f + ((h >> 12) % 40) / 100f;
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                sr.transform.position = new Vector3(x, groundY + def.height * scale * 0.5f, 0f);
                var tint = Color.Lerp(settings.backgroundColor, Color.white, 0.55f);
                tint.a = 0.9f;
                sr.color = tint;
            }
            for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
        }
    }
}
