using UnityEngine;

namespace Geodashy.Editing
{
    /// <summary>Pan/zoom controller for the editor camera.</summary>
    public class EditorCamera : MonoBehaviour
    {
        public const float BaseHalfHeight = 5.5f;
        public const float MinZoom = 0.2f;
        public const float MaxZoom = 4f;

        public Camera cam;
        public float zoom = 1f;
        public float minX = -8f;
        public float minY = -8f;
        public float panSpeed = 14f;

        Vector3 panStartWorld;
        bool panning;

        public void Init(Camera c)
        {
            cam = c;
            cam.orthographic = true;
            ApplyZoom();
        }

        public Vector2 Position
        {
            get => cam.transform.position;
            set
            {
                var p = cam.transform.position;
                p.x = Mathf.Max(minX, value.x);
                p.y = Mathf.Max(minY, value.y);
                cam.transform.position = p;
            }
        }

        public float HalfHeight => cam.orthographicSize;
        public float HalfWidth => cam.orthographicSize * cam.aspect;

        public Rect ViewRect
        {
            get
            {
                var p = Position;
                return new Rect(p.x - HalfWidth, p.y - HalfHeight, HalfWidth * 2f, HalfHeight * 2f);
            }
        }

        public void SetZoom(float z)
        {
            zoom = Mathf.Clamp(z, MinZoom, MaxZoom);
            ApplyZoom();
        }

        /// <summary>Zooms while keeping the world point under the cursor fixed.</summary>
        public void ZoomAt(float newZoom, Vector2 screenPoint)
        {
            var before = ScreenToWorld(screenPoint);
            SetZoom(newZoom);
            var after = ScreenToWorld(screenPoint);
            Position = Position + (before - after);
        }

        void ApplyZoom()
        {
            cam.orthographicSize = BaseHalfHeight / zoom;
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            return new Vector2(w.x, w.y);
        }

        public void Pan(Vector2 worldDelta)
        {
            Position = Position + worldDelta;
        }

        public void PanKeys(Vector2 dir, float dt)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            Pan(dir.normalized * panSpeed * dt / zoom);
        }

        public void BeginDragPan(Vector2 screen)
        {
            panning = true;
            panStartWorld = ScreenToWorld(screen);
        }

        public void DragPan(Vector2 screen)
        {
            if (!panning) return;
            var now = ScreenToWorld(screen);
            Pan((Vector2)panStartWorld - now);
        }

        public void EndDragPan()
        {
            panning = false;
        }

        public bool IsPanning => panning;
    }
}
