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

        /// <summary>Determines the spawn position and initial state, honouring Start Position objects and portals to the left.</summary>
        void ComputeStart(Vector2? marker)
        {
            var s = level.settings;
            startMount = s.startMount;
            startSpeed = s.startSpeed;
            startFlipped = s.startGravityFlipped;
            startMini = s.startMini;
            startPos = new Vector2(0f, s.groundY + 0.5f);

            // right-most enabled start position (left of the marker if one is given)
            LevelObject best = null;
            foreach (var o in level.objects)
            {
                if (o.type != "start_pos" || o.GetBool("disabled")) continue;
                if (marker != null && o.x > marker.Value.x) continue;
                if (best == null || o.x > best.x) best = o;
            }
            if (best != null)
            {
                startPos = new Vector2(best.x, best.y);
                startMount = best.GetString("mount", startMount);
                startSpeed = best.GetInt("speed", startSpeed);
                startFlipped = best.GetBool("flipped", startFlipped);
                startMini = best.GetBool("mini", startMini);
            }
            if (marker != null)
            {
                float scanFrom = best != null ? best.x : -1f;
                startPos = marker.Value;
                // apply every portal between the base state and the marker
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
                        case PortalType.Mount: startMount = def.portalMount; break;
                        case PortalType.GravityNormal: startFlipped = false; break;
                        case PortalType.GravityFlip: startFlipped = true; break;
                        case PortalType.Speed: startSpeed = (int)def.portalSpeed; break;
                        case PortalType.SizeMini: startMini = true; break;
                        case PortalType.SizeNormal: startMini = false; break;
                    }
                }
            }
            startPos.x = Mathf.Max(0f, startPos.x);
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
            level.settings.bgFarOverride = pristine.settings.bgFarOverride;
            level.settings.bgMidOverride = pristine.settings.bgMidOverride;
            level.settings.bgNearOverride = pristine.settings.bgNearOverride;
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
                hud.SetProgress(1f);
                hud.ShowComplete(attempts, elapsed, player.jumps, coins, totalCoins);
                if (music != null && level.settings.fadeOut) music.Stop();
            }
        }

        public void OnPlayerDied()
        {
            respawnTimer = 0.8f;
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

        void StartMusic()
        {
            var id = level.settings.songId;
            if (string.IsNullOrEmpty(id)) return;
            PlaySong(id, level.settings.songOffset + startPos.x / MountCatalog.Speed(startSpeed), false);
        }

        public void PlaySong(string id, float offset, bool loop)
        {
            if (string.IsNullOrEmpty(id)) return;
            var clip = Resources.Load<AudioClip>("Songs/" + id);
            if (clip == null)
            {
                Debug.LogWarning("Song not found in Resources/Songs: " + id);
                return;
            }
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
            if (music != null) music.Stop();
            if (world != null) world.Destroy();
            cam.ResetProjectionMatrix();
            ground.showCeiling = false;
        }
    }
}
