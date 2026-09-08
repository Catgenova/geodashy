using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Resolves art for objects, mounts, backgrounds and ground. Looks in Resources first
    /// (so artists can drop in real sprites) and falls back to procedural placeholders.
    /// </summary>
    public static class SpriteLibrary
    {
        static readonly Dictionary<string, Sprite> resourceCache = new Dictionary<string, Sprite>();
        static Material spriteMaterial;

        /// <summary>Unlit sprite material that works under URP 2D without lights.</summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial != null) return spriteMaterial;
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                spriteMaterial = shader != null ? new Material(shader) : null;
                return spriteMaterial;
            }
        }

        static Sprite LoadResource(string path)
        {
            if (resourceCache.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>(path);
            resourceCache[path] = s;
            return s;
        }

        public static Sprite ForObject(ObjectDefinition def)
        {
            var real = LoadResource("Sprites/" + def.SpriteId);
            return real != null ? real : PlaceholderSpriteFactory.ForDefinition(def);
        }

        public static Sprite ForMount(MountDefinition m)
        {
            var real = LoadResource("Sprites/mount_" + m.id);
            return real != null ? real : PlaceholderSpriteFactory.ForMount(m);
        }

        public static Sprite ForBackgroundLayer(BackgroundLayerDefinition layer)
        {
            var real = LoadResource("Backgrounds/" + layer.id);
            return real != null ? real : BackgroundArt.ForLayer(layer);
        }

        public static Sprite ForGround(GroundTheme g)
        {
            var real = LoadResource("Backgrounds/ground_" + g.id);
            return real != null ? real : PlaceholderSpriteFactory.Ground(g);
        }

        public static void ApplyMaterial(SpriteRenderer sr)
        {
            var m = SpriteMaterial;
            if (m != null) sr.sharedMaterial = m;
        }

        public static SpriteRenderer CreateRenderer(string name, Transform parent, Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            ApplyMaterial(sr);
            return sr;
        }
    }
}
