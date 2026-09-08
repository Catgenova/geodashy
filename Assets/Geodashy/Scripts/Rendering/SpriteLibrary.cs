using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Resolves art for objects, mounts, backgrounds and ground. Looks in Resources first
    /// (so artists can drop in real sprites) and falls back to procedural placeholders.
    /// </summary>
    /// <summary>Frame sets for a mount: a run cycle used on the ground and a jump cycle used in the air.</summary>
    public class MountAnimation
    {
        public Sprite[] run;
        public Sprite[] jump;
        public float runFps = 14f;
        public float jumpFps = 20f;
        /// <summary>True when the air clip loops (flight) instead of playing once and holding the last frame (a jump).</summary>
        public bool loopInAir;
    }

    public static class SpriteLibrary
    {
        static readonly Dictionary<string, Sprite> resourceCache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, MountAnimation> mountAnimations = new Dictionary<string, MountAnimation>();
        static Dictionary<string, Texture2D> spriteTextures;
        static Material spriteMaterial;

        /// <summary>Art height of an animated mount frame in world units (the hitbox stays 1x1).</summary>
        public const float MountFrameHeightUnits = 1.3f;

        static Dictionary<string, Texture2D> SpriteTextures
        {
            get
            {
                if (spriteTextures != null) return spriteTextures;
                spriteTextures = new Dictionary<string, Texture2D>();
                foreach (var t in Resources.LoadAll<Texture2D>("Sprites")) spriteTextures[t.name] = t;
                return spriteTextures;
            }
        }

        /// <summary>Finds a sheet named {prefix}_{frames} (e.g. mount_horse_run_9) and returns its frame count, or 0.</summary>
        static Texture2D FindSheet(string prefix, out int frames)
        {
            frames = 0;
            foreach (var kv in SpriteTextures)
            {
                if (!kv.Key.StartsWith(prefix + "_")) continue;
                var tail = kv.Key.Substring(prefix.Length + 1);
                if (int.TryParse(tail, out var n) && n > 0)
                {
                    frames = n;
                    return kv.Value;
                }
            }
            return null;
        }

        static Sprite[] Slice(Texture2D tex, int frames, float heightUnits)
        {
            float fw = tex.width / (float)frames;
            float ppu = tex.height / heightUnits;
            // pivot so the bottom of the art sits on the hitbox bottom (0.5 units below centre)
            var pivot = new Vector2(0.5f, Mathf.Clamp01(0.5f / heightUnits));
            var result = new Sprite[frames];
            for (int i = 0; i < frames; i++)
            {
                result[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
                result[i].name = tex.name + "_" + i;
            }
            return result;
        }

        /// <summary>
        /// Animation sheets for a mount, or null when none exist. Drop horizontal strips into
        /// Resources/Sprites named mount_{id}_run_{frames}.png and mount_{id}_jump_{frames}.png.
        /// </summary>
        public static MountAnimation ForMountAnimation(MountDefinition m)
        {
            if (mountAnimations.TryGetValue(m.id, out var cached)) return cached;
            MountAnimation anim = null;
            var runTex = FindSheet("mount_" + m.id + "_run", out var runFrames);
            var jumpTex = FindSheet("mount_" + m.id + "_jump", out var jumpFrames);
            var flyTex = FindSheet("mount_" + m.id + "_fly", out var flyFrames);
            if (runTex != null || jumpTex != null || flyTex != null)
            {
                anim = new MountAnimation();
                if (runTex != null) anim.run = Slice(runTex, runFrames, MountFrameHeightUnits);
                if (jumpTex != null) anim.jump = Slice(jumpTex, jumpFrames, MountFrameHeightUnits);
                if (flyTex != null)
                {
                    // flying mounts: one looping cycle whether "grounded" or not
                    var fly = Slice(flyTex, flyFrames, MountFrameHeightUnits);
                    if (anim.run == null) anim.run = fly;
                    if (anim.jump == null) anim.jump = fly;
                    anim.jumpFps = anim.runFps = 12f;
                    anim.loopInAir = true;
                }
                if (anim.run == null) anim.run = anim.jump;
                if (anim.jump == null) anim.jump = anim.run;
            }
            mountAnimations[m.id] = anim;
            return anim;
        }

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
            var anim = ForMountAnimation(m);
            if (anim != null && anim.run != null && anim.run.Length > 0) return anim.run[0];
            var real = LoadResource("Sprites/mount_" + m.id);
            return real != null ? real : PlaceholderSpriteFactory.ForMount(m);
        }

        /// <summary>True when the mount uses real animation frames rather than a placeholder.</summary>
        public static bool MountIsAnimated(MountDefinition m) => ForMountAnimation(m) != null;

        /// <summary>
        /// Horizontal scale sign to make the mount face the direction of travel. Placeholders are drawn facing right;
        /// animation sheets are, by convention, authored facing left (like the shipped knight) and are mirrored.
        /// </summary>
        public static float MountFacing(MountDefinition m) => MountIsAnimated(m) ? -1f : 1f;

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
