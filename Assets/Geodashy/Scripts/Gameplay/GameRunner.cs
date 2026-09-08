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
        float respawnTimer = -1f;
        bool paused;
        bool complete;
        float elapsed;
        AudioSource music;
        bool wasHeld;
        Transform worldRoot;

        public void Begin(LevelData data, Camera camera_, ParallaxBackground bg, GroundRenderer gr, Vector2? start, Action exit)
        {
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
            foreach (var v in world.all) if (v.def.kind == ObjectKind.Collectible) totalCoins++;

            var playerGo = new GameObject("Rider");
            playerGo.transform.SetParent(transform, false);
            player = playerGo.AddComponent<PlayerController>();
            player.Init(this, world, level.settings);
            triggers = new TriggerSystem(world, this);
            playCamera = new PlayCamera(cam, player, level.settings);
            hud = PlayHUD.Create(transform, () => TogglePause(), RestartFromStart, Exit);

            ComputeStart(start);
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
            attempts++;
            complete = false;
            paused = false;
            elapsed = 0f;
            coins = 0;
            respawnTimer = -1f;
            ResetWorld();
            player.Spawn(startPos, startMount, startSpeed, startFlipped, startMini);
            triggers.ResetForStart(startPos.x);
            playCamera.Reset(startPos.x);
            hud.SetAttempt(attempts);
            hud.SetCoins(0, totalCoins);
            hud.HideComplete();
            hud.ShowPause(false);
            hud.ShowHint(player.mount.name + " — " + player.mount.control);
            StartMusic();
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
                player.Tick(dt);
                respawnTimer -= dt;
                if (respawnTimer <= 0f) Respawn();
                return;
            }

            elapsed += dt;
            player.SetInput(held, pressed);
            player.Tick(dt);
            triggers.Update(dt);
            world.UpdateSpinners(dt);
            world.FlushDirty();
            ground.showCeiling = player.mount.flying || player.flipped;
            playCamera.Update(dt);
            hud.SetProgress(finishX > 0f ? player.position.x / finishX : 0f);

            if (player.finished)
            {
                complete = true;
                hud.SetProgress(1f, true);
                hud.ShowComplete(attempts, elapsed, player.jumps, coins, totalCoins);
                if (music != null && level.settings.fadeOut) music.Stop();
            }
        }

        public void OnPlayerDied()
        {
            respawnTimer = 0.8f;
            musicRequest++;
            hud.SetProgress(finishX > 0f ? player.position.x / finishX : 0f, true);
            if (music != null) music.Stop();
        }

        public void OnCollect(LevelObjectView v)
        {
            coins++;
            hud.SetCoins(coins, totalCoins);
            if (v.def.id == "key") triggers.OnItemCollected(v.data.GetInt("itemId", 1));
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
            musicRequest++;
            if (music != null) music.Stop();
            if (world != null) world.Destroy();
            cam.ResetProjectionMatrix();
            ground.showCeiling = false;
        }
    }
}
