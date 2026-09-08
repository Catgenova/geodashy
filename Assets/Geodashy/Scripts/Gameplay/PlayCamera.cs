using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Gameplay
{
    /// <summary>Follows the rider with a horizontal lead, soft vertical follow, zoom/offset tweens, shake, static lock and mirroring.</summary>
    public class PlayCamera
    {
        public Camera cam;
        public PlayerController player;
        public LevelSettings settings;
        public float baseHalfHeight = 5.5f;

        float zoom = 1f, zoomFrom = 1f, zoomTo = 1f, zoomT = 1f, zoomDur;
        Easing zoomEase;
        Vector2 offset, offsetFrom, offsetTo;
        float offsetT = 1f, offsetDur;
        Easing offsetEase;
        float shakeStrength, shakeDuration, shakeInterval, shakeTimer, shakeTick;
        Vector2 shakeOffset;
        LevelObjectView staticTarget;
        bool staticFollowPlayerY;
        float staticBlend, staticBlendTarget, staticDur;
        Vector2 staticPos;
        bool mirrored;
        float currentY;

        public PlayCamera(Camera cam, PlayerController player, LevelSettings settings)
        {
            this.cam = cam;
            this.player = player;
            this.settings = settings;
            cam.orthographic = true;
            currentY = settings.groundY + baseHalfHeight - 1f;
        }

        public void Reset(float x)
        {
            zoom = zoomFrom = zoomTo = 1f;
            zoomT = 1f;
            offset = offsetFrom = offsetTo = Vector2.zero;
            offsetT = 1f;
            shakeDuration = 0f;
            staticTarget = null;
            staticBlend = staticBlendTarget = 0f;
            currentY = settings.groundY + baseHalfHeight - 1f;
            cam.orthographicSize = baseHalfHeight;
            cam.transform.position = new Vector3(x + HalfWidth * 0.3f, currentY, -10f);
            cam.transform.rotation = Quaternion.identity;
            SetMirror(false);
        }

        public float HalfWidth => cam.orthographicSize * cam.aspect;

        public struct Snapshot
        {
            public float zoom, currentY;
            public Vector2 offset;
        }

        public Snapshot Capture() => new Snapshot { zoom = zoom, currentY = currentY, offset = offset };

        public void Restore(Snapshot s)
        {
            zoom = zoomFrom = zoomTo = s.zoom;
            zoomT = 1f;
            offset = offsetFrom = offsetTo = s.offset;
            offsetT = 1f;
            currentY = s.currentY;
            shakeDuration = 0f;
            staticTarget = null;
            staticBlend = staticBlendTarget = 0f;
            cam.orthographicSize = baseHalfHeight / zoom;
            cam.transform.position = new Vector3(player.position.x + HalfWidth * 0.35f * player.direction + offset.x, currentY + offset.y, -10f);
        }

        public void SetZoom(float target, float duration, Easing ease)
        {
            zoomFrom = zoom;
            zoomTo = Mathf.Clamp(target, 0.25f, 4f);
            zoomDur = duration;
            zoomT = 0f;
            zoomEase = ease;
        }

        public void SetOffset(Vector2 target, float duration, Easing ease)
        {
            offsetFrom = offset;
            offsetTo = target;
            offsetDur = duration;
            offsetT = 0f;
            offsetEase = ease;
        }

        public void SetStatic(LevelObjectView target, float duration, bool followPlayerY)
        {
            staticTarget = target;
            staticFollowPlayerY = followPlayerY;
            staticBlendTarget = target != null ? 1f : 0f;
            staticDur = Mathf.Max(0.01f, duration);
        }

        public void Shake(float strength, float interval, float duration)
        {
            shakeStrength = strength;
            shakeInterval = Mathf.Max(0.005f, interval);
            shakeDuration = duration;
            shakeTimer = 0f;
            shakeTick = 0f;
        }

        public void SetMirror(bool on)
        {
            if (mirrored == on) return;
            mirrored = on;
            cam.ResetProjectionMatrix();
            if (on)
            {
                var m = cam.projectionMatrix;
                m.m00 = -m.m00;
                cam.projectionMatrix = m;
            }
        }

        public void Update(float dt)
        {
            if (zoomT < 1f)
            {
                zoomT = zoomDur <= 0.001f ? 1f : Mathf.Min(1f, zoomT + dt / zoomDur);
                zoom = Mathf.Lerp(zoomFrom, zoomTo, GeoMath.Ease(zoomEase, zoomT));
            }
            if (offsetT < 1f)
            {
                offsetT = offsetDur <= 0.001f ? 1f : Mathf.Min(1f, offsetT + dt / offsetDur);
                offset = Vector2.Lerp(offsetFrom, offsetTo, GeoMath.Ease(offsetEase, offsetT));
            }
            cam.orthographicSize = baseHalfHeight / zoom;
            float halfH = cam.orthographicSize;
            float halfW = HalfWidth;

            // horizontal: rider sits about a third of the way in
            float targetX = player.position.x + halfW * 0.35f * player.direction;

            // vertical: keep the ground visible, follow when the rider leaves the middle band
            float minY = settings.groundY + halfH - 1f;
            float targetY = currentY;
            float py = player.position.y;
            float band = halfH * 0.35f;
            if (py > currentY + band) targetY = py - band;
            else if (py < currentY - band) targetY = py + band;
            if (player.mount.flying)
            {
                float roomCenter = settings.groundY + settings.ceilingHeight / 2f;
                targetY = Mathf.Lerp(targetY, roomCenter, 0.5f);
            }
            targetY = Mathf.Max(minY, targetY);
            currentY = Mathf.Lerp(currentY, targetY, 1f - Mathf.Exp(-6f * dt));

            var desired = new Vector2(targetX, currentY) + offset;

            if (staticBlend != staticBlendTarget)
                staticBlend = Mathf.MoveTowards(staticBlend, staticBlendTarget, dt / staticDur);
            if (staticTarget != null)
            {
                staticPos = staticTarget.WorldPosition;
                if (staticFollowPlayerY) staticPos.y = desired.y;
            }
            if (staticBlend > 0f) desired = Vector2.Lerp(desired, staticPos, staticBlend);

            if (shakeDuration > 0f)
            {
                shakeTimer += dt;
                shakeTick += dt;
                if (shakeTick >= shakeInterval)
                {
                    shakeTick = 0f;
                    shakeOffset = Random.insideUnitCircle * shakeStrength;
                }
                if (shakeTimer >= shakeDuration)
                {
                    shakeDuration = 0f;
                    shakeOffset = Vector2.zero;
                }
            }

            cam.transform.position = new Vector3(desired.x + shakeOffset.x, desired.y + shakeOffset.y, -10f);
            SetMirror(player.mirror);
        }
    }
}
