using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Geometry helpers shared by the editor and the game.</summary>
    public static class GeoMath
    {
        /// <summary>Size of an object in world units after scale (before rotation).</summary>
        public static Vector2 ObjectSize(LevelObject o, ObjectDefinition def)
        {
            return new Vector2(def.width * Mathf.Abs(o.scaleX), def.height * Mathf.Abs(o.scaleY));
        }

        /// <summary>Axis-aligned bounds of a rotated object.</summary>
        public static Rect ObjectBounds(LevelObject o, ObjectDefinition def)
        {
            var size = ObjectSize(o, def);
            float rad = o.rotation * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(rad));
            float s = Mathf.Abs(Mathf.Sin(rad));
            float w = size.x * c + size.y * s;
            float h = size.x * s + size.y * c;
            return new Rect(o.x - w / 2f, o.y - h / 2f, w, h);
        }

        /// <summary>True if the world point lies inside the object's rotated rectangle.</summary>
        public static bool PointInObject(Vector2 p, LevelObject o, ObjectDefinition def, float padding = 0f)
        {
            var size = ObjectSize(o, def);
            var local = WorldToLocal(p, new Vector2(o.x, o.y), o.rotation);
            return Mathf.Abs(local.x) <= size.x / 2f + padding && Mathf.Abs(local.y) <= size.y / 2f + padding;
        }

        public static Vector2 WorldToLocal(Vector2 p, Vector2 center, float rotationDeg)
        {
            var d = p - center;
            float rad = -rotationDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }

        public static Vector2 LocalToWorld(Vector2 local, Vector2 center, float rotationDeg)
        {
            float rad = rotationDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return center + new Vector2(local.x * c - local.y * s, local.x * s + local.y * c);
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static bool RectsOverlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
        }

        public static Rect RectFromPoints(Vector2 a, Vector2 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        public static Rect Expand(Rect r, float amount)
        {
            return new Rect(r.xMin - amount, r.yMin - amount, r.width + amount * 2f, r.height + amount * 2f);
        }

        /// <summary>Snaps a centre position so that the object sits on the grid according to its size.</summary>
        public static Vector2 SnapCenter(Vector2 p, Vector2 size, float grid)
        {
            if (grid <= 0f) return p;
            return new Vector2(SnapAxis(p.x, size.x, grid), SnapAxis(p.y, size.y, grid));
        }

        public static float SnapAxis(float v, float size, float grid)
        {
            int cells = Mathf.Max(1, Mathf.RoundToInt(size / grid));
            float offset = (cells % 2 == 1) ? grid * 0.5f : 0f;
            return Mathf.Round((v - offset) / grid) * grid + offset;
        }

        public static float SnapValue(float v, float grid)
        {
            if (grid <= 0f) return v;
            return Mathf.Round(v / grid) * grid;
        }

        public static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a < 0f) a += 360f;
            return a;
        }

        // ---- easing ----------------------------------------------------------

        public static float Ease(Easing e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Easing.Linear: return t;
                case Easing.EaseIn: return t * t;
                case Easing.EaseOut: return 1f - (1f - t) * (1f - t);
                case Easing.EaseInOut: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Easing.SineIn: return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                case Easing.SineOut: return Mathf.Sin(t * Mathf.PI / 2f);
                case Easing.SineInOut: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Easing.ExponentialIn: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case Easing.ExponentialOut: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Easing.ExponentialInOut:
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                case Easing.BackIn:
                {
                    const float c1 = 1.70158f;
                    return (c1 + 1f) * t * t * t - c1 * t * t;
                }
                case Easing.BackOut:
                {
                    const float c1 = 1.70158f;
                    float u = t - 1f;
                    return 1f + (c1 + 1f) * u * u * u + c1 * u * u;
                }
                case Easing.BackInOut:
                {
                    const float c2 = 1.70158f * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
                }
                case Easing.ElasticIn:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
                }
                case Easing.ElasticOut:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case Easing.ElasticInOut:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c5 = (2f * Mathf.PI) / 4.5f;
                    return t < 0.5f
                        ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
                        : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
                }
                case Easing.BounceOut: return BounceOut(t);
                case Easing.BounceIn: return 1f - BounceOut(1f - t);
                case Easing.BounceInOut: return t < 0.5f ? (1f - BounceOut(1f - 2f * t)) / 2f : (1f + BounceOut(2f * t - 1f)) / 2f;
            }
            return t;
        }

        static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
