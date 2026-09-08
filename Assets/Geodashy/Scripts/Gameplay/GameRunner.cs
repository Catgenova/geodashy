using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Geodashy.Gameplay
{
    /// <summary>Runs a level: builds the world, drives the player, triggers, camera, HUD, deaths and completion.</summary>
    public class GameRunner : MonoBehaviour
    {
        public LevelData level;
        public LevelData pristine;
        public Camera cam;
        public ParallaxBackground background;
        public GroundRenderer ground;
        public LevelWorld world;
        public PlayerController player;
        public TriggerSystem triggers;
        public PlayCamera playCamera;
        public PlayHUD hud;
        public float finishX;
        public bool orbHoldActivates = true;
        /// <summary>Raised on every death: rider position, contact point, and what killed the rider.</summary>
        public event Action<Vector2, Vector2, string> DeathRecorded;

        Action onExit;
        Vector2 startPos;
        string startMount;
        int startSpeed;
        bool startFlipped, startMini;
        int attempts;
        int coins, totalCoins;
        int gems, totalGems, keys;
        bool paused;
        bool complete;
        float elapsed;
        AudioSource music;
        bool wasHeld;
        Transform worldRoot;
        HitboxOverlay deathOverlay;
        float deathTimer;
        ParticleBurst particles;
        float slowMo;
        int lastBeat = -1;
        public GroundProps groundProps;
        bool introActive;

        // ---- wisp trail ---------------------------------------------------------
        struct TrailPoint
        {
            public Vector2 pos;
            public float time;
        }

        const float TrailLife = 1.1f;
        readonly List<TrailPoint> trail = new List<TrailPoint>();
        HitboxOverlay trailRibbon;
        float trailSparkleTimer;
        Vector2 lastTrailDir;
        LevelObjectView pulsingPortal;
        float portalPulse;
        LevelStats stats;
        readonly List<float> sessionDeaths = new List<float>();
        bool fullRun;

        // ---- practice mode ---------------------------------------------------
        class Checkpoint
        {
            public PlayerController.Snapshot player;
            public TriggerSystem.Snapshot triggers;
            public LevelWorld.Snapshot world;
            public PlayCamera.Snapshot camera;
            public LevelSettings settings;
            public List<ColorChannel> colors;
            public float elapsed;
            public int coins, gems, keys;
            public bool ceiling;
            public GameObject marker;
        }

        public Difficulty difficulty = Difficulty.Checkpoints;
        /// <summary>Training difficulty (player-placed waystones).</summary>
        public bool practice => difficulty == Difficulty.Training;
        public bool autoCheckpoints = true;
        readonly List<LevelObjectView> authoredWaystones = new List<LevelObjectView>();
        int nextAuthored;
        readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
        int currentCheckpoint = -1;
        float autoCheckpointTimer;
        float lastCheckpointX = -100f;
        public int CheckpointCount => checkpoints.Count;

        public void Begin(LevelData data, Camera camera_, ParallaxBackground bg, GroundRenderer gr, Vector2? start, Action exit, Difficulty mode = Difficulty.Checkpoints, string exitTarget = "editor")
        {
            difficulty = mode;
            level = data;
            pristine = data.DeepClone();
            cam = camera_;
            background = bg;
            ground = gr;
            onExit = exit;
            background.settings = level.settings;
            ground.settings = level.settings;
            background.ApplySettings();
            ground.ApplySettings();
            cam.backgroundColor = level.settings.backgroundColor;

            worldRoot = new GameObject("World").transform;
            worldRoot.SetParent(transform, false);
            world = new LevelWorld(level, worldRoot);
            finishX = level.GetFinishX();
            totalCoins = 0;
            totalGems = 0;
            foreach (var v in world.all) if (v.def.id == "checkpoint") authoredWaystones.Add(v);
            authoredWaystones.Sort((a, b) => a.data.x.CompareTo(b.data.x));
            foreach (var v in world.all)
            {
                if (v.def.kind != ObjectKind.Collectible) continue;
                if (v.def.id == "gem") totalGems++;
                else if (v.def.id != "key") totalCoins++;
            }

            var playerGo = new GameObject("Rider");
            playerGo.transform.SetParent(transform, false);
            player = playerGo.AddComponent<PlayerController>();
            player.Init(this, world, level.settings);
            triggers = new TriggerSystem(world, this);
            playCamera = new PlayCamera(cam, player, level.settings);
            hud = PlayHUD.Create(transform, () => TogglePause(), RestartFromStart, Exit, TogglePractice, PlaceCheckpoint, RemoveCheckpoint);
            deathOverlay = HitboxOverlay.Create(transform, "Death Overlay", 980);
            particles = ParticleBurst.Create(transform, "Particles", 40);
            trailRibbon = HitboxOverlay.Create(transform, "Wisp Trail", -3);
            hud.SetExitTarget(exitTarget);

            ComputeStart(start);
            fullRun = start == null && difficulty == Difficulty.Champion;
            stats = LevelStatsStorage.Load(level.id);
            attempts = 0;
            introActive = exitTarget == "menu";
            Respawn();
            if (introActive)
            {
                var m = MountCatalog.Get(startMount);
                hud.ShowIntro(level.name, level.description, SpriteLibrary.ForMount(m), m.name, m.control + "\n" + DifficultyInfo.Name(difficulty) + ": " + DifficultyInfo.Describe(difficulty), SpriteLibrary.MountFacing(m));
                playCamera.Update(0f);
            }
        }

        /// <summary>Resolved spawn state: where the rider appears and with what mount/speed/gravity/size.</summary>
        public struct StartState
        {
            public Vector2 position;
            public string mount;
            public int speed;
            public bool flipped;
            public bool mini;
            /// <summary>The Start Position object that was used, or null.</summary>
            public LevelObject startObject;
        }

        /// <summary>
        /// Determines the spawn position and initial state. Uses the right-most enabled Start Position
        /// (left of the marker when one is given), then applies every portal between it and the marker.
        /// Shared with the editor so the spawn preview always matches the game.
        /// </summary>
        public static StartState ResolveStart(LevelData level, Vector2? marker)
        {
            var s = level.settings;
            var st = new StartState
            {
                mount = s.startMount, speed = s.startSpeed, flipped = s.startGravityFlipped, mini = s.startMini,
                position = new Vector2(0f, s.groundY + 0.5f)
            };

            LevelObject best = null;
            foreach (var o in level.objects)
            {
                if (o.type != "start_pos" || o.GetBool("disabled")) continue;
                if (marker != null && o.x > marker.Value.x) continue;
                if (best == null || o.x > best.x) best = o;
            }
            if (best != null)
            {
                st.startObject = best;
                st.position = new Vector2(best.x, best.y);
                st.mount = best.GetString("mount", st.mount);
                st.speed = best.GetInt("speed", st.speed);
                st.flipped = best.GetBool("flipped", st.flipped);
                st.mini = best.GetBool("mini", st.mini);
            }
            if (marker != null)
            {
                float scanFrom = best != null ? best.x : -1f;
                st.position = marker.Value;
                var portals = new List<LevelObject>();
                foreach (var o in level.objects)
                {
                    var def = ObjectCatalog.Get(o.type);
                    if (def == null || def.kind != ObjectKind.Portal) continue;
                    if (o.x > scanFrom && o.x < marker.Value.x) portals.Add(o);
                }
                portals.Sort((a, b) => a.x.CompareTo(b.x));
                foreach (var p in portals)
                {
                    var def = ObjectCatalog.Get(p.type);
                    switch (def.portalType)
                    {
                        case PortalType.Mount: st.mount = def.portalMount; break;
                        case PortalType.GravityNormal: st.flipped = false; break;
                        case PortalType.GravityFlip: st.flipped = true; break;
                        case PortalType.Speed: st.speed = (int)def.portalSpeed; break;
                        case PortalType.SizeMini: st.mini = true; break;
                        case PortalType.SizeNormal: st.mini = false; break;
                    }
                }
            }
            st.position.x = Mathf.Max(0f, st.position.x);
            return st;
        }

        void ComputeStart(Vector2? marker)
        {
            var st = ResolveStart(level, marker);
            startPos = st.position;
            startMount = st.mount;
            startSpeed = st.speed;
            startFlipped = st.flipped;
            startMini = st.mini;
        }

        void Respawn()
        {
            deathOverlay.Clear();
            hud.HideDeath();
            if (difficulty != Difficulty.Champion && checkpoints.Count > 0)
            {
                if (currentCheckpoint < 0 || currentCheckpoint >= checkpoints.Count) currentCheckpoint = checkpoints.Count - 1;
                RespawnAtCheckpoint(checkpoints[currentCheckpoint]);
                return;
            }
            attempts++;
            complete = false;
            paused = false;
            elapsed = 0f;
            coins = 0;
            gems = 0;
            keys = 0;
            lastBeat = -1;
            autoCheckpointTimer = 0f;
            lastCheckpointX = -100f;
            ClearTrail();
            ResetWorld();
            player.Spawn(startPos, startMount, startSpeed, startFlipped, startMini);
            triggers.ResetForStart(startPos.x);
            playCamera.Reset(startPos.x);
            if (fullRun) stats.attempts++;
            hud.SetAttempt(attempts);
            hud.SetLoot(0, totalCoins, 0, totalGems, 0);
            hud.SetMarkers(stats.deaths, sessionDeaths, stats.bestProgress);
            hud.HideComplete();
            hud.ShowPause(false);
            RefreshAuthoredWaystones();
            RefreshHudMode();
            hud.ShowHint(player.mount.name + " — " + player.mount.control + (practice ? "   ·   Z raises a waystone, X removes it, ← → scrub between them" : ""));
            if (!introActive) StartMusic();
        }

        /// <summary>Marks author-placed waystones behind the rider as reached, and dims them all on Champion.</summary>
        void RefreshAuthoredWaystones()
        {
            nextAuthored = 0;
            while (nextAuthored < authoredWaystones.Count && authoredWaystones[nextAuthored].data.x <= player.position.x) nextAuthored++;
            for (int i = 0; i < authoredWaystones.Count; i++)
            {
                var v = authoredWaystones[i];
                if (difficulty == Difficulty.Champion) v.renderer2D.color = new Color(0.6f, 0.6f, 0.6f, 0.35f);
                else v.renderer2D.color = i < nextAuthored ? new Color(0.75f, 1f, 0.8f, 1f) : new Color(0.55f, 0.6f, 0.58f, 0.9f);
            }
        }

        void UpdateAuthoredWaystones()
        {
            while (nextAuthored < authoredWaystones.Count && player.position.x >= authoredWaystones[nextAuthored].data.x)
            {
                var v = authoredWaystones[nextAuthored++];
                if (difficulty != Difficulty.Checkpoints) continue;
                v.renderer2D.color = new Color(0.75f, 1f, 0.8f, 1f);
                particles.Emit(v.WorldPosition, new Color(0.5f, 1f, 0.6f), 14, 3f, 0.6f, 0.1f, 2f);
                Sfx.Play("waystone", 0.7f);
                PlaceCheckpoint(false);
                hud.ShowHint("Waystone reached — you will return here if you fall.", 2.5f);
            }
        }

        void RefreshHudMode()
        {
            switch (difficulty)
            {
                case Difficulty.Training:
                    hud.SetMode("TRAINING", checkpoints.Count > 0 && currentCheckpoint >= 0 ? "waystone " + (currentCheckpoint + 1) + "/" + checkpoints.Count : "no waystones yet", true, autoCheckpoints);
                    break;
                case Difficulty.Checkpoints:
                    hud.SetMode("CHECKPOINTS", checkpoints.Count > 0 ? checkpoints.Count + " waystone" + (checkpoints.Count == 1 ? "" : "s") + " reached" : "no waystone reached yet", false, false);
                    break;
                default:
                    hud.SetMode("CHAMPION", "one life · sets your record", false, false);
                    break;
            }
        }

        // ---- practice mode -----------------------------------------------------------

        public void TogglePractice()
        {
            SetPractice(!practice);
        }

        public void SetPractice(bool on)
        {
            if (practice == on) return;
            fullRun = false; // a run that touched training never counts for the record
            difficulty = on ? Difficulty.Training : Difficulty.Checkpoints;
            if (!on) ClearCheckpoints();
            RefreshAuthoredWaystones();
            RefreshHudMode();
            hud.ShowHint(on ? "Training: Z raises a waystone, X removes the last one, ← → jump between waystones." : "Checkpoints: you fall back to the last author-placed waystone.", 5f);
        }

        public void PlaceCheckpoint() => PlaceCheckpoint(true);

        void PlaceCheckpoint(bool manual)
        {
            if (player.dead || complete || paused) return;
            if (manual && !practice) return;
            if (difficulty == Difficulty.Champion) return;
            var cp = new Checkpoint
            {
                player = player.Capture(),
                triggers = triggers.Capture(),
                world = world.Capture(),
                camera = playCamera.Capture(),
                settings = level.settings.Clone(),
                colors = new List<ColorChannel>(),
                elapsed = elapsed,
                coins = coins,
                gems = gems,
                keys = keys,
                ceiling = ground.showCeiling
            };
            foreach (var c in level.colors) cp.colors.Add(c.Clone());
            if (manual)
            {
                cp.marker = CreateCheckpointMarker(player.position);
                Sfx.Play("waystone", 0.7f);
            }
            checkpoints.Add(cp);
            currentCheckpoint = checkpoints.Count - 1;
            lastCheckpointX = player.position.x;
            autoCheckpointTimer = 0f;
            RefreshHudMode();
        }

        public void RemoveCheckpoint()
        {
            if (!practice || checkpoints.Count == 0) return;
            var cp = checkpoints[checkpoints.Count - 1];
            checkpoints.RemoveAt(checkpoints.Count - 1);
            if (cp.marker != null) Destroy(cp.marker);
            lastCheckpointX = checkpoints.Count > 0 ? checkpoints[checkpoints.Count - 1].player.position.x : -100f;
            if (currentCheckpoint >= checkpoints.Count) currentCheckpoint = checkpoints.Count - 1;
            RefreshHudMode();
        }

        /// <summary>Scrubs to an earlier (-1) or later (+1) waystone and respawns there.</summary>
        public void ScrubCheckpoint(int delta)
        {
            if (!practice || checkpoints.Count == 0) return;
            int target = Mathf.Clamp((currentCheckpoint < 0 ? checkpoints.Count - 1 : currentCheckpoint) + delta, 0, checkpoints.Count - 1);
            currentCheckpoint = target;
            deathOverlay.Clear();
            hud.HideDeath();
            RespawnAtCheckpoint(checkpoints[target]);
            hud.ShowHint("Waystone " + (target + 1) + " of " + checkpoints.Count, 2f);
        }

        void ClearCheckpoints()
        {
            foreach (var cp in checkpoints) if (cp.marker != null) Destroy(cp.marker);
            checkpoints.Clear();
            currentCheckpoint = -1;
            lastCheckpointX = -100f;
        }

        GameObject CreateCheckpointMarker(Vector2 pos)
        {
            var go = new GameObject("Waystone");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(sr);
            sr.sprite = SpriteLibrary.ForObject(ObjectCatalog.Get("checkpoint"));
            sr.color = new Color(0.75f, 1f, 0.8f, 0.85f);
            sr.sortingOrder = -1;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var gsr = glow.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(gsr);
            gsr.sprite = PlaceholderSpriteFactory.Circle();
            gsr.color = new Color(0.4f, 1f, 0.5f, 0.25f);
            gsr.sortingOrder = -2;
            glow.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            return go;
        }

        void RespawnAtCheckpoint(Checkpoint cp)
        {
            attempts++;
            complete = false;
            paused = false;
            autoCheckpointTimer = 0f;
            ClearTrail();
            elapsed = cp.elapsed;
            coins = cp.coins;
            gems = cp.gems;
            keys = cp.keys;

            // world + colours + themes
            world.Restore(cp.world);
            var live = level.settings;
            var saved = cp.settings;
            live.backgroundColor = saved.backgroundColor;
            live.groundColor = saved.groundColor;
            live.lineColor = saved.lineColor;
            live.objectColor = saved.objectColor;
            live.backgroundTheme = saved.backgroundTheme;
            live.far = saved.far.Clone();
            live.mid = saved.mid.Clone();
            live.near = saved.near.Clone();
            live.groundTheme = saved.groundTheme;
            level.colors.Clear();
            foreach (var c in cp.colors) level.colors.Add(c.Clone());
            background.ApplySettings();
            ground.ApplySettings();
            ground.showCeiling = cp.ceiling;
            world.FlushDirty();
            foreach (var v in world.all) world.MarkDirty(v); // colours may have changed
            world.FlushDirty();

            triggers.Restore(cp.triggers);
            player.Restore(cp.player);
            playCamera.Restore(cp.camera);
            hud.SetAttempt(attempts);
            hud.SetLoot(coins, totalCoins, gems, totalGems, keys);
            hud.HideComplete();
            hud.ShowPause(false);
            RefreshAuthoredWaystones();
            RefreshHudMode();

            // music: resume from the checkpoint's position in the song
            float offset = level.settings.songOffset + startPos.x / MountCatalog.Speed(startSpeed) + elapsed;
            RestartMusicAt(offset);
        }

        void RestartMusicAt(float offset)
        {
            if (!string.IsNullOrEmpty(level.settings.songFile))
            {
                var path = LevelStorage.AssetPath(level.id, level.settings.songFile);
                int request = ++musicRequest;
                float startedAt = Time.time;
                AudioLoader.Load(this, path, clip =>
                {
                    if (clip != null && request == musicRequest && this != null) PlayClip(clip, offset + (Time.time - startedAt), false);
                });
            }
            else if (!string.IsNullOrEmpty(level.settings.songId)) PlaySong(level.settings.songId, offset, false);
        }

        void UpdateTrail(float dt)
        {
            bool wisp = player.mount.id == "wisp" && !player.dead;
            float now = Time.time;
            if (wisp)
            {
                // sample the path: always when the direction changes (the zig-zag corners), otherwise every few frames
                var dir = player.velocity.y >= 0f ? Vector2.up : Vector2.down;
                bool corner = dir != lastTrailDir;
                if (trail.Count == 0 || corner || (player.position - trail[trail.Count - 1].pos).sqrMagnitude > 0.09f)
                {
                    trail.Add(new TrailPoint { pos = player.position, time = now });
                    lastTrailDir = dir;
                }
                trailSparkleTimer += dt;
                var mc = player.mount.Color;
                while (trailSparkleTimer > 0.025f)
                {
                    trailSparkleTimer -= 0.025f;
                    var back = player.position - new Vector2(player.direction * 0.3f, 0f);
                    particles.Emit(back, Color.Lerp(mc, Color.white, Random.value * 0.6f), 1, 1.2f, 0.45f, 0.07f, -0.5f, 180f, 60f);
                }
            }
            trail.RemoveAll(tp => now - tp.time > TrailLife);
            if (trail.Count > 200) trail.RemoveRange(0, trail.Count - 200);

            trailRibbon.Begin();
            if (trail.Count > 1)
            {
                var c = player.mount.Color;
                for (int i = 1; i < trail.Count; i++)
                {
                    float age = now - trail[i].time;
                    float a = Mathf.Clamp01(1f - age / TrailLife);
                    trailRibbon.Segment(trail[i - 1].pos, trail[i].pos, new Color(c.r, c.g, c.b, 0.85f * a), 0.06f + 0.14f * a);
                    if (i % 3 == 0) trailRibbon.Segment(trail[i - 1].pos, trail[i].pos, new Color(1f, 1f, 1f, 0.5f * a), 0.04f * a);
                }
                // live segment from the last sample to the rider
                if (wisp) trailRibbon.Segment(trail[trail.Count - 1].pos, player.position, new Color(c.r, c.g, c.b, 0.85f), 0.2f);
            }
            trailRibbon.End();
        }

        void ClearTrail()
        {
            trail.Clear();
            if (trailRibbon != null) trailRibbon.Clear();
        }

        void UpdateBeat()
        {
            var s = level.settings;
            if (!s.beatPulse || s.bpm < 20f) return;
            float songTime = s.songOffset + startPos.x / MountCatalog.Speed(startSpeed) + elapsed;
            int beat = Mathf.FloorToInt(songTime * s.bpm / 60f);
            if (beat == lastBeat) return;
            lastBeat = beat;
            bool bar = beat % 4 == 0;
            ground.Pulse(bar ? 1f : 0.55f);
            background.Pulse(bar ? 0.9f : 0.35f);
        }

        void UpdateAutoCheckpoint(float dt)
        {
            if (!practice || !autoCheckpoints || player.dead || complete) return;
            autoCheckpointTimer += dt;
            if (autoCheckpointTimer < 3f) return;
            if (!player.onGround && !player.mount.flying) return;
            if (player.position.x - lastCheckpointX < 4f) return;
            PlaceCheckpoint();
        }

        void ResetWorld()
        {
            foreach (var v in world.all)
            {
                v.runtimeOffset = Vector2.zero;
                v.runtimeRotation = 0f;
                v.runtimeAlpha = 1f;
                v.runtimeActive = true;
                v.pulseColor = Color.clear;
                world.MarkDirty(v);
            }
            // restore colours / themes that triggers may have changed
            level.settings.backgroundColor = pristine.settings.backgroundColor;
            level.settings.groundColor = pristine.settings.groundColor;
            level.settings.lineColor = pristine.settings.lineColor;
            level.settings.objectColor = pristine.settings.objectColor;
            level.settings.backgroundTheme = pristine.settings.backgroundTheme;
            level.settings.far = pristine.settings.far.Clone();
            level.settings.mid = pristine.settings.mid.Clone();
            level.settings.near = pristine.settings.near.Clone();
            level.settings.groundTheme = pristine.settings.groundTheme;
            level.colors.Clear();
            foreach (var c in pristine.colors) level.colors.Add(c.Clone());
            background.ApplySettings();
            ground.ApplySettings();
            world.FlushDirty();
        }

        public void RestartFromStart()
        {
            paused = false;
            deathOverlay.Clear();
            hud.HideDeath();
            ClearCheckpoints();
            Respawn();
        }

        public void TogglePause()
        {
            if (complete) return;
            paused = !paused;
            hud.ShowPause(paused);
        }

        public void Exit()
        {
            onExit?.Invoke();
        }

        void Update()
        {
            if (paused) return;
            float dt = Time.deltaTime;
            if (slowMo > 0f)
            {
                slowMo -= Time.deltaTime;
                dt *= 0.55f;
            }
            if (pulsingPortal != null)
            {
                portalPulse -= Time.deltaTime;
                float k = 1f + 0.3f * Mathf.Sin(Mathf.Clamp01(portalPulse / 0.35f) * Mathf.PI);
                pulsingPortal.ApplyTransform();
                var sc = pulsingPortal.transform.localScale;
                pulsingPortal.transform.localScale = new Vector3(sc.x * k, sc.y * k, 1f);
                if (portalPulse <= 0f)
                {
                    pulsingPortal.ApplyTransform();
                    pulsingPortal = null;
                }
            }

            // one-button input: mouse / touch / space / up arrow (ignored while the pointer is over HUD buttons)
            bool held = false, pressed = false;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            var ts = Touchscreen.current;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null && !overUI)
            {
                held |= mouse.leftButton.isPressed;
                pressed |= mouse.leftButton.wasPressedThisFrame;
            }
            if (ts != null && !overUI)
            {
                held |= ts.primaryTouch.press.isPressed;
                pressed |= ts.primaryTouch.press.wasPressedThisFrame;
            }
            if (kb != null)
            {
                held |= kb.spaceKey.isPressed || kb.upArrowKey.isPressed || kb.wKey.isPressed;
                pressed |= kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
                if (kb.rKey.wasPressedThisFrame) RestartFromStart();
                if (kb.zKey.wasPressedThisFrame) PlaceCheckpoint();
                if (kb.xKey.wasPressedThisFrame) RemoveCheckpoint();
                if (kb.cKey.wasPressedThisFrame) TogglePractice();
                if (practice && kb.leftArrowKey.wasPressedThisFrame) ScrubCheckpoint(-1);
                if (practice && kb.rightArrowKey.wasPressedThisFrame) ScrubCheckpoint(1);
            }
            if (introActive)
            {
                if (pressed || (kb != null && kb.enterKey.wasPressedThisFrame))
                {
                    introActive = false;
                    hud.HideIntro();
                    hud.Flash(new Color(1f, 0.95f, 0.8f, 0.4f), 0.25f);
                    Sfx.Play("horn", 0.8f);
                    StartMusic();
                }
                return;
            }
            if (pressed) triggers.OnPress();
            if (wasHeld && !held) triggers.OnRelease();
            wasHeld = held;

            if (complete)
            {
                world.UpdateSpinners(dt);
                playCamera.Update(dt);
                return;
            }

            if (player.dead)
            {
                // the run stays frozen on the failure until the player asks to retry
                player.Tick(dt);
                deathTimer += dt;
                playCamera.Update(dt); // keeps the shake alive during the freeze
                DrawDeathOverlay();
                if (deathTimer > 0.35f && (pressed || (kb != null && kb.enterKey.wasPressedThisFrame))) Retry();
                return;
            }

            elapsed += dt;
            player.SetInput(held, pressed);
            player.Tick(dt);
            UpdateTrail(dt);
            UpdateAuthoredWaystones();
            UpdateAutoCheckpoint(dt);
            UpdateBeat();
            triggers.Update(dt);
            world.UpdateSpinners(dt);
            world.FlushDirty();
            ground.showCeiling = player.mount.flying || player.flipped;
            playCamera.Update(dt);
            float progress = finishX > 0f ? player.position.x / finishX : 0f;
            hud.SetProgress(progress);
            if (fullRun && progress > stats.bestProgress + 0.002f)
            {
                stats.bestProgress = Mathf.Clamp01(progress);
                hud.SetMarkers(stats.deaths, sessionDeaths, stats.bestProgress);
            }

            if (player.finished)
            {
                complete = true;
                Sfx.Play("complete");
                hud.SetProgress(1f, true);
                if (fullRun)
                {
                    stats.bestProgress = 1f;
                    stats.completions++;
                    LevelStatsStorage.Save(stats);
                }
                else if (difficulty == Difficulty.Checkpoints && startPos.x <= 0.01f)
                {
                    stats.checkpointCompletions++;
                    LevelStatsStorage.Save(stats);
                }
                hud.SetMarkers(stats.deaths, sessionDeaths, stats.bestProgress);
                hud.ShowComplete(attempts, elapsed, player.jumps, coins, totalCoins, stats.attempts, stats.completions, gems, totalGems, DifficultyInfo.Name(difficulty));
                if (music != null && level.settings.fadeOut) music.Stop();
            }
        }

        void DrawDeathOverlay()
        {
            deathOverlay.Begin();
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 8f);
            float w = 0.07f;
            // rider hitboxes: outer (landing/side collision) and inner (hazards)
            deathOverlay.Rect(player.Bounds, new Color(1f, 1f, 1f, 0.5f), 0.03f);
            deathOverlay.Rect(player.InnerBounds, new Color(1f, 0.9f, 0.3f, 0.9f), 0.03f);
            var k = player.killer;
            if (k != null)
            {
                var red = HitboxOverlay.DangerColor;
                red.a = pulse;
                if (player.deathEdge == "hitbox" || k.def.kind == ObjectKind.Hazard)
                {
                    deathOverlay.DrawViewKind(k, EdgeKind.Danger, red, w);
                    if (k.def.kind == ObjectKind.Slope) deathOverlay.DrawViewKind(k, EdgeKind.Safe, red, w);
                }
                else
                {
                    var bounds = k.Bounds;
                    var e = Hitboxes.AabbEdge(bounds, player.deathEdge, EdgeKind.Danger);
                    deathOverlay.Segment(e.a, e.b, red, w);
                }
            }
            // contact point: white dot with a red ring
            deathOverlay.Dot(player.deathPoint, 0.13f, new Color(1f, 0.2f, 0.2f, pulse));
            deathOverlay.Dot(player.deathPoint, 0.07f, Color.white);
            deathOverlay.End();
        }

        void Retry()
        {
            deathOverlay.Clear();
            hud.HideDeath();
            Respawn();
        }

        public void OnJumped(Vector2 pos, float up)
        {
            Sfx.Play("jump_" + player.mount.id, 0.8f, 1f, 0.05f, "jump");
            var feet = pos - new Vector2(0f, up * player.Size.y * 0.45f);
            particles.Emit(feet, new Color(0.9f, 0.85f, 0.75f, 0.7f), 4, 1.8f, 0.3f, 0.08f, 3f, up > 0 ? 270f : 90f, 120f);
        }

        public void OnLanded(Vector2 pos, float up, Vector2 size)
        {
            Sfx.Play("land", 0.5f, 1f, 0.08f);
            var feet = pos - new Vector2(0f, up * size.y * 0.5f);
            particles.Emit(feet, new Color(0.85f, 0.8f, 0.7f, 0.75f), 7, 2.4f, 0.35f, 0.09f, 4f, up > 0 ? 90f : 270f, 150f);
        }

        public void OnPlayerDied()
        {
            deathTimer = 0f;
            musicRequest++;
            Sfx.Play("death", 1f, 1f, 0.03f);
            var c = player.mount.Color;
            particles.Emit(player.position, c, 18, 9f, 0.8f, 0.17f, 22f);
            particles.Emit(player.position, player.mount.Accent, 8, 6f, 0.6f, 0.12f, 18f);
            playCamera.Shake(0.35f, 0.03f, 0.3f);
            hud.Flash(new Color(1f, 0.25f, 0.15f, 0.45f), 0.18f);
            float progress = Mathf.Clamp01(finishX > 0f ? player.position.x / finishX : 0f);
            hud.SetProgress(progress, true);
            sessionDeaths.Add(progress);
            if (fullRun)
            {
                stats.RecordDeath(progress);
                if (progress > stats.bestProgress) stats.bestProgress = progress;
                LevelStatsStorage.Save(stats);
            }
            hud.SetMarkers(stats.deaths, sessionDeaths, stats.bestProgress);
            if (music != null) music.Stop();
        }

        public void OnCollect(LevelObjectView v)
        {
            particles.Emit(v.WorldPosition, v.def.primaryColor, 12, 4f, 0.5f, 0.1f, 5f);
            if (v.def.id == "key")
            {
                keys++;
                Sfx.Play("key", 0.9f);
                int id = v.data.GetInt("itemId", 1);
                triggers.OnItemCollected(id);
                int opened = 0;
                foreach (var g in world.all)
                {
                    if (g.def.id != "locked_gate" || !g.runtimeActive || g.data.GetInt("keyId", 1) != id) continue;
                    g.runtimeActive = false;
                    world.MarkDirty(g);
                    opened++;
                }
                hud.ShowHint(opened > 0 ? "The key turns. " + (opened == 1 ? "A gate" : opened + " gates") + " swing open!" : "Picked up a dungeon key.", 3f);
                if (opened > 0) Sfx.Play("gate");
            }
            else if (v.def.id == "gem")
            {
                gems++;
                Sfx.Play("gem", 0.8f);
            }
            else
            {
                coins++;
                Sfx.Play("coin", 0.7f, 1f, 0.06f);
            }
            hud.SetLoot(coins, totalCoins, gems, totalGems, keys);
        }

        public void OnInteract(LevelObjectView v)
        {
            var col = v.def.primaryColor;
            switch (v.def.kind)
            {
                case ObjectKind.Portal:
                {
                    bool mountGate = v.def.portalType == PortalType.Mount;
                    switch (v.def.portalType)
                    {
                        case PortalType.Mount: Sfx.Play("portal_mount"); break;
                        case PortalType.GravityFlip:
                        case PortalType.GravityNormal: Sfx.Play("portal_gravity", 0.9f); break;
                        case PortalType.Speed: Sfx.Play("portal_speed", 0.9f); break;
                        case PortalType.Teleport: Sfx.Play("teleport"); break;
                        default: Sfx.Play("portal", 0.8f); break;
                    }
                    particles.Emit(v.WorldPosition, col, mountGate ? 32 : 18, 6f, 0.6f, 0.14f, 0f);
                    particles.Emit(player.position, Color.white, 10, 4f, 0.4f, 0.1f, 0f);
                    hud.Flash(new Color(col.r, col.g, col.b, mountGate ? 0.45f : 0.25f), mountGate ? 0.3f : 0.18f);
                    playCamera.Shake(mountGate ? 0.15f : 0.06f, 0.03f, 0.18f);
                    if (mountGate) slowMo = 0.32f;
                    pulsingPortal = v;
                    portalPulse = 0.35f;
                    break;
                }
                case ObjectKind.Orb:
                    switch (v.def.orbType)
                    {
                        case OrbType.GravityFlip:
                        case OrbType.GravityJump: Sfx.Play("rune_gravity", 0.9f); break;
                        case OrbType.BigJump: Sfx.Play("rune_big", 0.9f); break;
                        case OrbType.Dash:
                        case OrbType.DashFlip: Sfx.Play("rune_dash", 0.9f); break;
                        case OrbType.Slam: Sfx.Play("rune_void", 0.9f); break;
                        case OrbType.Teleport: Sfx.Play("teleport", 0.8f); break;
                        default: Sfx.Play("rune", 0.9f, 1f, 0.08f); break;
                    }
                    particles.Emit(v.WorldPosition, col, 10, 5f, 0.4f, 0.1f, 6f);
                    break;
                case ObjectKind.Pad:
                    Sfx.Play(v.def.padType == PadType.GravityFlip ? "pad_gravity" : "pad", 0.8f, v.def.padType == PadType.BigJump ? 0.85f : 1f, 0.06f);
                    particles.Emit(v.WorldPosition, col, 8, 4f, 0.35f, 0.09f, 8f, player.flipped ? 270f : 90f, 90f);
                    break;
            }
        }

        public void OnPlayerStateChanged()
        {
            hud.ShowHint(player.mount.name + " — " + player.mount.control, 3f);
        }

        // ---- music --------------------------------------------------------------

        int musicRequest;

        void StartMusic()
        {
            float offset = level.settings.songOffset + startPos.x / MountCatalog.Speed(startSpeed);
            if (!string.IsNullOrEmpty(level.settings.songFile))
            {
                var path = LevelStorage.AssetPath(level.id, level.settings.songFile);
                int request = ++musicRequest;
                float startedAt = Time.time;
                AudioLoader.Load(this, path, clip =>
                {
                    if (clip == null || request != musicRequest || this == null) return;
                    // the level may already be running: skip ahead by however long the load took
                    PlayClip(clip, offset + (Time.time - startedAt), false);
                });
                return;
            }
            var id = level.settings.songId;
            if (string.IsNullOrEmpty(id)) return;
            PlaySong(id, offset, false);
        }

        public void PlaySong(string id, float offset, bool loop)
        {
            if (string.IsNullOrEmpty(id)) return;
            var clip = Resources.Load<AudioClip>("Songs/" + id);
            if (clip == null)
            {
                // Song trigger may also name an imported file
                var path = LevelStorage.AssetPath(level.id, id);
                if (path != null && System.IO.File.Exists(path))
                {
                    int request = ++musicRequest;
                    float startedAt = Time.time;
                    AudioLoader.Load(this, path, c =>
                    {
                        if (c != null && request == musicRequest && this != null) PlayClip(c, offset + (Time.time - startedAt), loop);
                    });
                    return;
                }
                Debug.LogWarning("Song not found in Resources/Songs or the level's assets: " + id);
                return;
            }
            PlayClip(clip, offset, loop);
        }

        void PlayClip(AudioClip clip, float offset, bool loop)
        {
            if (music == null)
            {
                music = gameObject.AddComponent<AudioSource>();
                music.playOnAwake = false;
            }
            music.clip = clip;
            music.loop = loop;
            music.volume = Sfx.MusicVolume;
            music.time = Mathf.Clamp(offset, 0f, Mathf.Max(0f, clip.length - 0.1f));
            music.Play();
        }

        public void Shutdown()
        {
            if (stats != null && fullRun) LevelStatsStorage.Save(stats);
            musicRequest++;
            if (music != null) music.Stop();
            if (world != null) world.Destroy();
            cam.ResetProjectionMatrix();
            ground.showCeiling = false;
        }
    }
}
