using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    public enum EdgeKind
    {
        Safe,     // you can land on / touch this edge
        Danger,   // touching this edge kills
        Interact  // orbs, pads, portals, loot
    }

    public struct HitEdge
    {
        public Vector2 a;
        public Vector2 b;
        public EdgeKind kind;

        public HitEdge(Vector2 a, Vector2 b, EdgeKind kind)
        {
            this.a = a;
            this.b = b;
            this.kind = kind;
        }
    }

    /// <summary>
    /// The single source of truth for collision shapes. PlayerController collides against these,
    /// and the editor / death overlays draw exactly the same geometry.
    /// </summary>
    public static class Hitboxes
    {
        public struct HazardShape
        {
            public bool circle;
            public float radius;
            /// <summary>Half extents of the box in unrotated local space.</summary>
            public Vector2 half;
            /// <summary>Box centre offset in unrotated local space.</summary>
            public Vector2 offset;
        }

        /// <summary>Death hitbox of a hazard, given its scaled size (before rotation).</summary>
        public static HazardShape Hazard(ObjectDefinition def, Vector2 size)
        {
            switch (def.collider)
            {
                case ColliderShape.Triangle:
                    return new HazardShape { half = new Vector2(size.x * 0.18f, size.y * 0.35f), offset = new Vector2(0f, -size.y * 0.1f) };
                case ColliderShape.Circle:
                    return new HazardShape { circle = true, radius = Mathf.Min(size.x, size.y) * 0.5f * def.hitboxScale };
                default:
                    return new HazardShape { half = size * 0.5f * def.hitboxScale };
            }
        }

        /// <summary>Touch radius of orbs and loot (matches PlayerController.CircleTouch).</summary>
        public static float TouchRadius(ObjectDefinition def, Vector2 size)
        {
            return Mathf.Max(size.x, size.y) * 0.5f * def.hitboxScale;
        }

        /// <summary>Axis-aligned bounds of a rotated box.</summary>
        public static Rect Aabb(Vector2 center, Vector2 size, float rotation)
        {
            float rad = rotation * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(rad)), s = Mathf.Abs(Mathf.Sin(rad));
            float w = size.x * c + size.y * s;
            float h = size.x * s + size.y * c;
            return new Rect(center.x - w / 2f, center.y - h / 2f, w, h);
        }

        public static void AabbEdges(Rect r, EdgeKind top, EdgeKind bottom, EdgeKind sides, List<HitEdge> outEdges)
        {
            var bl = new Vector2(r.xMin, r.yMin);
            var br = new Vector2(r.xMax, r.yMin);
            var tl = new Vector2(r.xMin, r.yMax);
            var tr = new Vector2(r.xMax, r.yMax);
            outEdges.Add(new HitEdge(tl, tr, top));
            outEdges.Add(new HitEdge(bl, br, bottom));
            outEdges.Add(new HitEdge(bl, tl, sides));
            outEdges.Add(new HitEdge(br, tr, sides));
        }

        /// <summary>Edge of an axis-aligned box by name ("top", "bottom", "left", "right").</summary>
        public static HitEdge AabbEdge(Rect r, string which, EdgeKind kind)
        {
            switch (which)
            {
                case "top": return new HitEdge(new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), kind);
                case "bottom": return new HitEdge(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), kind);
                case "left": return new HitEdge(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), kind);
                default: return new HitEdge(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), kind);
            }
        }

        /// <summary>
        /// Collects the classified collision edges of an object placed at centre with the given rotation,
        /// scaled size and flips. Circles are returned as 28-segment polygons.
        /// </summary>
        public static void Collect(ObjectDefinition def, Vector2 center, float rotation, Vector2 size, bool flipX, bool flipY, List<HitEdge> outEdges)
        {
            if (def == null) return;
            switch (def.kind)
            {
                case ObjectKind.Solid:
                    AabbEdges(Aabb(center, size, rotation), EdgeKind.Safe, EdgeKind.Safe, EdgeKind.Danger, outEdges);
                    break;

                case ObjectKind.Slope:
                {
                    float rot = GeoMath.NormalizeAngle(rotation);
                    bool fx = flipX, fy = flipY;
                    if (Mathf.Abs(Mathf.DeltaAngle(rot, 180f)) < 1f)
                    {
                        fx = !fx;
                        fy = !fy;
                    }
                    else if (Mathf.Abs(Mathf.DeltaAngle(rot, 0f)) > 1f)
                    {
                        AabbEdges(Aabb(center, size, rotation), EdgeKind.Safe, EdgeKind.Safe, EdgeKind.Danger, outEdges);
                        break;
                    }
                    var b = new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y);
                    // surface runs from the low corner to the high corner
                    Vector2 low, high, backTop, backBottom;
                    if (!fy)
                    {
                        low = fx ? new Vector2(b.xMax, b.yMin) : new Vector2(b.xMin, b.yMin);
                        high = fx ? new Vector2(b.xMin, b.yMax) : new Vector2(b.xMax, b.yMax);
                        backBottom = fx ? new Vector2(b.xMin, b.yMin) : new Vector2(b.xMax, b.yMin);
                        backTop = high;
                        outEdges.Add(new HitEdge(low, high, EdgeKind.Safe));
                        outEdges.Add(new HitEdge(backBottom, backTop, EdgeKind.Danger));
                        outEdges.Add(new HitEdge(new Vector2(b.xMin, b.yMin), new Vector2(b.xMax, b.yMin), EdgeKind.Safe));
                    }
                    else
                    {
                        low = fx ? new Vector2(b.xMax, b.yMax) : new Vector2(b.xMin, b.yMax);
                        high = fx ? new Vector2(b.xMin, b.yMin) : new Vector2(b.xMax, b.yMin);
                        backTop = fx ? new Vector2(b.xMin, b.yMax) : new Vector2(b.xMax, b.yMax);
                        backBottom = high;
                        outEdges.Add(new HitEdge(low, high, EdgeKind.Safe));
                        outEdges.Add(new HitEdge(backBottom, backTop, EdgeKind.Danger));
                        outEdges.Add(new HitEdge(new Vector2(b.xMin, b.yMax), new Vector2(b.xMax, b.yMax), EdgeKind.Safe));
                    }
                    break;
                }

                case ObjectKind.Hazard:
                {
                    var hs = Hazard(def, size);
                    if (hs.circle)
                    {
                        CircleEdges(center, hs.radius, EdgeKind.Danger, outEdges);
                        break;
                    }
                    float sx = flipX ? -1f : 1f, sy = flipY ? -1f : 1f;
                    Vector2 L(float x, float y) => GeoMath.LocalToWorld(new Vector2((hs.offset.x + x) * sx, (hs.offset.y + y) * sy), center, rotation);
                    var p0 = L(-hs.half.x, -hs.half.y);
                    var p1 = L(hs.half.x, -hs.half.y);
                    var p2 = L(hs.half.x, hs.half.y);
                    var p3 = L(-hs.half.x, hs.half.y);
                    outEdges.Add(new HitEdge(p0, p1, EdgeKind.Danger));
                    outEdges.Add(new HitEdge(p1, p2, EdgeKind.Danger));
                    outEdges.Add(new HitEdge(p2, p3, EdgeKind.Danger));
                    outEdges.Add(new HitEdge(p3, p0, EdgeKind.Danger));
                    break;
                }

                case ObjectKind.Orb:
                case ObjectKind.Collectible:
                    CircleEdges(center, TouchRadius(def, size), EdgeKind.Interact, outEdges);
                    break;

                case ObjectKind.Pad:
                case ObjectKind.Portal:
                    AabbEdges(Aabb(center, size, rotation), EdgeKind.Interact, EdgeKind.Interact, EdgeKind.Interact, outEdges);
                    break;
            }
        }

        public static void CircleEdges(Vector2 c, float r, EdgeKind kind, List<HitEdge> outEdges, int segments = 28)
        {
            var prev = c + new Vector2(r, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var p = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                outEdges.Add(new HitEdge(prev, p, kind));
                prev = p;
            }
        }
    }
}
