using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Gameplay
{
    /// <summary>The rider + mount. One button: pressed / held. Custom kinematic physics in the Geometry Dash style.</summary>
    public class PlayerController : MonoBehaviour
    {
        public const float StepDt = 1f / 120f;

        public GameRunner runner;
        public LevelWorld world;
        public LevelSettings settings;

        public MountDefinition mount;
        public int speedTier = 1;
        public bool flipped;
        public bool mini;
        public bool mirror;
        public int direction = 1;
        public bool visible = true;

        public Vector2 position;
        public Vector2 velocity;
        public bool onGround;
        public bool dead;
        public bool finished;
        public float rotationDeg;
        public int jumps;

        // input
        bool held;
        bool pressedThisStep;
        float pressBuffer;
        float holdTime;
        bool dashing;
        bool dashFlip;

        readonly HashSet<int> usedInteractables = new HashSet<int>();
        readonly HashSet<int> touching = new HashSet<int>();
        readonly HashSet<int> touchingNow = new HashSet<int>();
        float accumulator;
        Vector2 prevPosition;
        SpriteRenderer sr;
        SpriteRenderer deathBurst;
        float deathTimer;
        float slopeSlope;
        bool onSlope;

        public float Speed => MountCatalog.Speed(speedTier);
        public float Up => flipped ? -1f : 1f;
        public Vector2 Size
        {
            get
            {
                float s = mini ? 0.6f : 1f;
                return new Vector2(mount.width * s, mount.height * s);
            }
        }

        public Rect Bounds
        {
            get
            {
                var s = Size;
                return new Rect(position.x - s.x / 2f, position.y - s.y / 2f, s.x, s.y);
            }
        }

        /// <summary>Smaller inner box used against hazards (forgiving like the original game).</summary>
        public Rect InnerBounds
        {
            get
            {
                var s = Size * 0.4f;
                return new Rect(position.x - s.x / 2f, position.y - s.y / 2f, s.x, s.y);
            }
        }

        public void Init(GameRunner r, LevelWorld w, LevelSettings s)
        {
            runner = r;
            world = w;
            settings = s;
            sr = gameObject.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(sr);
            sr.sortingOrder = 0;
            var burstGo = new GameObject("DeathBurst");
            burstGo.transform.SetParent(transform.parent, false);
            deathBurst = burstGo.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(deathBurst);
            deathBurst.sprite = PlaceholderSpriteFactory.Circle();
            deathBurst.sortingOrder = 5;
            deathBurst.enabled = false;
        }

        public void SetMount(string id)
        {
            mount = MountCatalog.Get(id);
            sr.sprite = SpriteLibrary.ForMount(mount);
            dashing = false;
            ApplyVisual();
        }

        public void Spawn(Vector2 pos, string mountId, int speed, bool gravityFlipped, bool isMini)
        {
            position = pos;
            prevPosition = pos;
            velocity = Vector2.zero;
            speedTier = speed;
            flipped = gravityFlipped;
            mini = isMini;
            direction = 1;
            dead = false;
            finished = false;
            onGround = false;
            rotationDeg = 0f;
            held = false;
            pressBuffer = 0f;
            holdTime = 0f;
            dashing = false;
            accumulator = 0f;
            usedInteractables.Clear();
            touching.Clear();
            deathBurst.enabled = false;
            SetMount(mountId);
            ApplyVisual();
        }

        /// <summary>Everything needed to put the rider back exactly where it was (practice checkpoints).</summary>
        public class Snapshot
        {
            public Vector2 position, velocity;
            public string mount;
            public int speedTier, direction, jumps;
            public bool flipped, mini, mirror, onGround, dashing;
            public float rotationDeg;
            public HashSet<int> used;
        }

        public Snapshot Capture()
        {
            return new Snapshot
            {
                position = position, velocity = velocity, mount = mount.id, speedTier = speedTier, direction = direction, jumps = jumps,
                flipped = flipped, mini = mini, mirror = mirror, onGround = onGround, dashing = dashing, rotationDeg = rotationDeg,
                used = new HashSet<int>(usedInteractables)
            };
        }

        public void Restore(Snapshot s)
        {
            dead = false;
            finished = false;
            deathBurst.enabled = false;
            position = s.position;
            prevPosition = s.position;
            velocity = s.velocity;
            speedTier = s.speedTier;
            direction = s.direction;
            jumps = s.jumps;
            flipped = s.flipped;
            mini = s.mini;
            mirror = s.mirror;
            onGround = s.onGround;
            dashing = false;
            rotationDeg = s.rotationDeg;
            held = false;
            pressBuffer = 0f;
            holdTime = 0f;
            accumulator = 0f;
            usedInteractables.Clear();
            usedInteractables.UnionWith(s.used);
            touching.Clear();
            SetMount(s.mount);
            ApplyVisual();
        }

        public void SetInput(bool isHeld, bool pressed)
        {
            held = isHeld;
            if (pressed) pressBuffer = 0.12f;
            if (!isHeld) holdTime = 0f;
        }

        public void Tick(float dt)
        {
            if (dead)
            {
                deathTimer += dt;
                float t = Mathf.Clamp01(deathTimer / 0.5f);
                deathBurst.transform.localScale = Vector3.one * (0.5f + t * 3f);
                deathBurst.color = new Color(1f, 0.6f, 0.2f, 1f - t);
                return;
            }
            if (finished) return;
            accumulator += Mathf.Min(dt, 0.1f);
            while (accumulator >= StepDt)
            {
                accumulator -= StepDt;
                Step(StepDt);
                if (dead || finished) break;
            }
            ApplyVisual();
        }

        // ---------------------------------------------------------------------

        void Step(float dt)
        {
            prevPosition = position;
            pressedThisStep = pressBuffer > 0f;
            if (held) holdTime += dt;

            // horizontal --------------------------------------------------------
            position.x += Speed * direction * dt;

            // vertical (mount rules) ------------------------------------------------
            MountPhysics(dt);
            position.y += velocity.y * dt;

            // world bounds ----------------------------------------------------------------
            var size = Size;
            float groundY = settings.groundY;
            float ceilY = groundY + settings.ceilingHeight;
            bool hasCeiling = mount.flying || flipped;
            bool wasGround = onGround;
            onGround = false;
            onSlope = false;
            if (position.y - size.y / 2f <= groundY)
            {
                position.y = groundY + size.y / 2f;
                if (velocity.y < 0f) velocity.y = 0f;
                if (!flipped) onGround = true;
            }
            if (hasCeiling && position.y + size.y / 2f >= ceilY)
            {
                position.y = ceilY - size.y / 2f;
                if (velocity.y > 0f) velocity.y = 0f;
                if (flipped) onGround = true;
            }

            // objects ----------------------------------------------------------------------
            var query = world.Query(GeoMath.Expand(Bounds, 1.5f));
            touchingNow.Clear();
            for (int i = 0; i < query.Count; i++)
            {
                var v = query[i];
                if (dead) break;
                switch (v.def.kind)
                {
                    case ObjectKind.Solid: CollideSolid(v); break;
                    case ObjectKind.Slope: CollideSlope(v); break;
                    case ObjectKind.Hazard: CollideHazard(v); break;
                    case ObjectKind.Orb: TouchOrb(v); break;
                    case ObjectKind.Pad: TouchPad(v); break;
                    case ObjectKind.Portal: TouchPortal(v); break;
                    case ObjectKind.Collectible: TouchCollectible(v); break;
                }
            }
            // forget interactables we are no longer touching so pads/portals can re-fire later
            var stale = new List<int>();
            foreach (var uid in touching) if (!touchingNow.Contains(uid)) stale.Add(uid);
            foreach (var uid in stale)
            {
                touching.Remove(uid);
                usedInteractables.Remove(uid);
            }
            foreach (var uid in touchingNow) touching.Add(uid);

            if (pressBuffer > 0f) pressBuffer -= dt;

            // triggers & finish ------------------------------------------------------------
            runner.triggers.PlayerAdvanced(this, prevPosition, position);
            if (position.x >= runner.finishX) finished = true;

            // rotation ----------------------------------------------------------------------
            UpdateRotation(dt, wasGround);
        }

        void MountPhysics(float dt)
        {
            float up = Up;
            float g = mount.gravity;
            bool press = pressedThisStep;

            if (dashing)
            {
                velocity.y = 0f;
                if (!held) dashing = false;
                return;
            }

            switch (mount.id)
            {
                case "horse":
                    if (onGround && (press || held))
                    {
                        velocity.y = mount.jumpVelocity * (mini ? 0.85f : 1f) * up;
                        onGround = false;
                        pressBuffer = 0f;
                        jumps++;
                    }
                    velocity.y -= g * up * dt;
                    break;
                case "cart":
                    if (onGround && press)
                    {
                        velocity.y = mount.jumpVelocity * up;
                        onGround = false;
                        holdTime = 0f;
                        pressBuffer = 0f;
                        jumps++;
                    }
                    if (!onGround && held && holdTime < 0.32f) velocity.y += mount.holdAccel * up * dt;
                    velocity.y -= g * up * dt;
                    break;
                case "dragon":
                    velocity.y += (held ? mount.holdAccel - g : -g) * up * dt;
                    velocity.y = Mathf.Clamp(velocity.y, -mount.maxFallSpeed, mount.maxRiseSpeed);
                    break;
                case "griffin":
                    if (press)
                    {
                        velocity.y = mount.jumpVelocity * (mini ? 0.9f : 1f) * up;
                        pressBuffer = 0f;
                        onGround = false;
                        jumps++;
                    }
                    velocity.y -= g * up * dt;
                    velocity.y = Mathf.Clamp(velocity.y, -mount.maxFallSpeed, mount.maxRiseSpeed);
                    break;
                case "boar":
                    if (onGround && press)
                    {
                        flipped = !flipped;
                        up = Up;
                        onGround = false;
                        velocity.y = 2f * up;
                        pressBuffer = 0f;
                        jumps++;
                    }
                    velocity.y -= g * up * dt;
                    break;
                case "wisp":
                {
                    float slope = mount.waveSlope * (mini ? 2f : 1f);
                    velocity.y = (held ? 1f : -1f) * up * Speed * slope;
                    break;
                }
                case "shadowcat":
                    if (press && onGround)
                    {
                        TeleportToOppositeSurface();
                        pressBuffer = 0f;
                        jumps++;
                        up = Up;
                    }
                    velocity.y -= g * up * dt;
                    break;
                default:
                    velocity.y -= g * up * dt;
                    break;
            }
            velocity.y = Mathf.Clamp(velocity.y, -mount.maxFallSpeed, mount.maxRiseSpeed);
        }

        /// <summary>Shadow cat / shadow rune: flip gravity and snap to the nearest surface in the new direction.</summary>
        public void TeleportToOppositeSurface()
        {
            flipped = !flipped;
            var size = Size;
            float groundY = settings.groundY;
            float ceilY = groundY + settings.ceilingHeight;
            float target = flipped ? ceilY - size.y / 2f : groundY + size.y / 2f;
            var searchRect = new Rect(position.x - size.x * 0.4f, Mathf.Min(groundY, position.y), size.x * 0.8f, Mathf.Max(ceilY, position.y + 40f) - Mathf.Min(groundY, position.y));
            foreach (var v in world.Query(searchRect))
            {
                if (v.def.kind != ObjectKind.Solid) continue;
                var b = v.Bounds;
                if (b.xMax < position.x - size.x * 0.4f || b.xMin > position.x + size.x * 0.4f) continue;
                if (flipped)
                {
                    // moving up: nearest block bottom above us
                    if (b.yMin >= position.y + size.y * 0.4f && b.yMin - size.y / 2f < target) target = b.yMin - size.y / 2f;
                }
                else
                {
                    if (b.yMax <= position.y - size.y * 0.4f && b.yMax + size.y / 2f > target) target = b.yMax + size.y / 2f;
                }
            }
            position.y = target;
            velocity.y = 0f;
            onGround = true;
        }

        void CollideSolid(LevelObjectView v)
        {
            var p = Bounds;
            var b = v.Bounds;
            if (!GeoMath.RectsOverlap(p, b)) return;
            float dxLeft = p.xMax - b.xMin, dxRight = b.xMax - p.xMin;
            float dyDown = p.yMax - b.yMin, dyUp = b.yMax - p.yMin; // dyUp: how far we'd move up to sit on top
            float dx = Mathf.Min(dxLeft, dxRight);
            float dy = Mathf.Min(dyDown, dyUp);
            var size = Size;
            float prevBottom = prevPosition.y - size.y / 2f, prevTop = prevPosition.y + size.y / 2f;
            bool cameFromAbove = prevBottom >= b.yMax - 0.08f;
            bool cameFromBelow = prevTop <= b.yMin + 0.08f;

            if (cameFromAbove || cameFromBelow || dy <= dx + 0.02f)
            {
                bool landOnTop = cameFromAbove || (!cameFromBelow && position.y > b.center.y);
                if (landOnTop)
                {
                    position.y = b.yMax + size.y / 2f;
                    if (!flipped)
                    {
                        if (velocity.y < 0f) velocity.y = 0f;
                        onGround = true;
                    }
                    else
                    {
                        // gravity points up: hitting a block's top is a head bump
                        if (mount.flying || mount.id == "wisp") velocity.y = Mathf.Max(0f, velocity.y);
                        else if (velocity.y < -0.01f) Die();
                    }
                }
                else
                {
                    position.y = b.yMin - size.y / 2f;
                    if (flipped)
                    {
                        if (velocity.y > 0f) velocity.y = 0f;
                        onGround = true;
                    }
                    else
                    {
                        if (mount.flying) velocity.y = Mathf.Min(0f, velocity.y);
                        else if (velocity.y > 0.01f) Die();
                        else velocity.y = 0f;
                    }
                }
            }
            else
            {
                Die();
            }
        }

        void CollideSlope(LevelObjectView v)
        {
            var b = v.Bounds;
            var p = Bounds;
            if (!GeoMath.RectsOverlap(p, b)) return;
            float rot = GeoMath.NormalizeAngle(v.WorldRotation);
            bool fx = v.data.flipX, fy = v.data.flipY;
            if (Mathf.Abs(Mathf.DeltaAngle(rot, 180f)) < 1f)
            {
                fx = !fx;
                fy = !fy;
            }
            else if (Mathf.Abs(Mathf.DeltaAngle(rot, 0f)) > 1f)
            {
                CollideSolid(v); // rotated 90/270: treat as a block
                return;
            }
            var size = Size;
            float t = Mathf.Clamp01((position.x - b.xMin) / Mathf.Max(0.001f, b.width));
            if (fx) t = 1f - t;
            float slope = b.height / Mathf.Max(0.001f, b.width) * (fx ? -1f : 1f);
            if (!fy)
            {
                // floor slope: surface rises with t
                float surface = b.yMin + t * b.height;
                if (position.x < b.xMin || position.x > b.xMax) return;
                if (position.y - size.y / 2f <= surface + 0.05f)
                {
                    if (flipped)
                    {
                        if (position.y - size.y / 2f < surface - 0.3f) Die();
                        return;
                    }
                    position.y = surface + size.y / 2f;
                    if (velocity.y < 0f) velocity.y = 0f;
                    onGround = true;
                    onSlope = true;
                    slopeSlope = slope;
                }
            }
            else
            {
                float surface = b.yMax - t * b.height;
                if (position.x < b.xMin || position.x > b.xMax) return;
                if (position.y + size.y / 2f >= surface - 0.05f)
                {
                    if (!flipped)
                    {
                        if (position.y + size.y / 2f > surface + 0.3f) Die();
                        return;
                    }
                    position.y = surface - size.y / 2f;
                    if (velocity.y > 0f) velocity.y = 0f;
                    onGround = true;
                    onSlope = true;
                    slopeSlope = -slope;
                }
            }
        }

        void CollideHazard(LevelObjectView v)
        {
            var inner = InnerBounds;
            if (!GeoMath.RectsOverlap(inner, v.Bounds)) return;
            var size = v.Size;
            Vector2 half;
            Vector2 localOffset = Vector2.zero;
            switch (v.def.collider)
            {
                case ColliderShape.Triangle:
                    half = new Vector2(size.x * 0.18f, size.y * 0.35f);
                    localOffset = new Vector2(0f, -size.y * 0.1f);
                    break;
                case ColliderShape.Circle:
                {
                    float r = Mathf.Min(size.x, size.y) * 0.5f * v.def.hitboxScale;
                    var c = v.WorldPosition;
                    float nx = Mathf.Clamp(c.x, inner.xMin, inner.xMax), ny = Mathf.Clamp(c.y, inner.yMin, inner.yMax);
                    if ((new Vector2(nx, ny) - c).sqrMagnitude <= r * r) Die();
                    return;
                }
                default:
                    half = size * 0.5f * v.def.hitboxScale;
                    break;
            }
            // test inner box corners + centre in the hazard's local space
            Vector2[] pts =
            {
                inner.center, new Vector2(inner.xMin, inner.yMin), new Vector2(inner.xMax, inner.yMin), new Vector2(inner.xMin, inner.yMax), new Vector2(inner.xMax, inner.yMax)
            };
            float sx = v.data.flipX ? -1f : 1f, sy = v.data.flipY ? -1f : 1f;
            foreach (var pt in pts)
            {
                var local = GeoMath.WorldToLocal(pt, v.WorldPosition, v.WorldRotation);
                local = new Vector2(local.x * sx, local.y * sy) - localOffset;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y)
                {
                    Die();
                    return;
                }
            }
            // also catch the case where the hazard box is fully inside the player's inner box
            var hz = GeoMath.LocalToWorld(localOffset, v.WorldPosition, v.WorldRotation);
            if (inner.Contains(hz)) Die();
        }

        bool CircleTouch(LevelObjectView v)
        {
            var b = Bounds;
            var c = v.WorldPosition;
            float r = Mathf.Max(v.Size.x, v.Size.y) * 0.5f * v.def.hitboxScale;
            float nx = Mathf.Clamp(c.x, b.xMin, b.xMax), ny = Mathf.Clamp(c.y, b.yMin, b.yMax);
            return (new Vector2(nx, ny) - c).sqrMagnitude <= r * r;
        }

        void TouchOrb(LevelObjectView v)
        {
            if (!CircleTouch(v)) return;
            touchingNow.Add(v.data.uid);
            if (usedInteractables.Contains(v.data.uid)) return;
            if (!(pressedThisStep || held)) return;
            if (held && !pressedThisStep && holdTime > 0.5f && !runner.orbHoldActivates) return;
            usedInteractables.Add(v.data.uid);
            pressBuffer = 0f;
            jumps++;
            float up = Up;
            switch (v.def.orbType)
            {
                case OrbType.Jump: velocity.y = 19.4f * up; break;
                case OrbType.SmallJump: velocity.y = 14f * up; break;
                case OrbType.BigJump: velocity.y = 26f * up; break;
                case OrbType.GravityFlip:
                    flipped = !flipped;
                    velocity.y = 11f * Up;
                    break;
                case OrbType.GravityJump:
                    flipped = !flipped;
                    velocity.y = 19.4f * Up;
                    break;
                case OrbType.Slam: velocity.y = -30f * up; break;
                case OrbType.Dash:
                    dashing = true;
                    velocity.y = 0f;
                    break;
                case OrbType.DashFlip:
                    flipped = !flipped;
                    dashing = true;
                    velocity.y = 0f;
                    break;
                case OrbType.Teleport: TeleportToOppositeSurface(); break;
                case OrbType.None:
                {
                    int g = v.data.GetInt("target", 0);
                    if (g > 0) runner.triggers.SpawnGroup(g, 0f);
                    break;
                }
            }
            onGround = false;
            runner.OnInteract(v);
        }

        void TouchPad(LevelObjectView v)
        {
            if (!GeoMath.RectsOverlap(Bounds, v.Bounds)) return;
            touchingNow.Add(v.data.uid);
            if (usedInteractables.Contains(v.data.uid)) return;
            usedInteractables.Add(v.data.uid);
            float up = Up;
            switch (v.def.padType)
            {
                case PadType.Jump: velocity.y = 20f * up; break;
                case PadType.SmallJump: velocity.y = 14f * up; break;
                case PadType.BigJump: velocity.y = 27f * up; break;
                case PadType.GravityFlip:
                    flipped = !flipped;
                    velocity.y = 12f * Up;
                    break;
                case PadType.Teleport: TeleportToOppositeSurface(); break;
            }
            onGround = false;
            runner.OnInteract(v);
        }

        void TouchPortal(LevelObjectView v)
        {
            var pb = v.Bounds;
            if (!GeoMath.RectsOverlap(Bounds, pb)) return;
            touchingNow.Add(v.data.uid);
            if (usedInteractables.Contains(v.data.uid)) return;
            usedInteractables.Add(v.data.uid);
            ApplyPortal(v.def, v.data);
            runner.OnInteract(v);
        }

        /// <summary>Applies a portal's effect. Also used to pre-scan portals when starting mid-level.</summary>
        public void ApplyPortal(ObjectDefinition def, LevelObject data)
        {
            switch (def.portalType)
            {
                case PortalType.Mount:
                    SetMount(def.portalMount);
                    if (!mount.flying) onGround = false;
                    break;
                case PortalType.GravityNormal: flipped = false; break;
                case PortalType.GravityFlip: flipped = true; break;
                case PortalType.Speed: speedTier = (int)def.portalSpeed; break;
                case PortalType.SizeNormal: mini = false; break;
                case PortalType.SizeMini: mini = true; break;
                case PortalType.MirrorOn: mirror = true; break;
                case PortalType.MirrorOff: mirror = false; break;
                case PortalType.DualOn:
                case PortalType.DualOff:
                    break; // dual riders are not simulated yet
                case PortalType.Teleport:
                    position.y += data.GetFloat("exitY", 3f);
                    break;
            }
            runner.OnPlayerStateChanged();
        }

        void TouchCollectible(LevelObjectView v)
        {
            if (!CircleTouch(v)) return;
            if (!v.runtimeActive) return;
            v.runtimeActive = false;
            world.MarkDirty(v);
            runner.OnCollect(v);
        }

        void UpdateRotation(float dt, bool wasGround)
        {
            float up = Up;
            switch (mount.rotationMode)
            {
                case 1: // spin while airborne, settle on landing
                    if (onGround) rotationDeg = Mathf.MoveTowardsAngle(rotationDeg, Mathf.Round(rotationDeg / 90f) * 90f, 720f * dt);
                    else rotationDeg -= 420f * dt * up * direction;
                    break;
                case 2: // tilt with velocity
                {
                    float target = Mathf.Atan2(velocity.y, Speed) * Mathf.Rad2Deg * (mount.id == "wisp" ? 1f : 0.6f) * direction;
                    rotationDeg = Mathf.LerpAngle(rotationDeg, target, 1f - Mathf.Exp(-14f * dt));
                    break;
                }
                case 3: // roll
                    rotationDeg -= Speed * 60f * dt * direction * (flipped ? -1f : 1f);
                    break;
                default:
                    rotationDeg = Mathf.LerpAngle(rotationDeg, 0f, 1f - Mathf.Exp(-10f * dt));
                    break;
            }
            // slope launch: leaving an up-slope keeps the momentum
            if (wasGround && !onGround && onSlope == false && slopeSlope > 0f && !mount.flying && velocity.y <= 0.01f)
            {
                velocity.y = slopeSlope * Speed * 0.55f * up;
            }
            if (!onSlope) slopeSlope = 0f;
        }

        public void Die()
        {
            if (dead) return;
            dead = true;
            deathTimer = 0f;
            deathBurst.enabled = true;
            deathBurst.transform.position = new Vector3(position.x, position.y, 0f);
            deathBurst.transform.localScale = Vector3.one * 0.5f;
            sr.enabled = false;
            runner.OnPlayerDied();
        }

        void ApplyVisual()
        {
            if (sr == null) return;
            transform.position = new Vector3(position.x, position.y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, rotationDeg);
            float s = mini ? 0.6f : 1f;
            float baseScale = 1f;
            if (sr.sprite != null) baseScale = mount.width / Mathf.Max(0.01f, sr.sprite.bounds.size.x);
            transform.localScale = new Vector3(baseScale * s * direction, baseScale * s * (flipped ? -1f : 1f), 1f);
            sr.enabled = visible && !dead;
        }
    }
}
