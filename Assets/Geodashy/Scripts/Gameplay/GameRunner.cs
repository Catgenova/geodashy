using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Profiling;

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
        PlayEffects fx;
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
        bool lootBannerShown;
        /// <summary>Tests and bots: when set, replaces device input with (held, pressed).</summary>
        public Func<(bool held, bool pressed)> autoInput;

        // ---- death replay ghost: the last two seconds of the run, replayed at the crash site --------
        struct ReplaySample
        {
            public Vector2 pos;
            public float rot;
            public Sprite sprite;
            public Vector3 scale;
            public float time;
        }
        const float ReplayWindow = 2f;
        readonly List<ReplaySample> replay = new List<ReplaySample>();
        SpriteRenderer replayGhost;
        float replayClock;
        const float GhostFps = 9f;
        float ghostAnimTime;

        /// <summary>Puts the spectral rider's current frame on a ghost renderer, facing the way it is moving.</summary>
        void ApplyGhostFrame(SpriteRenderer sr, Vector2 pos, float facing, float dt)
        {
            var frames = SpriteLibrary.GhostFrames();
            if (frames == null)
            {
                // no sheet: fall back to the mount silhouette
                if (sr.sprite == null) sr.sprite = SpriteLibrary.ForMount(player.mount);
                float mountScale = sr.sprite != null ? player.mount.width / Mathf.Max(0.01f, sr.sprite.bounds.size.x) : 1f;
                sr.transform.localScale = new Vector3(mountScale * facing * SpriteLibrary.MountFacing(player.mount), mountScale, 1f);
            }
            else
            {
                ghostAnimTime += dt;
                sr.sprite = frames[Mathf.FloorToInt(ghostAnimTime * GhostFps) % frames.Length];
                float scale = player.mount.width / Mathf.Max(0.01f, sr.sprite.bounds.size.x);
                sr.transform.localScale = new Vector3(scale * facing, scale, 1f);
            }
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.transform.rotation = Quaternion.identity;
        }

        // ---- personal-best ghost (Champion): a faint rider following the best recorded run ----------
        SpriteRenderer bestGhost;
        readonly List<float> runX = new List<float>();
        readonly List<float> runY = new List<float>();
        float runSampleTimer;
        float runStartBest;
        float mountTrailTimer;

        // ---- run trace for the editor (every frame since the last respawn) and the report card profile ----
        public readonly List<Vector2> runTrace = new List<Vector2>();
        readonly List<float> profile = new List<float>();
        float profileTimer;

        // ---- reactive crowd: banners sway, lights flare, bells swing on the beat and as the rider passes ----
        class Reactive
        {
            public LevelObjectView view;
            public int kind;   // 0 sway, 1 flare, 2 swing
            public float phase;
            public float energy;
        }
        readonly List<Reactive> reactive = new List<Reactive>();
        float beatEnergy;

        // ---- rune combos, coin streaks, sync check, crash guard ----
        int combo;
        float comboTimer;
        int coinStreak;
        float coinStreakTimer;
        float dashLineTimer;
        public bool syncCheck;
        /// <summary>Jumps of the last run: x, y and the offset from the nearest beat in beats (-0.5..0.5).</summary>
        public readonly List<Vector3> jumpBeats = new List<Vector3>();
        bool crashed;
        int lastFlashBeat = -1;

        // ---- captures, performance readout, profiler markers ----
        CaptureRecorder recorder;
        float perfTimer, perfFps;
        int perfDrawn;
        public const string PerfHudPref = "geodashy.perfHud";
        public const string RecordClipsPref = "geodashy.recordClips";
        public static bool PerfHud
        {
            get => PlayerPrefs.GetInt(PerfHudPref, 0) == 1;
            set { PlayerPrefs.SetInt(PerfHudPref, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        public static bool RecordClips
        {
            get => PlayerPrefs.GetInt(RecordClipsPref, 0) == 1;
            set { PlayerPrefs.SetInt(RecordClipsPref, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        static readonly ProfilerMarker PlayerMarker = new ProfilerMarker("LyreFlyer.Player");
        static readonly ProfilerMarker TriggersMarker = new ProfilerMarker("LyreFlyer.Triggers");
        static readonly ProfilerMarker WorldMarker = new ProfilerMarker("LyreFlyer.World");
        static readonly ProfilerMarker EffectsMarker = new ProfilerMarker("LyreFlyer.Effects");

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
            fx = new PlayEffects(this, world, transform, particles, cam, level);
            trailRibbon = HitboxOverlay.Create(transform, "Wisp Trail", -3);
            hud.SetExitTarget(exitTarget);
            replayGhost = MakeGhost("Replay Ghost", 4);
            bestGhost = MakeGhost("Best Run Ghost", 3);
            CollectReactive();
            CrashGuard.Hook();
            previousAnnounce = Achievements.Announce;
            Achievements.Announce = msg =>
            {
                if (hud != null) hud.ShowHint(msg, 4f);
                Sfx.Play("horn", 0.6f, 1.3f);
            };
            Achievements.LevelPlayed(level.id);
            recorder = CaptureRecorder.Create(transform, level.name);
            recorder.recording = RecordClips;
            hud.onScreenshot = () => recorder.SaveScreenshot(path => hud.ShowHint(string.IsNullOrEmpty(path) ? "Screenshot failed" : "Screenshot saved: " + path, 5f));
            hud.onClip = () =>
            {
                var path = recorder.SaveClip();
                hud.ShowHint(string.IsNullOrEmpty(path) ? "No clip recorded yet (enable clip recording in Options)" : "Clip saved: " + path, 5f);
            };
            hud.SetClipAvailable(RecordClips);

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

        /// <summary>Finds decorations that react to the beat and the rider by their catalog tags.</summary>
        void CollectReactive()
        {
            reactive.Clear();
            foreach (var v in world.all)
            {
                if (v.def.kind != ObjectKind.Decoration) continue;
                int kind = -1;
                foreach (var t in v.def.tags)
                {
                    if (t == "flag" || t == "banner" || t == "tent" || t == "pavilion" || t == "tapestry" || t == "garland") kind = 0;
                    else if (t == "fire" || t == "light" || t == "candle" || t == "lantern" || t == "brazier") kind = 1;
                    else if (t == "bell") kind = 2;
                    if (kind >= 0) break;
                }
                if (kind < 0) continue;
                reactive.Add(new Reactive { view = v, kind = kind, phase = UnityEngine.Random.value * 6.28f });
            }
        }

        void UpdateReactive(float dt)
        {
            if (reactive.Count == 0) return;
            beatEnergy = Mathf.Max(0f, beatEnergy - dt * 2.5f);
            float camX = cam.transform.position.x;
            float halfW = playCamera.HalfWidth + 3f;
            float px = player.position.x;
            float now = Time.time;
            foreach (var r in reactive)
            {
                var v = r.view;
                float dx = v.data.x - camX;
                bool near = Mathf.Abs(dx) < halfW;
                if (!near)
                {
                    if (v.visualWobble != 0f || v.visualPulse != 0f)
                    {
                        v.visualWobble = 0f;
                        v.visualPulse = 0f;
                        v.ApplyTransform();
                    }
                    continue;
                }
                // the rider passing close by adds a gust
                float pass = Mathf.Clamp01(1f - Mathf.Abs(v.data.x - px) / 3f);
                r.energy = Mathf.Max(r.energy - dt * 1.8f, pass * 0.8f);
                float e = Mathf.Clamp01(beatEnergy * 0.7f + r.energy);
                switch (r.kind)
                {
                    case 0: v.visualWobble = Mathf.Sin(now * 5f + r.phase) * (2f + 9f * e); v.visualPulse = 0f; break;
                    case 1: v.visualPulse = (0.03f + 0.18f * e) * (0.5f + 0.5f * Mathf.Sin(now * 14f + r.phase)); v.visualWobble = 0f; break;
                    default: v.visualWobble = Mathf.Sin(now * 4f + r.phase) * 14f * e; v.visualPulse = 0f; break;
                }
                v.ApplyTransform();
            }
        }

        SpriteRenderer MakeGhost(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(sr);
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        void RecordReplay()
        {
            float now = elapsed;
            replay.Add(new ReplaySample { pos = player.position, rot = player.rotationDeg, sprite = player.CurrentSprite, scale = player.VisualScale, time = now });
            while (replay.Count > 0 && now - replay[0].time > ReplayWindow) replay.RemoveAt(0);
        }

        /// <summary>Loops the recorded window as a translucent rider while the death freeze is on screen.</summary>
        void UpdateReplayGhost(float dt)
        {
            if (replay.Count < 2)
            {
                replayGhost.enabled = false;
                return;
            }
            float span = replay[replay.Count - 1].time - replay[0].time;
            if (span < 0.15f)
            {
                replayGhost.enabled = false;
                return;
            }
            replayClock += dt;
            float loop = span + 0.5f;   // hold on the crash for half a second before looping
            float t = replayClock % loop;
            if (t > span) t = span;
            float target = replay[0].time + t;
            int i = 0;
            while (i < replay.Count - 1 && replay[i + 1].time < target) i++;
            var s = replay[i];
            replayGhost.enabled = true;
            float facing = i + 1 < replay.Count ? Mathf.Sign(replay[i + 1].pos.x - s.pos.x + 0.0001f) : 1f;
            ApplyGhostFrame(replayGhost, s.pos, facing, dt);
            float fade = t > span - 0.05f ? 0f : 1f;
            replayGhost.color = new Color(0.75f, 0.95f, 1f, 0.75f * fade);
        }

        void RecordBestRunSample(float dt)
        {
            if (!fullRun) return;
            runSampleTimer += dt;
            while (runSampleTimer >= LevelStats.BestRunStep)
            {
                runSampleTimer -= LevelStats.BestRunStep;
                runX.Add(player.position.x);
                runY.Add(player.position.y);
            }
        }

        /// <summary>Keeps this run as the personal-best recording when it went further than any before.</summary>
        void CommitBestRun(float progress)
        {
            if (!fullRun || runX.Count == 0) return;
            if (progress <= stats.bestRunProgress + 0.0005f && stats.bestRunX.Count > 0) return;
            stats.bestRunProgress = progress;
            stats.bestRunX = new List<float>(runX);
            stats.bestRunY = new List<float>(runY);
        }

        void UpdateBestGhost()
        {
            if (!fullRun || stats.bestRunX.Count == 0 || player.dead)
            {
                bestGhost.enabled = false;
                return;
            }
            int i = Mathf.FloorToInt(elapsed / LevelStats.BestRunStep);
            if (i >= stats.bestRunX.Count - 1)
            {
                bestGhost.enabled = false;
                return;
            }
            float f = (elapsed - i * LevelStats.BestRunStep) / LevelStats.BestRunStep;
            float x = Mathf.Lerp(stats.bestRunX[i], stats.bestRunX[i + 1], f);
            float y = Mathf.Lerp(stats.bestRunY[i], stats.bestRunY[i + 1], f);
            bestGhost.enabled = true;
            float facing = stats.bestRunX[i + 1] >= stats.bestRunX[i] ? 1f : -1f;
            ApplyGhostFrame(bestGhost, new Vector2(x, y), facing, Time.deltaTime);
            bestGhost.color = new Color(1f, 0.92f, 0.6f, 0.55f);
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
            fx?.Reset();
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
            lootBannerShown = false;
            hud.HideLootBanner();
            replay.Clear();
            replayClock = 0f;
            if (replayGhost != null) replayGhost.enabled = false;
            runX.Clear();
            runY.Clear();
            runTrace.Clear();
            jumpBeats.Clear();
            combo = 0;
            coinStreak = 0;
            profile.Clear();
            profileTimer = 0f;
            runSampleTimer = 0f;
            runStartBest = stats != null ? stats.bestProgress : 0f;
            if (bestGhost != null) bestGhost.sprite = null;
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
                Haptics.Waystone();
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
                    particles.Emit(back, Color.Lerp(mc, Color.white, UnityEngine.Random.value * 0.6f), 1, 1.2f, 0.45f, 0.07f, -0.5f, 180f, 60f);
                }
            }
            trail.RemoveAll(tp => now - tp.time > TrailLife);
            if (trail.Count > 200) trail.RemoveRange(0, trail.Count - 200);
            UpdateMountTrail(dt);

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

        /// <summary>Mount-specific particles: embers, feathers, dirt, dust, shadow wisps.</summary>
        void UpdateMountTrail(float dt)
        {
            if (player.dead || !player.visible) return;
            string id = player.mount.id;
            if (id == "wisp") return;
            mountTrailTimer += dt;
            var back = player.position - new Vector2(player.direction * player.Size.x * 0.45f, 0f);
            var feet = player.position - new Vector2(player.direction * player.Size.x * 0.2f, player.Up * player.Size.y * 0.45f);
            float rand = UnityEngine.Random.value;
            switch (id)
            {
                case "dragon":
                    if (mountTrailTimer < 0.05f) return;
                    particles.Emit(back, Color.Lerp(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.3f), rand), 1, 1.5f, 0.6f, 0.08f, -1.5f, 180f, 70f);
                    break;
                case "griffin":
                    if (mountTrailTimer < 0.11f) return;
                    particles.Emit(back, rand < 0.7f ? new Color(0.95f, 0.9f, 0.8f) : new Color(0.85f, 0.7f, 0.35f), 1, 0.8f, 1.1f, 0.1f, 1.2f, 180f, 50f);
                    break;
                case "boar":
                    if (mountTrailTimer < 0.05f || !player.onGround) return;
                    particles.Emit(feet, new Color(0.45f, 0.3f, 0.18f), 2, 3.5f, 0.45f, 0.08f, 9f, player.Up > 0 ? 110f : 250f, 60f);
                    break;
                case "cart":
                    if (mountTrailTimer < 0.06f || !player.onGround) return;
                    particles.Emit(feet, new Color(0.7f, 0.65f, 0.55f, 0.6f), 2, 1.2f, 0.7f, 0.16f, 0.4f, player.Up > 0 ? 120f : 240f, 70f);
                    break;
                case "shadowcat":
                    if (mountTrailTimer < 0.05f) return;
                    particles.Emit(back, Color.Lerp(new Color(0.35f, 0.2f, 0.6f), new Color(0.7f, 0.45f, 1f), rand), 1, 0.9f, 0.55f, 0.12f, -0.6f, 180f, 40f);
                    break;
                default:   // horse: hoof dust while running
                    if (mountTrailTimer < 0.08f || !player.onGround) return;
                    particles.Emit(feet, new Color(0.85f, 0.78f, 0.65f, 0.55f), 1, 1.5f, 0.4f, 0.09f, 2f, player.Up > 0 ? 120f : 240f, 60f);
                    break;
            }
            mountTrailTimer = 0f;
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
            hud.PulseVignette(bar ? 0.45f : 0.18f);
            beatEnergy = bar ? 1f : 0.55f;
            if (fx != null) fx.beatEnergy = beatEnergy;
            if (syncCheck && beat != lastFlashBeat)
            {
                lastFlashBeat = beat;
                hud.Flash(new Color(1f, 1f, 1f, bar ? 0.35f : 0.18f), 0.08f);
            }
        }

        /// <summary>Beats since the song started at this moment of the run.</summary>
        float SongBeat()
        {
            var s = level.settings;
            float songTime = s.songOffset + startPos.x / MountCatalog.Speed(startSpeed) + elapsed;
            return songTime * s.bpm / 60f;
        }

        void UpdateRuneEffects(float dt)
        {
            if (comboTimer > 0f)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f) combo = 0;
            }
            if (coinStreakTimer > 0f)
            {
                coinStreakTimer -= dt;
                if (coinStreakTimer <= 0f) coinStreak = 0;
            }
            // speed lines while dashing
            if (player.IsDashing)
            {
                dashLineTimer += dt;
                while (dashLineTimer > 0.03f)
                {
                    dashLineTimer -= 0.03f;
                    var p = player.position + new Vector2(-player.direction * 0.4f, UnityEngine.Random.Range(-0.9f, 0.9f));
                    particles.Emit(p, new Color(0.6f, 1f, 0.95f, 0.7f), 1, 6f, 0.25f, 0.05f, 0f, player.direction > 0 ? 180f : 0f, 4f);
                }
            }
        }

        /// <summary>Loot within a short reach drifts to the rider.</summary>
        void UpdateCoinMagnet(float dt)
        {
            var b = GeoMath.Expand(player.Bounds, 1.6f);
            var near = world.Query(b);
            foreach (var v in near)
            {
                if (v.def.kind != ObjectKind.Collectible || !v.runtimeActive) continue;
                var pos = v.WorldPosition;
                var d = player.position - pos;
                float dist = d.magnitude;
                if (dist > 1.8f || dist < 0.05f) continue;
                v.runtimeOffset += d / dist * Mathf.Min(dist, (8f + (1.8f - dist) * 12f) * dt);
                world.MarkDirty(v);
            }
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
            float progress = finishX > 0f ? Mathf.Clamp01(player.position.x / finishX) : 0f;
            string loot = totalCoins + totalGems > 0 ? "   ·   loot " + (coins + gems) + " / " + (totalCoins + totalGems) : "";
            hud.ShowPause(paused, "Attempt " + attempts + "   ·   " + elapsed.ToString("0.0") + " s   ·   " + (progress * 100f).ToString("0.0") + "%" + loot + "\n" + level.name + "   ·   " + DifficultyInfo.Name(difficulty));
        }

        public void Exit()
        {
            onExit?.Invoke();
        }

        void Update()
        {
            if (paused || crashed) return;
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                // a bug in play should never freeze the phone: freeze the run, report, and offer the way out
                crashed = true;
                Debug.LogException(e);
                var path = CrashGuard.Report(e, "level " + level.id + " (" + level.name + ") · mount " + (player != null ? player.mount.id : "?") + " · x " + (player != null ? player.position.x.ToString("0.0") : "?") + " · " + DifficultyInfo.Name(difficulty));
                hud.ShowDeath("The quest stumbled: " + e.GetType().Name, finishX > 0f ? player.position.x / finishX : 0f);
                hud.ShowHint(string.IsNullOrEmpty(path) ? "Bug report could not be written" : "Bug report written to " + path, 30f);
                hud.ShowPause(true);
                paused = true;
            }
        }

        void Tick()
        {
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
            // only a press that lands on a button / slider counts as "over UI"; HUD text never swallows a tap
            if (mouse != null && !Application.isMobilePlatform && !Geodashy.Editing.UI.EditorUI.IsScreenPointOverUI(mouse.position.ReadValue(), true))
            {
                held |= mouse.leftButton.isPressed;
                pressed |= mouse.leftButton.wasPressedThisFrame;
            }
            if (ts != null && !Geodashy.Editing.UI.EditorUI.IsScreenPointOverUI(ts.primaryTouch.position.ReadValue(), true))
            {
                held |= ts.primaryTouch.press.isPressed;
                pressed |= ts.primaryTouch.press.wasPressedThisFrame;
            }
            if (autoInput != null)
            {
                var ai = autoInput();
                held = ai.held;
                pressed = ai.pressed;
            }
            // keyboard (rebindable jump key plus Space / Up / W) and any gamepad button
            if (autoInput == null)
            {
                held |= InputConfig.JumpHeld();
                pressed |= InputConfig.JumpPressed();
            }
            var gp = Gamepad.current;
            if (gp != null && gp.startButton.wasPressedThisFrame && !introActive && !complete && !crashed)
            {
                TogglePause();
                return;
            }
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) RestartFromStart();
                if (kb.zKey.wasPressedThisFrame) PlaceCheckpoint();
                if (kb.xKey.wasPressedThisFrame) RemoveCheckpoint();
                if (kb.cKey.wasPressedThisFrame) TogglePractice();
                if (practice && kb.leftArrowKey.wasPressedThisFrame) ScrubCheckpoint(-1);
                if (practice && kb.rightArrowKey.wasPressedThisFrame) ScrubCheckpoint(1);
            }
            if (introActive)
            {
                if (pressed || InputConfig.ConfirmPressed())
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
                fx.Update(dt, player, playCamera.HalfWidth, cam.orthographicSize);   // confetti and the waving banner
                return;
            }

            if (player.dead)
            {
                // the run stays frozen on the failure until the player asks to retry
                player.Tick(dt);
                deathTimer += dt;
                playCamera.Update(dt); // keeps the shake alive during the freeze
                DrawDeathOverlay();
                UpdateReplayGhost(dt);
                if (deathTimer > 0.35f && (pressed || InputConfig.ConfirmPressed())) Retry();
                return;
            }

            elapsed += dt;
            PlayerMarker.Begin();
            player.SetInput(held, pressed);
            player.Tick(dt);
            PlayerMarker.End();
            UpdateRuneEffects(dt);
            UpdateCoinMagnet(dt);
            EffectsMarker.Begin();
            RecordReplay();
            RecordBestRunSample(dt);
            if (runTrace.Count < 20000) runTrace.Add(player.position);
            profileTimer += dt;
            if (profileTimer >= 0.1f)
            {
                profileTimer -= 0.1f;
                profile.Add(Mathf.Clamp01((player.position.y - level.settings.groundY) / Mathf.Max(1f, level.settings.ceilingHeight)));
            }
            UpdateBestGhost();
            UpdateTrail(dt);
            UpdateReactive(dt);
            fx.Update(dt, player, playCamera.HalfWidth, cam.orthographicSize);
            EffectsMarker.End();
            UpdateAuthoredWaystones();
            UpdateAutoCheckpoint(dt);
            UpdateBeat();
            TriggersMarker.Begin();
            triggers.Update(dt);
            TriggersMarker.End();
            WorldMarker.Begin();
            world.UpdateSpinners(dt);
            world.FlushDirty();
            ground.showCeiling = player.mount.flying || player.flipped;
            playCamera.Update(dt);
            world.UpdateCulling(cam.transform.position.x, playCamera.HalfWidth);
            WorldMarker.End();
            UpdatePerf(dt);
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
                fx.Finish(player, playCamera);
                slowMo = 0.9f;
                hud.Flash(new Color(1f, 0.95f, 0.7f, 0.35f), 0.4f);
                hud.SetProgress(1f, true);
                bool allLoot = coins >= totalCoins && gems >= totalGems && totalCoins + totalGems > 0;
                float parTime = finishX / MountCatalog.Speed(startSpeed) * 1.03f + 0.5f;
                if (fullRun)
                {
                    stats.bestProgress = 1f;
                    stats.completions++;
                    CommitBestRun(1f);
                    if (allLoot) stats.fullLoot = true;
                    string medals = (elapsed <= parTime ? "S" : "") + (allLoot ? "L" : "") + (attempts == 1 ? "D" : "");
                    stats.AddRun(new RunRecord { rider = PlayerProfile.Name, seconds = elapsed, dateUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), medals = medals, attempts = attempts });
                    LevelStatsStorage.Save(stats);
                    Achievements.Unlock("champion_clear");
                    if (level.id.StartsWith("daily_"))
                    {
                        Achievements.Unlock("daily_1");
                        if (Achievements.AddCounter("daily", 1) >= 10) Achievements.Unlock("daily_10");
                    }
                }
                if (startPos.x <= 0.01f)
                {
                    Achievements.Unlock("first_clear");
                    if (attempts == 1) Achievements.Unlock("deathless");
                    if (allLoot) Achievements.Unlock("all_loot");
                    if (elapsed <= parTime) Achievements.Unlock("under_par");
                    if (player.nearMisses >= 10) Achievements.Unlock("near_miss_10");
                    if (player.jumps >= 100) Achievements.Unlock("jumps_100");
                    CheckCampaignComplete();
                }
                else if (difficulty == Difficulty.Checkpoints && startPos.x <= 0.01f)
                {
                    stats.checkpointCompletions++;
                    if (allLoot) stats.fullLoot = true;
                    LevelStatsStorage.Save(stats);
                }
                if (bestGhost != null) bestGhost.enabled = false;
                hud.SetMarkers(stats.deaths, sessionDeaths, stats.bestProgress);
                float par = finishX / MountCatalog.Speed(startSpeed) * 1.03f + 0.5f;
                var card = new ReportCard
                {
                    attempts = attempts, seconds = elapsed, par = par, jumps = player.jumps, coins = coins, totalCoins = totalCoins, gems = gems, totalGems = totalGems,
                    totalAttempts = stats.attempts, completions = stats.completions, difficultyName = DifficultyInfo.Name(difficulty), nearMisses = player.nearMisses,
                    medalTime = elapsed <= par, medalLoot = allLoot, medalDeathless = attempts == 1 && startPos.x <= 0.01f
                };
                // compress the height profile to ~60 columns
                int cols = Mathf.Min(60, profile.Count);
                for (int i = 0; i < cols; i++) card.profile.Add(profile[i * profile.Count / cols]);
                card.deaths.AddRange(sessionDeaths);
                hud.ShowComplete(card);
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

        void CheckCampaignComplete()
        {
            try
            {
                foreach (var info in LevelStorage.ListLevels())
                {
                    if (!info.builtIn) continue;
                    var st = info.id == level.id ? stats : LevelStatsStorage.Load(info.id);
                    if (st.completions == 0 && st.checkpointCompletions == 0 && !(info.id == level.id)) return;
                }
                Achievements.Unlock("campaign_complete");
            }
            catch (Exception)
            {
            }
        }

        void UpdatePerf(float dt)
        {
            if (!PerfHud)
            {
                if (perfTimer >= 0f) { perfTimer = -1f; hud.SetPerf(""); }
                return;
            }
            perfFps = Mathf.Lerp(perfFps <= 0f ? 60f : perfFps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.1f);
            perfTimer += dt;
            if (perfTimer < 0.5f && perfTimer >= 0f) return;
            perfTimer = 0f;
            perfDrawn = 0;
            foreach (var v in world.all) if (v != null && v.renderer2D.enabled) perfDrawn++;
            hud.SetPerf(string.Format("{0:0} fps · {1} objects · {2} drawn · {3} reactive · {4:0.0} MB", perfFps, world.all.Count, perfDrawn, reactive.Count, System.GC.GetTotalMemory(false) / 1048576f));
        }

        void Retry()
        {
            deathOverlay.Clear();
            hud.HideDeath();
            Respawn();
        }

        public void OnJumped(Vector2 pos, float up)
        {
            Haptics.Jump();
            if (level.settings.bpm >= 20f)
            {
                float beat = SongBeat();
                jumpBeats.Add(new Vector3(pos.x, pos.y, beat - Mathf.Round(beat)));
                if (jumpBeats.Count > 5000) jumpBeats.RemoveAt(0);
            }
            Sfx.Play("jump_" + player.mount.id, 0.8f, 1f, 0.05f, "jump");
            var feet = pos - new Vector2(0f, up * player.Size.y * 0.45f);
            particles.Emit(feet, new Color(0.9f, 0.85f, 0.75f, 0.7f), 4, 1.8f, 0.3f, 0.08f, 3f, up > 0 ? 270f : 90f, 120f);
            // the boar's gravity flip kicks off the surface with a skid
            if (player.mount.id == "boar") fx.Skid(feet, up, player.direction, new Color(0.5f, 0.35f, 0.2f), 0.8f);
        }

        public void OnLanded(Vector2 pos, float up, Vector2 size, float fallSpeed = 0f)
        {
            Sfx.Play("land_" + player.mount.id, 0.5f, 1f, 0.08f, "land");
            var feet = pos - new Vector2(0f, up * size.y * 0.5f);
            fx.Landing(feet, up, fallSpeed, player.mount.id);
            // hard landings thump the camera a little; ordinary hops do not
            if (fallSpeed > 16f)
            {
                float k = Mathf.Clamp01((fallSpeed - 16f) / 16f);
                playCamera.Shake(0.06f + 0.16f * k, 0.02f, 0.1f + 0.08f * k);
                particles.Emit(feet, new Color(0.85f, 0.8f, 0.7f, 0.6f), Mathf.RoundToInt(6 * k), 3.5f, 0.4f, 0.1f, 5f, up > 0 ? 90f : 270f, 170f);
            }
        }

        public void OnPlayerDied()
        {
            deathTimer = 0f;
            replayClock = 0f;
            musicRequest++;
            Haptics.Death();
            Sfx.Play("death_" + player.mount.id, 1f, 1f, 0.03f, "death");
            if (Achievements.AddCounter("deaths", 1) >= 100) Achievements.Unlock("deaths_100");
            combo = 0;
            hud.ShowCombo(0);
            var c = player.mount.Color;
            particles.Emit(player.position, c, 18, 9f, 0.8f, 0.17f, 22f);
            particles.Emit(player.position, player.mount.Accent, 8, 6f, 0.6f, 0.12f, 18f);
            playCamera.Shake(0.35f, 0.03f, 0.3f);
            hud.Flash(new Color(1f, 0.25f, 0.15f, 0.45f), 0.18f);
            float progress = Mathf.Clamp01(finishX > 0f ? player.position.x / finishX : 0f);
            hud.SetProgress(progress, true);
            sessionDeaths.Add(progress);
            DeathRecorded?.Invoke(player.position, player.deathPoint, player.killer != null ? player.killer.def.name : "the world");
            if (fullRun)
            {
                stats.RecordDeath(progress);
                stats.RecordKiller(player.killer != null ? player.killer.def.name : "the world");
                if (progress > stats.bestProgress) stats.bestProgress = progress;
                if (progress > runStartBest) CommitBestRun(progress);
                LevelStatsStorage.Save(stats);
            }
            if (bestGhost != null) bestGhost.enabled = false;
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
                // consecutive coins chime up the scale
                coinStreak = coinStreakTimer > 0f ? coinStreak + 1 : 1;
                coinStreakTimer = 1f;
                Sfx.Play("coin", 0.7f, 1f + 0.06f * Mathf.Min(coinStreak - 1, 12), 0.02f);
                if (Achievements.AddCounter("coins", 1) >= 1000) Achievements.Unlock("coins_1000");
            }
            hud.SetLoot(coins, totalCoins, gems, totalGems, keys);
            if (!lootBannerShown && coins >= totalCoins && gems >= totalGems && totalCoins + totalGems > 0)
            {
                lootBannerShown = true;
                hud.ShowLootBanner("All loot gathered!");
                hud.Flash(new Color(1f, 0.85f, 0.3f, 0.3f), 0.25f);
                Sfx.Play("gem", 1f, 1.2f);
                particles.Emit(player.position, new Color(1f, 0.85f, 0.3f), 20, 5f, 0.7f, 0.12f, 6f);
            }
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
                    if (v.def.portalType == PortalType.GravityFlip || v.def.portalType == PortalType.GravityNormal) hud.Ripple(new Color(col.r, col.g, col.b, 0.8f));
                    fx.PortalCrossed(v, player, hud);
                    playCamera.Shake(mountGate ? 0.15f : 0.06f, 0.03f, 0.18f);
                    if (mountGate) slowMo = 0.32f;
                    pulsingPortal = v;
                    portalPulse = 0.35f;
                    break;
                }
                case ObjectKind.Orb:
                    // chained runes: a rising combo with a higher pitch each time
                    combo = comboTimer > 0f ? combo + 1 : 1;
                    comboTimer = 1.4f;
                    hud.ShowCombo(combo);
                    if (v.def.orbType == OrbType.GravityFlip || v.def.orbType == OrbType.GravityJump || v.def.orbType == OrbType.DashFlip) hud.Ripple(new Color(col.r, col.g, col.b, 0.8f));
                    switch (v.def.orbType)
                    {
                        case OrbType.GravityFlip:
                        case OrbType.GravityJump: Sfx.Play("rune_gravity", 0.9f); break;
                        case OrbType.BigJump: Sfx.Play("rune_big", 0.9f); break;
                        case OrbType.Dash:
                        case OrbType.DashFlip: Sfx.Play("rune_dash", 0.9f); break;
                        case OrbType.Slam: Sfx.Play("rune_void", 0.9f); break;
                        case OrbType.Teleport: Sfx.Play("teleport", 0.8f); break;
                        default: Sfx.Play("rune", 0.9f, 1f + 0.08f * Mathf.Min(combo - 1, 8), 0.04f); break;
                    }
                    particles.Emit(v.WorldPosition, col, 10, 5f, 0.4f, 0.1f, 6f);
                    fx.RuneActivated(v, combo);
                    break;
                case ObjectKind.Pad:
                    Sfx.Play(v.def.padType == PadType.GravityFlip ? "pad_gravity" : "pad", 0.8f, v.def.padType == PadType.BigJump ? 0.85f : 1f, 0.06f);
                    particles.Emit(v.WorldPosition, col, 8, 4f, 0.35f, 0.09f, 8f, player.flipped ? 270f : 90f, 90f);
                    fx.PadTouched(v, player.flipped);
                    break;
            }
        }

        /// <summary>A hazard entered the danger halo and left it with the rider alive.</summary>
        public void OnNearMiss(int uid)
        {
            if (fx == null || !world.byUid.TryGetValue(uid, out var v) || v == null) return;
            fx.NearMiss(v.WorldPosition, player, playCamera, hud);
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

        System.Action<string> previousAnnounce;

        public void Shutdown()
        {
            Achievements.Announce = previousAnnounce;
            previousAnnounce = null;
            if (fx != null) fx.Destroy();
            if (stats != null && fullRun) LevelStatsStorage.Save(stats);
            musicRequest++;
            if (music != null) music.Stop();
            if (world != null) world.Destroy();
            cam.ResetProjectionMatrix();
            ground.showCeiling = false;
        }
    }
}
