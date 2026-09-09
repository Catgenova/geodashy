using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Gameplay
{
    /// <summary>
    /// The run's world-space eye candy, kept out of GameRunner: beat-driven glows on lights and runes, ambient
    /// emitters on decorations (sparks, smoke, fireflies, drips, petals, leaves, bubbles), rune afterimages and
    /// pad squash lines, near-miss shockwaves, mount silhouettes, god rays and the finish celebration.
    /// Everything here is cosmetic and skipped or softened under Reduce flashing.
    /// </summary>
    public class PlayEffects
    {
        readonly GameRunner runner;
        readonly LevelWorld world;
        readonly Transform root;
        readonly ParticleBurst particles;
        readonly ParticleBurst ambient;
        readonly Camera cam;
        readonly LevelData level;

        // ---- glows -----------------------------------------------------------------
        class Glow
        {
            public LevelObjectView view;
            public SpriteRenderer sr;
            public Color color;
            public float size, phase;
            public bool flicker;
        }
        readonly List<Glow> glows = new List<Glow>();

        // ---- ambient emitters --------------------------------------------------------
        enum Emitter { None, Sparks, Smoke, Fireflies, Drips, Petals, Leaves, Bubbles, Candle }
        class Ambient
        {
            public LevelObjectView view;
            public Emitter kind;
            public float timer;
        }
        readonly List<Ambient> ambients = new List<Ambient>();

        // ---- one-shot world sprites (silhouettes, rings, squash lines) ------------------
        class Puff
        {
            public SpriteRenderer sr;
            public float life, maxLife;
            public Vector2 pos, vel;
            public float scaleFrom, scaleTo, aspect;
            public Color color;
            public bool rainbow;
            public bool active;
        }
        readonly List<Puff> puffs = new List<Puff>();

        // ---- afterimage stamping after a rune -------------------------------------------
        float stampTimer, stampWindow;
        Color stampColor;

        // ---- god rays ------------------------------------------------------------------
        SpriteRenderer rays;
        float rayBase, rayBoost;
        readonly List<LevelObjectView> rayWindows = new List<LevelObjectView>();

        // ---- finish celebration ---------------------------------------------------------
        float finishTimer = -1f;
        LevelObjectView finishView;
        float confettiTimer;

        public float beatEnergy;

        public PlayEffects(GameRunner runner, LevelWorld world, Transform root, ParticleBurst particles, Camera cam, LevelData level)
        {
            this.runner = runner;
            this.world = world;
            this.root = root;
            this.particles = particles;
            this.cam = cam;
            this.level = level;
            ambient = ParticleBurst.Create(root, "Ambient Particles", 30);
            Collect();
            BuildRays();
        }

        static bool HasTag(ObjectDefinition d, params string[] tags)
        {
            foreach (var t in d.tags) foreach (var want in tags) if (t == want) return true;
            return false;
        }

        void Collect()
        {
            glows.Clear();
            ambients.Clear();
            rayWindows.Clear();
            foreach (var v in world.all)
            {
                var d = v.def;
                if (d == null) continue;
                bool fire = HasTag(d, "fire", "brazier", "campfire") || d.id == "torch";
                bool candle = HasTag(d, "candle", "lantern", "sconce");
                bool light = HasTag(d, "light") || d.id.StartsWith("window_") && d.id.EndsWith("_lit") || d.id == "window_stained";
                bool rune = d.kind == ObjectKind.Orb || d.kind == ObjectKind.Portal;
                if (fire || candle || light || rune)
                {
                    var g = new Glow { view = v, color = d.primaryColor, phase = Random.value * 6.28f, flicker = fire || candle };
                    g.size = rune ? Mathf.Max(v.Size.x, v.Size.y) * 2.2f : (fire ? 3.2f : (candle ? 2f : 2.6f));
                    g.sr = SpriteLibrary.CreateRenderer("glow", v.transform.parent, PlaceholderSpriteFactory.SoftGlow(), LevelObjectView.SortingBase + v.data.zLayer * 100 + v.data.zOrder - 1);
                    g.sr.color = new Color(g.color.r, g.color.g, g.color.b, 0f);
                    g.sr.enabled = false;
                    glows.Add(g);
                }
                Emitter e = Emitter.None;
                if (fire) e = Emitter.Sparks;
                else if (HasTag(d, "chimney", "smoke")) e = Emitter.Smoke;
                else if (HasTag(d, "festival", "garland") && HasTag(d, "light")) e = Emitter.Fireflies;
                else if (candle) e = Emitter.Candle;
                else if (HasTag(d, "stalactite", "cave") && d.kind == ObjectKind.Decoration) e = Emitter.Drips;
                else if (d.id == "tree_apple" || d.id == "tree_hawthorn" || HasTag(d, "blossom", "cherry", "petal")) e = Emitter.Petals;
                else if (HasTag(d, "fountain", "well", "water", "waterfall")) e = Emitter.Bubbles;
                else if (d.kind == ObjectKind.Decoration && (d.id.StartsWith("tree_") || HasTag(d, "forest") && HasTag(d, "nature"))) e = Emitter.Leaves;
                if (e != Emitter.None && d.kind == ObjectKind.Decoration) ambients.Add(new Ambient { view = v, kind = e, timer = Random.value });
                if (HasTag(d, "glass", "window") || d.id.StartsWith("window")) rayWindows.Add(v);
            }
        }

        void BuildRays()
        {
            string theme = level.settings.backgroundTheme ?? "";
            rayBase = theme == "forest" || theme == "haunted" ? 0.16f : (theme == "castle" || theme == "dungeon" ? 0.1f : (theme == "cavern" ? 0.08f : 0f));
            if (rayBase <= 0f && rayWindows.Count == 0) return;
            rays = SpriteLibrary.CreateRenderer("God Rays", root, PlaceholderSpriteFactory.LightRays(), ParallaxBackground.NearSorting + 500);
            rays.color = new Color(1f, 0.95f, 0.8f, 0f);
        }

        Puff TakePuff()
        {
            foreach (var p in puffs) if (!p.active) return p;
            var np = new Puff { sr = SpriteLibrary.CreateRenderer("fx", root, null, 45) };
            puffs.Add(np);
            return np;
        }

        void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float scaleFrom, float scaleTo, float aspect, float life, Color color, int order, bool rainbow = false, float rotation = 0f, bool flipX = false)
        {
            if (Accessibility.ReduceFlash) color.a *= 0.6f;
            var p = TakePuff();
            p.sr.sprite = sprite;
            p.sr.sortingOrder = order;
            p.sr.flipX = flipX;
            p.sr.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            p.pos = pos;
            p.vel = vel;
            p.scaleFrom = scaleFrom;
            p.scaleTo = scaleTo;
            p.aspect = aspect;
            p.life = p.maxLife = life;
            p.color = color;
            p.rainbow = rainbow;
            p.active = true;
            p.sr.enabled = true;
            p.sr.color = color;
            p.sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            p.sr.transform.localScale = new Vector3(scaleFrom * aspect, scaleFrom, 1f);
        }

        // =====================================================================
        // public hooks
        // =====================================================================

        /// <summary>Ground puff coloured by the ground theme, sized by how hard the landing was.</summary>
        public void Landing(Vector2 feet, float up, float fallSpeed, string mountId)
        {
            var g = ThemeCatalog.GetGround(level.settings.groundTheme);
            var dust = Color.Lerp(g.top, Color.white, 0.35f);
            dust.a = 0.75f;
            int count = 5 + Mathf.RoundToInt(Mathf.Clamp(fallSpeed, 0f, 40f) * 0.35f);
            float speed = 1.8f + Mathf.Clamp(fallSpeed, 0f, 40f) * 0.09f;
            particles.Emit(feet, dust, count, speed, 0.4f, 0.09f, 4f, up > 0 ? 90f : 270f, 160f);
            // heavier riders throw the darker body of the ground too
            if (fallSpeed > 10f || mountId == "boar" || mountId == "cart")
            {
                var clod = g.body;
                clod.a = 0.9f;
                particles.Emit(feet, clod, 3 + Mathf.RoundToInt(fallSpeed * 0.15f), speed * 0.9f, 0.5f, 0.07f, 9f, up > 0 ? 90f : 270f, 120f);
            }
            if (mountId == "cart") Skid(feet, up, 1, new Color(1f, 0.75f, 0.3f), 0.25f);
        }

        /// <summary>A streak of particles trailing behind the feet: boar flips and cart braking.</summary>
        public void Skid(Vector2 feet, float up, int direction, Color color, float strength)
        {
            var g = ThemeCatalog.GetGround(level.settings.groundTheme);
            var c = Color.Lerp(g.top, color, 0.5f);
            c.a = 0.85f;
            int n = Mathf.RoundToInt(6 + 10 * strength);
            particles.Emit(feet, c, n, 3.5f + 3f * strength, 0.3f, 0.06f, 6f, direction > 0 ? 180f : 0f, 14f);
            // the skid mark itself: a flat streak that stretches and fades
            var mark = new Color(g.body.r, g.body.g, g.body.b, 0.55f);
            Spawn(PlaceholderSpriteFactory.WhiteSquare(), feet - new Vector2(direction * 0.35f, 0f), new Vector2(-direction * 0.6f, 0f), 0.35f, 0.9f, 3f, 0.35f, mark, 12);
        }

        /// <summary>Wipe keyed to the gate crossed; the HUD draws the screen-space half, this stamps the world.</summary>
        public void PortalCrossed(LevelObjectView portal, PlayerController player, PlayHUD hud)
        {
            var col = portal.def.primaryColor;
            switch (portal.def.portalType)
            {
                case PortalType.GravityFlip:
                case PortalType.GravityNormal:
                    hud.GravityWave(col);
                    Ring(portal.WorldPosition, col, 4f, 0.5f);
                    break;
                case PortalType.Speed:
                    hud.SpeedStreaks(col, player.direction);
                    player.stretch = 0.45f;
                    break;
                case PortalType.Mount:
                    // the old silhouette flares out while the new mount pops in
                    if (player.CurrentSprite != null)
                    {
                        var size = player.Size;
                        float scale = size.y / Mathf.Max(0.01f, player.CurrentSprite.bounds.size.y);
                        Spawn(player.CurrentSprite, player.position, Vector2.zero, scale, scale * 2.4f, 1f, 0.45f, new Color(1f, 1f, 1f, 0.9f), 6, false, 0f, player.direction * SpriteLibrary.MountFacing(player.mount) < 0);
                        Spawn(player.CurrentSprite, player.position, Vector2.zero, scale, scale * 1.6f, 1f, 0.3f, new Color(col.r, col.g, col.b, 0.7f), 7, false, 0f, player.direction * SpriteLibrary.MountFacing(player.mount) < 0);
                    }
                    Ring(portal.WorldPosition, Color.white, 5f, 0.4f);
                    break;
                case PortalType.Teleport:
                    Ring(portal.WorldPosition, col, 3f, 0.4f);
                    break;
                default:
                    Ring(portal.WorldPosition, col, 2.5f, 0.35f);
                    break;
            }
        }

        /// <summary>Expanding world-space ring.</summary>
        public void Ring(Vector2 pos, Color color, float finalSize, float life, bool rainbow = false)
        {
            Spawn(PlaceholderSpriteFactory.Ring(), pos, Vector2.zero, 0.3f, finalSize, 1f, life, new Color(color.r, color.g, color.b, 0.9f), 48, rainbow);
        }

        /// <summary>Rune activated: a ring at the rune and ghost stamps of the mount along the launch arc.</summary>
        public void RuneActivated(LevelObjectView orb, int combo)
        {
            var col = orb.def.primaryColor;
            Ring(orb.WorldPosition, col, combo >= 3 ? 4.5f : 3f, 0.45f, combo >= 3);
            stampWindow = 0.32f;
            stampTimer = 0f;
            stampColor = combo >= 3 ? new Color(1f, 1f, 1f, 0.55f) : new Color(col.r, col.g, col.b, 0.5f);
        }

        /// <summary>Pad touched: a spring-shaped squash line at the pad.</summary>
        public void PadTouched(LevelObjectView pad, bool flipped)
        {
            var col = pad.def.primaryColor;
            var pos = pad.WorldPosition;
            float dir = flipped ? -1f : 1f;
            // three flattening bars rising off the pad like a coil letting go
            for (int i = 0; i < 3; i++)
            {
                float y = pos.y + dir * (0.15f + i * 0.22f);
                Spawn(PlaceholderSpriteFactory.WhiteSquare(), new Vector2(pos.x, y), new Vector2(0f, dir * (2.5f + i)), 0.18f, 0.04f, 4f + i * 2f, 0.28f + i * 0.05f, new Color(col.r, col.g, col.b, 0.8f - i * 0.2f), 46);
            }
        }

        /// <summary>Hazard skimmed: shockwave at the hazard, a camera nudge away from it and a red pulse on that screen edge.</summary>
        public void NearMiss(Vector2 hazardPos, PlayerController player, PlayCamera camera, PlayHUD hud)
        {
            var danger = HitboxOverlay.DangerColor;
            Ring(hazardPos, danger, 2.2f, 0.3f);
            particles.Emit(hazardPos, new Color(danger.r, danger.g, danger.b, 0.8f), 5, 3f, 0.25f, 0.05f, 2f);
            var away = player.position - hazardPos;
            camera.Nudge(away.sqrMagnitude > 0.0001f ? away.normalized * 0.12f : Vector2.zero, 0.12f);
            hud.DangerEdge(hazardPos - player.position);
        }

        /// <summary>Quest complete: confetti in your colours, the banner unfurls, the camera eases back.</summary>
        public void Finish(PlayerController player, PlayCamera camera)
        {
            finishTimer = 0f;
            confettiTimer = 0f;
            finishView = null;
            float best = float.MaxValue;
            foreach (var v in world.all)
            {
                if (v.def.kind != ObjectKind.Finish) continue;
                float d = Mathf.Abs(v.data.x - player.position.x);
                if (d < best)
                {
                    best = d;
                    finishView = v;
                }
            }
            camera.SetZoom(0.82f, 1.4f, Easing.EaseOut);
            Burst(player.position + new Vector2(0f, 1.5f), 26, 7f);
            if (finishView != null) Burst(finishView.WorldPosition + new Vector2(0f, finishView.Size.y * 0.5f), 20, 5f);
        }

        void Burst(Vector2 pos, int count, float speed)
        {
            var gold = new Color(1f, 0.85f, 0.3f);
            particles.Emit(pos, PlayerProfile.Primary, count, speed, 1.6f, 0.13f, 6f, 90f, 120f);
            particles.Emit(pos, PlayerProfile.Secondary, count * 2 / 3, speed * 0.9f, 1.6f, 0.11f, 6f, 90f, 130f);
            particles.Emit(pos, gold, count / 2, speed * 1.1f, 1.4f, 0.1f, 5f, 90f, 100f);
        }

        // =====================================================================
        // per frame
        // =====================================================================

        public void Update(float dt, PlayerController player, float halfWidth, float halfHeight)
        {
            float camX = cam.transform.position.x, camY = cam.transform.position.y;
            float now = Time.time;
            bool calm = Accessibility.ReduceFlash;
            float e = Mathf.Clamp01(beatEnergy);
            beatEnergy = Mathf.Max(0f, beatEnergy - dt * 2.2f);

            // glows
            float glowMargin = halfWidth + 4f;
            foreach (var g in glows)
            {
                var v = g.view;
                bool near = Mathf.Abs(v.data.x - camX) < glowMargin && !v.culled && v.runtimeActive && v.renderer2D.enabled;
                if (!near)
                {
                    if (g.sr.enabled) g.sr.enabled = false;
                    continue;
                }
                float a = 0.22f + 0.22f * e;
                if (g.flicker) a += 0.06f * Mathf.Sin(now * 13f + g.phase) + 0.04f * Mathf.Sin(now * 31f + g.phase * 2f);
                if (calm) a *= 0.6f;
                float s = g.size * (1f + 0.12f * e + (g.flicker ? 0.04f * Mathf.Sin(now * 9f + g.phase) : 0f));
                var pos = v.WorldPosition;
                g.sr.transform.position = new Vector3(pos.x, pos.y + (g.flicker ? v.Size.y * 0.15f : 0f), 0f);
                g.sr.transform.localScale = new Vector3(s, s, 1f);
                g.sr.color = new Color(g.color.r, g.color.g, g.color.b, a * v.runtimeAlpha);
                if (!g.sr.enabled) g.sr.enabled = true;
            }

            // ambient emitters
            float ambientMargin = halfWidth + 2f;
            foreach (var a in ambients)
            {
                var v = a.view;
                if (Mathf.Abs(v.data.x - camX) > ambientMargin || v.culled || !v.runtimeActive) continue;
                a.timer -= dt;
                if (a.timer > 0f) continue;
                var pos = v.WorldPosition;
                var size = v.Size;
                float rx = Random.Range(-size.x * 0.4f, size.x * 0.4f);
                switch (a.kind)
                {
                    case Emitter.Sparks:
                        a.timer = Random.Range(0.08f, 0.2f);
                        ambient.Emit(pos + new Vector2(rx * 0.5f, size.y * 0.3f), Color.Lerp(v.def.primaryColor, Color.white, 0.3f), 1, 1.4f, 0.7f, 0.05f, -1.8f, 90f, 40f);
                        break;
                    case Emitter.Candle:
                        a.timer = Random.Range(0.35f, 0.7f);
                        ambient.Emit(pos + new Vector2(rx * 0.3f, size.y * 0.4f), new Color(1f, 0.9f, 0.6f, 0.7f), 1, 0.5f, 0.9f, 0.035f, -0.6f, 90f, 25f);
                        break;
                    case Emitter.Smoke:
                        a.timer = Random.Range(0.18f, 0.3f);
                        ambient.Emit(pos + new Vector2(0f, size.y * 0.5f), new Color(0.8f, 0.8f, 0.85f, 0.35f), 1, 0.7f, 2.4f, 0.22f, -0.5f, 100f, 30f);
                        break;
                    case Emitter.Fireflies:
                        a.timer = Random.Range(0.25f, 0.6f);
                        ambient.Emit(pos + new Vector2(rx, Random.Range(-0.6f, 0.4f)), new Color(1f, 0.95f, 0.5f, 0.85f), 1, 0.35f, 2.2f, 0.045f, -0.05f, Random.Range(0f, 360f), 60f);
                        break;
                    case Emitter.Drips:
                        a.timer = Random.Range(0.9f, 2.5f);
                        ambient.Emit(pos + new Vector2(rx * 0.6f, -size.y * 0.5f), new Color(0.6f, 0.8f, 1f, 0.8f), 1, 0.2f, 0.9f, 0.05f, 9f, 270f, 5f);
                        break;
                    case Emitter.Petals:
                        a.timer = Random.Range(0.4f, 1.1f);
                        ambient.Emit(pos + new Vector2(rx, Random.Range(0f, size.y * 0.4f)), Color.Lerp(v.def.primaryColor, new Color(1f, 0.75f, 0.85f), 0.6f), 1, 0.6f, 2.6f, 0.06f, 0.35f, 200f, 60f);
                        break;
                    case Emitter.Leaves:
                        a.timer = Random.Range(0.8f, 2f);
                        ambient.Emit(pos + new Vector2(rx, Random.Range(0f, size.y * 0.4f)), Color.Lerp(v.def.primaryColor, new Color(0.7f, 0.5f, 0.2f), Random.value * 0.6f), 1, 0.7f, 2.8f, 0.07f, 0.45f, 210f, 50f);
                        break;
                    case Emitter.Bubbles:
                        a.timer = Random.Range(0.2f, 0.5f);
                        ambient.Emit(pos + new Vector2(rx * 0.7f, 0f), new Color(0.75f, 0.9f, 1f, 0.7f), 1, 0.8f, 0.8f, 0.045f, -1.2f, 90f, 30f);
                        break;
                }
            }

            // rune afterimages
            if (stampWindow > 0f)
            {
                stampWindow -= dt;
                stampTimer -= dt;
                if (stampTimer <= 0f && player.CurrentSprite != null && !player.dead)
                {
                    stampTimer = 0.07f;
                    float scale = player.Size.y / Mathf.Max(0.01f, player.CurrentSprite.bounds.size.y);
                    Spawn(player.CurrentSprite, player.position, Vector2.zero, scale, scale * 1.15f, 1f, 0.32f, stampColor, 5, false, player.transform.eulerAngles.z, player.direction * SpriteLibrary.MountFacing(player.mount) < 0);
                }
            }

            // one-shot sprites
            foreach (var p in puffs)
            {
                if (!p.active) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.active = false;
                    p.sr.enabled = false;
                    continue;
                }
                float t = 1f - p.life / p.maxLife;
                p.pos += p.vel * dt;
                float sc = Mathf.Lerp(p.scaleFrom, p.scaleTo, 1f - (1f - t) * (1f - t));
                p.sr.transform.position = new Vector3(p.pos.x, p.pos.y, 0f);
                p.sr.transform.localScale = new Vector3(sc * p.aspect, sc, 1f);
                var c = p.rainbow ? Color.HSVToRGB(Mathf.Repeat(now * 1.5f + t, 1f), 0.7f, 1f) : p.color;
                c.a = p.color.a * (1f - t);
                p.sr.color = c;
            }

            // god rays: base glow for shaded themes, brighter when passing windows or canopy gaps
            if (rays != null)
            {
                float boost = 0f;
                foreach (var w in rayWindows)
                {
                    if (w.culled || !w.runtimeActive) continue;
                    boost = Mathf.Max(boost, 1f - Mathf.Abs(w.data.x - player.position.x) / 4f);
                }
                rayBoost = Mathf.Lerp(rayBoost, Mathf.Clamp01(boost), 1f - Mathf.Exp(-3f * dt));
                float alpha = rayBase + 0.28f * rayBoost + 0.05f * e;
                if (calm) alpha *= 0.6f;
                rays.transform.position = new Vector3(camX + Mathf.Sin(now * 0.13f) * 0.6f, camY + halfHeight * 0.35f, 0f);
                float w2 = halfWidth * 2.4f, h2 = halfHeight * 2.4f;
                rays.transform.localScale = new Vector3(w2 / 4f, h2 / 4f, 1f);   // the sprite is 4 world units square
                rays.color = new Color(1f, 0.95f, 0.8f, alpha);
                rays.enabled = alpha > 0.01f;
            }

            // finish celebration: confetti keeps falling while the banner waves
            if (finishTimer >= 0f)
            {
                finishTimer += dt;
                confettiTimer -= dt;
                if (confettiTimer <= 0f && finishTimer < 3.5f)
                {
                    confettiTimer = 0.12f;
                    var top = new Vector2(camX + Random.Range(-halfWidth, halfWidth), camY + halfHeight + 0.5f);
                    var c = Random.value < 0.4f ? PlayerProfile.Primary : (Random.value < 0.6f ? PlayerProfile.Secondary : new Color(1f, 0.85f, 0.3f));
                    particles.Emit(top, c, 2, 1.2f, 2.6f, 0.12f, 2.2f, 270f, 60f);
                }
                if (finishView != null)
                {
                    float k = Mathf.Exp(-finishTimer * 1.1f);
                    finishView.visualWobble = Mathf.Sin(finishTimer * 11f) * 10f * k;
                    finishView.visualPulse = 0.08f * Mathf.Sin(finishTimer * 7f) * k;
                    finishView.ApplyTransform();
                }
            }
        }

        public void Reset()
        {
            finishTimer = -1f;
            stampWindow = 0f;
            if (finishView != null)
            {
                finishView.visualWobble = 0f;
                finishView.visualPulse = 0f;
                finishView.ApplyTransform();
                finishView = null;
            }
            foreach (var p in puffs)
            {
                p.active = false;
                p.sr.enabled = false;
            }
            ambient.Clear();
        }

        public void Destroy()
        {
            foreach (var g in glows) if (g.sr != null) Object.Destroy(g.sr.gameObject);
            glows.Clear();
            foreach (var p in puffs) if (p.sr != null) Object.Destroy(p.sr.gameObject);
            puffs.Clear();
            if (rays != null) Object.Destroy(rays.gameObject);
            if (ambient != null) Object.Destroy(ambient.gameObject);
        }
    }
}
