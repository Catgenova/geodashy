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

        public bool practice;
        public bool autoCheckpoints = true;
        readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
        int currentCheckpoint = -1;
        float autoCheckpointTimer;
        float lastCheckpointX = -100f;
        public int CheckpointCount => checkpoints.Count;

        public void Begin(LevelData data, Camera camera_, ParallaxBackground bg, GroundRenderer gr, Vector2? start, Action exit, bool practiceMode = false, string exitTarget = "editor")
        {
            practice = practiceMode;
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
            hud.SetExitTarget(exitTarget);

            ComputeStart(start);
            fullRun = start == null && !practice;
            stats = LevelStatsStorage.Load(level.id);
            attempts = 0;
            Respawn();
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
            if (practice && checkpoints.Count > 0)
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
            autoCheckpointTimer = 0f;
            lastCheckpointX = -100f;
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
            hud.SetPractice(practice, checkpoints.Count, autoCheckpoints, currentCheckpoint);
            hud.ShowHint(player.mount.name + " — " + player.mount.control + (practice ? "   ·   Z raises a waystone, X removes it, ← → scrub between them" : ""));
            StartMusic();
        }

        // ---- practice mode -----------------------------------------------------------

        public void TogglePractice()
        {
            SetPractice(!practice);
        }

        public void SetPractice(bool on)
        {
            if (practice == on) return;
            practice = on;
            fullRun = false; // a run that touched practice never counts for stats
            if (!on) ClearCheckpoints();
            hud.SetPractice(practice, checkpoints.Count, autoCheckpoints, currentCheckpoint);
            hud.ShowHint(on ? "Squire mode: Z raises a waystone, X removes the last one, ← → jump between waystones. Deaths return you to the last waystone." : "Knight mode: deaths return you to the start.", 5f);
        }

        public void PlaceCheckpoint()
        {
            if (!practice || player.dead || complete || paused) return;
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
            cp.marker = CreateCheckpointMarker(player.position);
            checkpoints.Add(cp);
            currentCheckpoint = checkpoints.Count - 1;
            lastCheckpointX = player.position.x;
            autoCheckpointTimer = 0f;
            hud.SetPractice(practice, checkpoints.Count, autoCheckpoints, currentCheckpoint);
        }

        public void RemoveCheckpoint()
        {
            if (!practice || checkpoints.Count == 0) return;
            var cp = checkpoints[checkpoints.Count - 1];
            checkpoints.RemoveAt(checkpoints.Count - 1);
            if (cp.marker != null) Destroy(cp.marker);
            lastCheckpointX = checkpoints.Count > 0 ? checkpoints[checkpoints.Count - 1].player.position.x : -100f;
            if (currentCheckpoint >= checkpoints.Count) currentCheckpoint = checkpoints.Count - 1;
            hud.SetPractice(practice, checkpoints.Count, autoCheckpoints, currentCheckpoint);
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
            hud.SetPractice(practice, checkpoints.Count, autoCheckpoints, currentCheckpoint);

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
                DrawDeathOverlay();
                if (deathTimer > 0.35f && (pressed || (kb != null && kb.enterKey.wasPressedThisFrame))) Retry();
                return;
            }

            elapsed += dt;
            player.SetInput(held, pressed);
            player.Tick(dt);
            UpdateAutoCheckpoint(dt);
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
                hud.SetProgress(1f, true);
                if (fullRun)
                {
                    stats.bestProgress = 1f;
                    stats.completions++;
                    LevelStatsStorage.Save(stats);
                }
                hud.SetMarkers(stats.deaths, sessionDeaths, stats.bestProgress);
                hud.ShowComplete(attempts, elapsed, player.jumps, coins, totalCoins, stats.attempts, stats.completions, gems, totalGems);
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

        public void OnPlayerDied()
        {
            deathTimer = 0f;
            musicRequest++;
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
            if (v.def.id == "key")
            {
                keys++;
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
            }
            else if (v.def.id == "gem") gems++;
            else coins++;
            hud.SetLoot(coins, totalCoins, gems, totalGems, keys);
        }

        public void OnInteract(LevelObjectView v)
        {
            // hook for particles / sound
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
