using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Gameplay
{
    /// <summary>
    /// Runtime container for a level being played: spatial buckets for static objects,
    /// a dynamic list for objects that triggers move, group lookup and trigger ordering.
    /// </summary>
    public class LevelWorld
    {
        public const float BucketSize = 4f;

        public LevelData level;
        public Transform root;
        public readonly List<LevelObjectView> all = new List<LevelObjectView>();
        public readonly Dictionary<int, LevelObjectView> byUid = new Dictionary<int, LevelObjectView>();
        public readonly Dictionary<int, List<LevelObjectView>> groups = new Dictionary<int, List<LevelObjectView>>();
        public readonly List<LevelObjectView> triggers = new List<LevelObjectView>();
        public readonly List<LevelObjectView> spinners = new List<LevelObjectView>();
        public readonly HashSet<LevelObjectView> dynamicObjects = new HashSet<LevelObjectView>();
        public readonly HashSet<LevelObjectView> dirty = new HashSet<LevelObjectView>();
        readonly Dictionary<int, List<LevelObjectView>> buckets = new Dictionary<int, List<LevelObjectView>>();
        readonly HashSet<LevelObjectView> querySeen = new HashSet<LevelObjectView>();

        public LevelWorld(LevelData level, Transform root)
        {
            this.level = level;
            this.root = root;
            Build();
        }

        void Build()
        {
            foreach (var o in level.objects)
            {
                var def = ObjectCatalog.Get(o.type);
                if (def == null) continue;
                var v = LevelObjectView.Create(root, o, level, true);
                all.Add(v);
                byUid[o.uid] = v;
                foreach (var g in o.groups)
                {
                    if (!groups.TryGetValue(g, out var list)) groups[g] = list = new List<LevelObjectView>();
                    list.Add(v);
                }
                if (def.kind == ObjectKind.Trigger) triggers.Add(v);
                if (def.spins) spinners.Add(v);
            }
            triggers.Sort((a, b) => a.data.x.CompareTo(b.data.x));

            // objects targeted by movement triggers are checked every frame instead of via buckets
            foreach (var t in triggers)
            {
                var tt = t.def.triggerType;
                if (tt == TriggerType.Move || tt == TriggerType.Rotate || tt == TriggerType.Follow)
                {
                    int g = t.data.GetInt("target", 0);
                    if (groups.TryGetValue(g, out var list)) foreach (var v in list) dynamicObjects.Add(v);
                    int c = t.data.GetInt("center", 0);
                    if (tt == TriggerType.Rotate && groups.TryGetValue(c, out var clist)) foreach (var v in clist) dynamicObjects.Add(v);
                }
            }

            foreach (var v in all)
            {
                if (dynamicObjects.Contains(v)) continue;
                if (v.def.kind == ObjectKind.Decoration || v.def.kind == ObjectKind.Trigger || v.def.kind == ObjectKind.Text || v.def.kind == ObjectKind.StartPos) continue;
                var b = v.Bounds;
                int x0 = Mathf.FloorToInt(b.xMin / BucketSize), x1 = Mathf.FloorToInt(b.xMax / BucketSize);
                for (int x = x0; x <= x1; x++)
                {
                    if (!buckets.TryGetValue(x, out var list)) buckets[x] = list = new List<LevelObjectView>();
                    list.Add(v);
                }
            }
        }

        public List<LevelObjectView> Group(int id)
        {
            return groups.TryGetValue(id, out var list) ? list : null;
        }

        /// <summary>All active, collidable objects whose bounds overlap the rect (static via buckets + all dynamic).</summary>
        public List<LevelObjectView> Query(Rect r)
        {
            var queryScratch = new List<LevelObjectView>();
            querySeen.Clear();
            int x0 = Mathf.FloorToInt(r.xMin / BucketSize), x1 = Mathf.FloorToInt(r.xMax / BucketSize);
            for (int x = x0; x <= x1; x++)
            {
                if (!buckets.TryGetValue(x, out var list)) continue;
                foreach (var v in list)
                {
                    if (!v.runtimeActive || querySeen.Contains(v)) continue;
                    if (!GeoMath.RectsOverlap(r, v.Bounds)) continue;
                    querySeen.Add(v);
                    queryScratch.Add(v);
                }
            }
            foreach (var v in dynamicObjects)
            {
                if (!v.runtimeActive || querySeen.Contains(v)) continue;
                if (v.def.kind == ObjectKind.Decoration || v.def.kind == ObjectKind.Trigger || v.def.kind == ObjectKind.Text) continue;
                if (!GeoMath.RectsOverlap(r, v.Bounds)) continue;
                querySeen.Add(v);
                queryScratch.Add(v);
            }
            return queryScratch;
        }

        public void MarkDirty(LevelObjectView v) => dirty.Add(v);

        /// <summary>Pushes runtime state (offsets, rotation, alpha, active) into the scene objects.</summary>
        public void FlushDirty()
        {
            foreach (var v in dirty)
            {
                if (v == null) continue;
                v.ApplyTransform();
                v.ApplyColor();
                bool visible = v.runtimeActive && !v.def.HiddenInPlay && v.def.id != "invisible_block";
                v.renderer2D.enabled = visible;
            }
            dirty.Clear();
        }

        public void UpdateSpinners(float dt)
        {
            foreach (var v in spinners)
            {
                v.spinAngle += v.data.GetFloat("spin", 180f) * dt;
                v.ApplyTransform();
            }
        }

        public void Destroy()
        {
            foreach (var v in all) if (v != null) Object.Destroy(v.gameObject);
            all.Clear();
        }
    }
}
