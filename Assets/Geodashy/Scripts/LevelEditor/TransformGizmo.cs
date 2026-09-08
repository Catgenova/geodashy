using System.Collections.Generic;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Editing
{
    /// <summary>
    /// On-canvas handles around the selection: corners scale uniformly, edges scale one axis,
    /// the ring above rotates, the centre moves. Drawn with sprites sized in screen pixels so it reads at any zoom.
    /// </summary>
    public class TransformGizmo : MonoBehaviour
    {
        public enum Handle { None, Move, Rotate, ScaleTL, ScaleTR, ScaleBL, ScaleBR, ScaleL, ScaleR, ScaleT, ScaleB }

        public const int Sorting = 970;
        static readonly Color FrameColor = new Color(1f, 0.85f, 0.35f, 0.9f);
        static readonly Color HandleColor = new Color(1f, 0.92f, 0.6f, 1f);
        static readonly Color HoverColor = new Color(0.5f, 1f, 0.6f, 1f);
        static readonly Color RotateColor = new Color(0.55f, 0.85f, 1f, 1f);

        HitboxOverlay lines;
        readonly Dictionary<Handle, SpriteRenderer> handles = new Dictionary<Handle, SpriteRenderer>();
        SpriteRenderer readout;
        Rect bounds;
        Vector2 center;
        float handleSize;
        float rotateOffset;
        string lastReadout;

        public Handle Hovered { get; private set; }
        public Vector2 Center => center;

        public static TransformGizmo Create(Transform parent)
        {
            var go = new GameObject("Transform Gizmo");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<TransformGizmo>();
            g.lines = HitboxOverlay.Create(go.transform, "Lines", Sorting);
            foreach (Handle h in System.Enum.GetValues(typeof(Handle)))
            {
                if (h == Handle.None) continue;
                bool round = h == Handle.Move || h == Handle.Rotate;
                var sr = SpriteLibrary.CreateRenderer(h.ToString(), go.transform, round ? PlaceholderSpriteFactory.Circle() : PlaceholderSpriteFactory.WhiteSquare(), Sorting + 2);
                sr.enabled = false;
                g.handles[h] = sr;
            }
            g.readout = SpriteLibrary.CreateRenderer("Readout", go.transform, null, Sorting + 3);
            g.readout.enabled = false;
            return g;
        }

        public void Hide()
        {
            lines.Clear();
            foreach (var kv in handles) kv.Value.enabled = false;
            readout.enabled = false;
            Hovered = Handle.None;
        }

        /// <summary>World position of a handle for the current bounds.</summary>
        public Vector2 HandlePosition(Handle h)
        {
            switch (h)
            {
                case Handle.Move: return center;
                case Handle.Rotate: return new Vector2(center.x, bounds.yMax + rotateOffset);
                case Handle.ScaleTL: return new Vector2(bounds.xMin, bounds.yMax);
                case Handle.ScaleTR: return new Vector2(bounds.xMax, bounds.yMax);
                case Handle.ScaleBL: return new Vector2(bounds.xMin, bounds.yMin);
                case Handle.ScaleBR: return new Vector2(bounds.xMax, bounds.yMin);
                case Handle.ScaleL: return new Vector2(bounds.xMin, center.y);
                case Handle.ScaleR: return new Vector2(bounds.xMax, center.y);
                case Handle.ScaleT: return new Vector2(center.x, bounds.yMax);
                case Handle.ScaleB: return new Vector2(center.x, bounds.yMin);
            }
            return center;
        }

        public Handle HitTest(Vector2 world)
        {
            Handle best = Handle.None;
            float bestD = float.MaxValue;
            float r = handleSize * 0.9f;
            foreach (var kv in handles)
            {
                float d = (HandlePosition(kv.Key) - world).magnitude;
                if (d <= r && d < bestD)
                {
                    bestD = d;
                    best = kv.Key;
                }
            }
            return best;
        }

        /// <summary>Redraws around the selection. pixelsPerUnit keeps the handles a constant screen size.</summary>
        public void Refresh(Rect selectionBounds, float pixelsPerUnit, Vector2 cursor, bool dragging, Handle activeHandle, string info)
        {
            bounds = selectionBounds;
            center = bounds.center;
            handleSize = 14f / Mathf.Max(1f, pixelsPerUnit);
            rotateOffset = 44f / Mathf.Max(1f, pixelsPerUnit);
            Hovered = dragging ? activeHandle : HitTest(cursor);

            lines.thickness = 2f / Mathf.Max(1f, pixelsPerUnit);
            lines.Begin();
            var pad = handleSize * 0.4f;
            lines.Rect(new Rect(bounds.xMin - pad, bounds.yMin - pad, bounds.width + pad * 2f, bounds.height + pad * 2f), FrameColor);
            lines.Segment(new Vector2(center.x, bounds.yMax), HandlePosition(Handle.Rotate), RotateColor);
            lines.End();

            foreach (var kv in handles)
            {
                var sr = kv.Value;
                sr.enabled = true;
                var p = HandlePosition(kv.Key);
                sr.transform.position = new Vector3(p.x, p.y, 0f);
                bool edge = kv.Key == Handle.ScaleL || kv.Key == Handle.ScaleR || kv.Key == Handle.ScaleT || kv.Key == Handle.ScaleB;
                float s = edge ? handleSize * 0.7f : handleSize;
                if (kv.Key == Handle.Rotate) s = handleSize * 1.1f;
                sr.transform.localScale = new Vector3(s, s, 1f);
                var col = kv.Key == Handle.Rotate ? RotateColor : HandleColor;
                if (kv.Key == Hovered) col = HoverColor;
                sr.color = col;
            }

            if (!string.IsNullOrEmpty(info))
            {
                if (info != lastReadout)
                {
                    readout.sprite = PlaceholderSpriteFactory.ForText(info, Color.white);
                    lastReadout = info;
                }
                float scale = 18f / Mathf.Max(1f, pixelsPerUnit);
                readout.transform.localScale = new Vector3(scale, scale, 1f);
                readout.transform.position = new Vector3(center.x, bounds.yMin - pad - handleSize * 1.6f, 0f);
                readout.color = FrameColor;
                readout.enabled = true;
            }
            else readout.enabled = false;
        }
    }
}
