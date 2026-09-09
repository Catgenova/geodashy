using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Draws classified hitbox edges (green safe, red danger, cyan interact) with a pool of thin sprites.</summary>
    public class HitboxOverlay : MonoBehaviour
    {
        public static Color SafeColor => Accessibility.SafeColor;
        public static Color DangerColor => Accessibility.DangerColor;
        public static Color InteractColor => Accessibility.InteractColor;

        public int sortingOrder = 960;
        public float thickness = 0.06f;

        readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        readonly List<HitEdge> scratch = new List<HitEdge>();
        int used;

        public static HitboxOverlay Create(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var o = go.AddComponent<HitboxOverlay>();
            o.sortingOrder = sortingOrder;
            return o;
        }

        public void Begin()
        {
            used = 0;
        }

        public void End()
        {
            for (int i = used; i < pool.Count; i++) if (pool[i].enabled) pool[i].enabled = false;
        }

        public void Clear()
        {
            Begin();
            End();
        }

        SpriteRenderer Next(Sprite sprite)
        {
            SpriteRenderer sr;
            if (used < pool.Count) sr = pool[used];
            else
            {
                sr = SpriteLibrary.CreateRenderer("seg", transform, sprite, sortingOrder);
                pool.Add(sr);
            }
            used++;
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.enabled = true;
            return sr;
        }

        public void Segment(Vector2 a, Vector2 b, Color color, float? width = null)
        {
            float t = width ?? thickness;
            var sr = Next(PlaceholderSpriteFactory.WhiteSquare());
            var d = b - a;
            float len = d.magnitude;
            sr.transform.position = new Vector3((a.x + b.x) / 2f, (a.y + b.y) / 2f, 0f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            sr.transform.localScale = new Vector3(len + t, t, 1f);
            sr.color = color;
        }

        public void Dot(Vector2 p, float radius, Color color)
        {
            var sr = Next(PlaceholderSpriteFactory.Circle());
            sr.transform.position = new Vector3(p.x, p.y, 0f);
            sr.transform.rotation = Quaternion.identity;
            sr.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            sr.color = color;
        }

        public static Color ColorFor(EdgeKind kind)
        {
            switch (kind)
            {
                case EdgeKind.Safe: return SafeColor;
                case EdgeKind.Danger: return DangerColor;
                default: return InteractColor;
            }
        }

        public void Edges(List<HitEdge> edges, float alpha = 1f)
        {
            foreach (var e in edges)
            {
                var c = ColorFor(e.kind);
                c.a *= alpha;
                Segment(e.a, e.b, c);
            }
        }

        /// <summary>Draws the classified edges of an object described by its definition and placement.</summary>
        public void DrawObject(ObjectDefinition def, Vector2 center, float rotation, Vector2 size, bool flipX, bool flipY, float alpha = 1f)
        {
            scratch.Clear();
            Hitboxes.Collect(def, center, rotation, size, flipX, flipY, scratch);
            Edges(scratch, alpha);
        }

        public void DrawView(LevelObjectView v, float alpha = 1f)
        {
            DrawObject(v.def, v.WorldPosition, v.WorldRotation, v.Size, v.data.flipX, v.data.flipY, alpha);
        }

        /// <summary>Draws only the edges of the given kind for a view.</summary>
        public void DrawViewKind(LevelObjectView v, EdgeKind kind, Color color, float width)
        {
            scratch.Clear();
            Hitboxes.Collect(v.def, v.WorldPosition, v.WorldRotation, v.Size, v.data.flipX, v.data.flipY, scratch);
            foreach (var e in scratch) if (e.kind == kind) Segment(e.a, e.b, color, width);
        }

        public void Rect(Rect r, Color color, float? width = null)
        {
            Segment(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color, width);
            Segment(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color, width);
            Segment(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color, width);
            Segment(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color, width);
        }
    }
}
