using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Gameplay
{
    /// <summary>Executes triggers during play: movement, rotation, colour, alpha, toggle, spawn, pulse, shake, camera, etc.</summary>
    public class TriggerSystem
    {
        abstract class Tween
        {
            public int targetGroup;
            public float time;
            public float duration;
            public Easing easing;
            public bool done;
            public abstract void Update(TriggerSystem sys, float dt);

            protected float Progress()
            {
                if (duration <= 0.0001f) return 1f;
                return Mathf.Clamp01(time / duration);
            }
        }

        class MoveTween : Tween
        {
            public Vector2 total;
            public Vector2 applied;
            public bool lockX, lockY;
            public List<LevelObjectView> objects;

            public override void Update(TriggerSystem sys, float dt)
            {
                time += dt;
                float e = GeoMath.Ease(easing, Progress());
                var target = total * e;
                var delta = target - applied;
                applied = target;
                if (lockX) delta.x += sys.playerDelta.x;
                if (lockY) delta.y += sys.playerDelta.y;
                foreach (var v in objects)
                {
                    v.runtimeOffset += delta;
                    sys.world.MarkDirty(v);
                }
                if (Progress() >= 1f && !lockX && !lockY) done = true;
                if ((lockX || lockY) && time >= duration) done = true;
            }
        }

        class RotateTween : Tween
        {
            public float total;
            public float applied;
            public bool lockRot;
            public LevelObjectView center;
            public List<LevelObjectView> objects;

            public override void Update(TriggerSystem sys, float dt)
            {
                time += dt;
                float e = GeoMath.Ease(easing, Progress());
                float target = total * e;
                float delta = target - applied;
                applied = target;
                foreach (var v in objects)
                {
                    if (center != null && center != v)
                    {
                        var c = center.WorldPosition;
                        var p = v.WorldPosition;
                        var np = GeoMath.Rotate(p - c, delta) + c;
                        v.runtimeOffset += np - p;
                    }
                    if (!lockRot) v.runtimeRotation += delta;
                    sys.world.MarkDirty(v);
                }
                if (Progress() >= 1f) done = true;
            }
        }

        class AlphaTween : Tween
        {
            public float from, to;
            public List<LevelObjectView> objects;

            public override void Update(TriggerSystem sys, float dt)
            {
                time += dt;
                float a = Mathf.Lerp(from, to, Progress());
                foreach (var v in objects)
                {
                    v.runtimeAlpha = a;
                    sys.world.MarkDirty(v);
                }
                if (Progress() >= 1f) done = true;
            }
        }

        class ColorTween : Tween
        {
            public int channel;
            public Color from, to;

            public override void Update(TriggerSystem sys, float dt)
            {
                time += dt;
                sys.SetChannelColor(channel, Color.Lerp(from, to, Progress()));
                if (Progress() >= 1f) done = true;
            }
        }

        class PulseTween : Tween
        {
            public Color color;
            public float fadeIn, hold, fadeOut;
            public List<LevelObjectView> objects;
            public int channel = -1;

            public override void Update(TriggerSystem sys, float dt)
            {
                time += dt;
                float a;
                if (time < fadeIn) a = fadeIn > 0 ? time / fadeIn : 1f;
                else if (time < fadeIn + hold) a = 1f;
                else if (time < fadeIn + hold + fadeOut) a = 1f - (time - fadeIn - hold) / Mathf.Max(0.001f, fadeOut);
                else
                {
                    a = 0f;
                    done = true;
                }
                var c = color;
                c.a = a * color.a;
                if (objects != null)
                {
                    foreach (var v in objects)
                    {
                        v.pulseColor = c;
                        sys.world.MarkDirty(v);
                    }
                }
                else sys.SetChannelPulse(channel, c);
            }
        }

        class FollowTween : Tween
        {
            public List<LevelObjectView> objects;
            public LevelObjectView leader;
            public Vector2 lastLeader;
            public float xMod = 1f, yMod = 1f;

            public override void Update(TriggerSystem sys, float dt)
            {
                time += dt;
                if (leader == null)
                {
                    done = true;
                    return;
                }
                var now = leader.runtimeOffset;
                var delta = now - lastLeader;
                lastLeader = now;
                delta = new Vector2(delta.x * xMod, delta.y * yMod);
                if (delta.sqrMagnitude > 0f)
                {
                    foreach (var v in objects)
                    {
                        if (v == leader) continue;
                        v.runtimeOffset += delta;
                        sys.world.MarkDirty(v);
                    }
                }
                if (time >= duration) done = true;
            }
        }

        class SpawnDelay
        {
            public int group;
            public float remaining;
        }

        public class TouchListener
        {
            public int group;
            public bool holdMode;
            public string toggleMode;
        }

        public LevelWorld world;
        public GameRunner runner;
        public Vector2 playerDelta;

        readonly List<Tween> tweens = new List<Tween>();
        readonly List<SpawnDelay> spawnQueue = new List<SpawnDelay>();
        readonly List<TouchListener> touchListeners = new List<TouchListener>();
        readonly HashSet<int> fired = new HashSet<int>();
        readonly Dictionary<int, List<LevelObjectView>> channelUsers = new Dictionary<int, List<LevelObjectView>>();
        readonly Dictionary<int, Color> channelPulse = new Dictionary<int, Color>();
        readonly Dictionary<int, int> itemCounts = new Dictionary<int, int>();
        int nextTriggerIndex;

        public TriggerSystem(LevelWorld world, GameRunner runner)
        {
            this.world = world;
            this.runner = runner;
            foreach (var v in world.all)
            {
                int ch = v.data.baseColor;
                if (ch == ColorChannelIds.Default) continue;
                if (!channelUsers.TryGetValue(ch, out var list)) channelUsers[ch] = list = new List<LevelObjectView>();
                list.Add(v);
            }
        }

        public class Snapshot
        {
            public int nextTriggerIndex;
            public HashSet<int> fired;
            public List<TouchListener> touchListeners;
            public Dictionary<int, int> itemCounts;
        }

        /// <summary>Captures firing progress. Running tweens are not captured; object positions come from the world snapshot.</summary>
        public Snapshot Capture()
        {
            return new Snapshot
            {
                nextTriggerIndex = nextTriggerIndex,
                fired = new HashSet<int>(fired),
                touchListeners = new List<TouchListener>(touchListeners),
                itemCounts = new Dictionary<int, int>(itemCounts)
            };
        }

        public void Restore(Snapshot s)
        {
            tweens.Clear();
            spawnQueue.Clear();
            channelPulse.Clear();
            nextTriggerIndex = s.nextTriggerIndex;
            fired.Clear();
            fired.UnionWith(s.fired);
            touchListeners.Clear();
            touchListeners.AddRange(s.touchListeners);
            itemCounts.Clear();
            foreach (var kv in s.itemCounts) itemCounts[kv.Key] = kv.Value;
        }

        /// <summary>Resets progress-based firing so triggers left of x are considered already passed.</summary>
        public void ResetForStart(float startX)
        {
            tweens.Clear();
            spawnQueue.Clear();
            touchListeners.Clear();
            fired.Clear();
            channelPulse.Clear();
            itemCounts.Clear();
            nextTriggerIndex = 0;
            while (nextTriggerIndex < world.triggers.Count && world.triggers[nextTriggerIndex].data.x < startX) nextTriggerIndex++;
        }

        public void PlayerAdvanced(PlayerController player, Vector2 from, Vector2 to)
        {
            playerDelta = to - from;
            // position-based triggers
            while (nextTriggerIndex < world.triggers.Count && world.triggers[nextTriggerIndex].data.x <= to.x)
            {
                var t = world.triggers[nextTriggerIndex++];
                if (t.data.GetBool("touch") || t.data.GetBool("spawn")) continue;
                Fire(t);
            }
            // touch triggers
            var pb = player.Bounds;
            foreach (var t in world.triggers)
            {
                if (!t.data.GetBool("touch")) continue;
                if (t.data.x > to.x + 3f) break;
                if (fired.Contains(t.data.uid) && !t.data.GetBool("multi")) continue;
                if (GeoMath.RectsOverlap(pb, t.Bounds)) Fire(t);
            }
        }

        public void Update(float dt)
        {
            for (int i = spawnQueue.Count - 1; i >= 0; i--)
            {
                spawnQueue[i].remaining -= dt;
                if (spawnQueue[i].remaining <= 0f)
                {
                    int g = spawnQueue[i].group;
                    spawnQueue.RemoveAt(i);
                    FireGroup(g);
                }
            }
            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                tweens[i].Update(this, dt);
                if (tweens[i].done) tweens.RemoveAt(i);
            }
        }

        public void OnPress()
        {
            foreach (var l in touchListeners) TouchFire(l);
        }

        public void OnRelease()
        {
            foreach (var l in touchListeners) if (l.holdMode) TouchFire(l);
        }

        void TouchFire(TouchListener l)
        {
            switch (l.toggleMode)
            {
                case "ToggleOn": SetGroupActive(l.group, true); break;
                case "ToggleOff": SetGroupActive(l.group, false); break;
                default: FireGroup(l.group); break;
            }
        }

        public void SpawnGroup(int group, float delay)
        {
            if (delay <= 0f) FireGroup(group);
            else spawnQueue.Add(new SpawnDelay { group = group, remaining = delay });
        }

        void FireGroup(int group)
        {
            var list = world.Group(group);
            if (list == null) return;
            foreach (var v in list)
            {
                if (v.def.kind == ObjectKind.Trigger) Fire(v, true);
            }
        }

        public void Fire(LevelObjectView t, bool spawned = false)
        {
            if (!t.runtimeActive) return;
            if (fired.Contains(t.data.uid) && !t.data.GetBool("multi")) return;
            fired.Add(t.data.uid);
            var d = t.data;
            var def = t.def;
            float duration = d.GetFloat("duration", 0.5f);
            var easing = d.GetEnum("easing", Easing.Linear);
            int target = d.GetInt("target", 0);
            var group = world.Group(target);

            switch (def.triggerType)
            {
                case TriggerType.Move:
                    if (group != null)
                        tweens.Add(new MoveTween
                        {
                            targetGroup = target, duration = duration, easing = easing, objects = group,
                            total = new Vector2(d.GetFloat("moveX"), d.GetFloat("moveY")), lockX = d.GetBool("lockX"), lockY = d.GetBool("lockY")
                        });
                    break;
                case TriggerType.Rotate:
                {
                    if (group == null) break;
                    var centerList = world.Group(d.GetInt("center", 0));
                    tweens.Add(new RotateTween
                    {
                        targetGroup = target, duration = duration, easing = easing, objects = group,
                        total = -d.GetFloat("degrees", 360f) * Mathf.Max(0f, d.GetFloat("times", 1f)),
                        lockRot = d.GetBool("lockRot"), center = centerList != null && centerList.Count > 0 ? centerList[0] : null
                    });
                    break;
                }
                case TriggerType.Alpha:
                    if (group != null)
                        tweens.Add(new AlphaTween { targetGroup = target, duration = duration, objects = group, from = group[0].runtimeAlpha, to = d.GetFloat("opacity", 0f) });
                    break;
                case TriggerType.Toggle:
                    SetGroupActive(target, d.GetBool("activate", true));
                    break;
                case TriggerType.Spawn:
                    SpawnGroup(target, d.GetFloat("delay", 0f));
                    break;
                case TriggerType.Color:
                {
                    int ch = d.GetInt("channel", 1);
                    var to = d.GetColor("color", Color.white);
                    to.a = d.GetFloat("opacity", 1f);
                    tweens.Add(new ColorTween { targetGroup = -ch, channel = ch, duration = duration, from = GetChannelColor(ch), to = to });
                    break;
                }
                case TriggerType.Pulse:
                {
                    bool channelMode = d.GetString("mode", "Group") == "Channel";
                    var pt = new PulseTween
                    {
                        color = d.GetColor("color", Color.white), fadeIn = d.GetFloat("fadeIn", 0.1f), hold = d.GetFloat("hold", 0.2f), fadeOut = d.GetFloat("fadeOut", 0.3f)
                    };
                    if (channelMode) pt.channel = target;
                    else pt.objects = group;
                    if (channelMode || group != null) tweens.Add(pt);
                    break;
                }
                case TriggerType.Shake:
                    runner.playCamera.Shake(d.GetFloat("strength", 0.3f), d.GetFloat("interval", 0.03f), duration);
                    break;
                case TriggerType.Follow:
                {
                    var leaderGroup = world.Group(d.GetInt("follow", 0));
                    if (group != null && leaderGroup != null && leaderGroup.Count > 0)
                        tweens.Add(new FollowTween
                        {
                            targetGroup = target, duration = duration, objects = group, leader = leaderGroup[0], lastLeader = leaderGroup[0].runtimeOffset,
                            xMod = d.GetFloat("xMod", 1f), yMod = d.GetFloat("yMod", 1f)
                        });
                    break;
                }
                case TriggerType.Stop:
                    tweens.RemoveAll(tw => tw.targetGroup == target);
                    spawnQueue.RemoveAll(s => s.group == target);
                    break;
                case TriggerType.CameraZoom:
                    runner.playCamera.SetZoom(d.GetFloat("zoom", 1f), duration, easing);
                    break;
                case TriggerType.CameraOffset:
                    runner.playCamera.SetOffset(new Vector2(d.GetFloat("offsetX"), d.GetFloat("offsetY")), duration, easing);
                    break;
                case TriggerType.CameraStatic:
                    runner.playCamera.SetStatic(group != null && group.Count > 0 ? group[0] : null, duration, d.GetBool("followPlayer"));
                    break;
                case TriggerType.BackgroundSwitch:
                    runner.background.SwitchTheme(d.GetString("theme", "castle"), duration);
                    break;
                case TriggerType.GroundSwitch:
                    runner.ground.SetTheme(d.GetString("theme", "stone"));
                    break;
                case TriggerType.HidePlayer:
                    runner.player.visible = false;
                    break;
                case TriggerType.ShowPlayer:
                    runner.player.visible = true;
                    break;
                case TriggerType.Touch:
                    touchListeners.Add(new TouchListener { group = target, holdMode = d.GetBool("holdMode"), toggleMode = d.GetString("toggleMode", "Spawn") });
                    break;
                case TriggerType.Random:
                {
                    float chance = d.GetFloat("chance", 50f) / 100f;
                    int g = Random.value < chance ? d.GetInt("target1", 0) : d.GetInt("target2", 0);
                    SpawnGroup(g, 0f);
                    break;
                }
                case TriggerType.Song:
                    runner.PlaySong(d.GetString("song", ""), d.GetFloat("offset", 0f), d.GetBool("loop"));
                    break;
                case TriggerType.Reverse:
                    runner.player.direction *= -1;
                    break;
                case TriggerType.Count:
                    // handled on collect; store the trigger so OnCollect can evaluate it
                    break;
            }
        }

        public void SetGroupActive(int group, bool active)
        {
            var list = world.Group(group);
            if (list == null) return;
            foreach (var v in list)
            {
                v.runtimeActive = active;
                world.MarkDirty(v);
            }
        }

        // ---- colour channels ----------------------------------------------------

        public Color GetChannelColor(int ch)
        {
            return world.level.ResolveColor(ch, Color.white);
        }

        public void SetChannelColor(int ch, Color c)
        {
            var s = world.level.settings;
            switch (ch)
            {
                case ColorChannelIds.Background:
                    s.backgroundColor = c;
                    runner.background.SetBackgroundColor(c);
                    break;
                case ColorChannelIds.Ground:
                    s.groundColor = c;
                    runner.ground.SetGroundColor(c);
                    break;
                case ColorChannelIds.Line:
                    s.lineColor = c;
                    runner.ground.SetLineColor(c);
                    break;
                case ColorChannelIds.Object:
                    s.objectColor = c;
                    break;
                default:
                {
                    var channel = world.level.GetOrCreateColorChannel(ch);
                    channel.color = new Color(c.r, c.g, c.b, 1f);
                    channel.opacity = c.a;
                    break;
                }
            }
            RefreshChannelUsers(ch);
        }

        void SetChannelPulse(int ch, Color c)
        {
            channelPulse[ch] = c;
            if (!channelUsers.TryGetValue(ch, out var list)) return;
            foreach (var v in list)
            {
                v.pulseColor = c;
                world.MarkDirty(v);
            }
        }

        void RefreshChannelUsers(int ch)
        {
            if (!channelUsers.TryGetValue(ch, out var list)) return;
            foreach (var v in list) world.MarkDirty(v);
        }

        // ---- items ----------------------------------------------------------------

        public void OnItemCollected(int itemId)
        {
            itemCounts.TryGetValue(itemId, out var n);
            n++;
            itemCounts[itemId] = n;
            foreach (var t in world.triggers)
            {
                if (t.def.triggerType != TriggerType.Count) continue;
                if (t.data.GetInt("itemId", 1) != itemId) continue;
                if (n >= t.data.GetInt("count", 1) && !fired.Contains(t.data.uid))
                {
                    fired.Add(t.data.uid);
                    SetGroupActive(t.data.GetInt("target", 0), t.data.GetBool("activate", true));
                }
            }
        }
    }
}
