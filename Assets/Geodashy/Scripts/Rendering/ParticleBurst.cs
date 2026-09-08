using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Tiny pooled particle system for shards, dust and portal sparks. Runs on unscaled game time so it animates during the death freeze.</summary>
    public class ParticleBurst : MonoBehaviour
    {
        class Particle
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float life, maxLife, size, gravity, spin, angle;
            public Color color;
            public bool active;
        }

        readonly List<Particle> pool = new List<Particle>();
        public int sortingOrder = 50;

        public static ParticleBurst Create(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<ParticleBurst>();
            b.sortingOrder = sortingOrder;
            return b;
        }

        Particle Take()
        {
            foreach (var p in pool) if (!p.active) return p;
            var np = new Particle { sr = SpriteLibrary.CreateRenderer("p", transform, PlaceholderSpriteFactory.WhiteSquare(), sortingOrder) };
            pool.Add(np);
            return np;
        }

        /// <summary>Emits particles in a cone: direction in degrees (90 = up), spread in degrees (360 = all directions).</summary>
        public void Emit(Vector2 pos, Color color, int count, float speed, float life, float size, float gravity, float direction = 90f, float spread = 360f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Take();
                float a = (direction + Random.Range(-spread / 2f, spread / 2f)) * Mathf.Deg2Rad;
                float sp = speed * Random.Range(0.4f, 1.1f);
                p.pos = pos + Random.insideUnitCircle * size;
                p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
                p.maxLife = p.life = life * Random.Range(0.6f, 1.2f);
                p.size = size * Random.Range(0.6f, 1.4f);
                p.gravity = gravity;
                p.spin = Random.Range(-540f, 540f);
                p.angle = Random.Range(0f, 360f);
                p.color = color;
                p.active = true;
                p.sr.enabled = true;
                p.sr.sortingOrder = sortingOrder;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var p in pool)
            {
                if (!p.active) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.active = false;
                    p.sr.enabled = false;
                    continue;
                }
                p.vel.y -= p.gravity * dt;
                p.vel *= 1f - 1.5f * dt;
                p.pos += p.vel * dt;
                p.angle += p.spin * dt;
                float t = p.life / p.maxLife;
                float s = p.size * (0.4f + 0.6f * t);
                p.sr.transform.position = new Vector3(p.pos.x, p.pos.y, 0f);
                p.sr.transform.rotation = Quaternion.Euler(0f, 0f, p.angle);
                p.sr.transform.localScale = new Vector3(s, s, 1f);
                var c = p.color;
                c.a *= t;
                p.sr.color = c;
            }
        }

        public void Clear()
        {
            foreach (var p in pool)
            {
                p.active = false;
                p.sr.enabled = false;
            }
        }
    }
}
