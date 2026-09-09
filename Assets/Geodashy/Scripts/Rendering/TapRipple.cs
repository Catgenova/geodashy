using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Expanding, fading rings at tap positions so touch input feels acknowledged.</summary>
    public class TapRipple : MonoBehaviour
    {
        class Ring
        {
            public SpriteRenderer sr;
            public float age;
            public bool active;
        }

        const float Life = 0.35f;
        readonly List<Ring> rings = new List<Ring>();
        static Sprite ringSprite;
        public int sortingOrder = 990;
        public Color color = new Color(1f, 1f, 1f, 0.8f);

        public static TapRipple Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("Tap Ripple");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TapRipple>();
            t.sortingOrder = sortingOrder;
            return t;
        }

        static Sprite RingSprite()
        {
            if (ringSprite != null) return ringSprite;
            var r = new Raster(64, 64);
            r.Clear(new Color(0, 0, 0, 0));
            r.Ring(32, 32, 31, 26, Color.white);
            ringSprite = r.ToSprite(64f);
            ringSprite.name = "tap_ring";
            return ringSprite;
        }

        public void Spawn(Vector2 world, float size = 0.9f)
        {
            Ring ring = null;
            foreach (var r in rings) if (!r.active) { ring = r; break; }
            if (ring == null)
            {
                var go = new GameObject("Ring");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = RingSprite();
                SpriteLibrary.ApplyMaterial(sr);
                sr.sortingOrder = sortingOrder;
                ring = new Ring { sr = sr };
                rings.Add(ring);
            }
            ring.active = true;
            ring.age = 0f;
            ring.sr.enabled = true;
            ring.sr.transform.position = new Vector3(world.x, world.y, 0f);
            ring.sr.transform.localScale = Vector3.one * size * 0.3f;
            ring.sr.color = color;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var r in rings)
            {
                if (!r.active) continue;
                r.age += dt;
                float t = r.age / Life;
                if (t >= 1f)
                {
                    r.active = false;
                    r.sr.enabled = false;
                    continue;
                }
                float s = Mathf.Lerp(0.3f, 1.2f, 1f - (1f - t) * (1f - t));
                r.sr.transform.localScale = Vector3.one * s;
                var c = color;
                c.a *= 1f - t;
                r.sr.color = c;
            }
        }
    }
}
