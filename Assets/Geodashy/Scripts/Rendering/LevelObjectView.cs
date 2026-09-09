using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Scene representation of one LevelObject, used by both the editor and play mode.</summary>
    public class LevelObjectView : MonoBehaviour
    {
        public const int SortingBase = 0;
        public const int OutlineSorting = 900;

        public LevelObject data;
        public ObjectDefinition def;
        public SpriteRenderer renderer2D;
        public LevelData level;
        public bool playMode;

        SpriteRenderer outline;
        string lastText;
        Color lastTextColor;

        // runtime (play mode) state driven by triggers
        [System.NonSerialized] public Vector2 runtimeOffset;
        [System.NonSerialized] public float runtimeRotation;
        [System.NonSerialized] public float runtimeAlpha = 1f;
        [System.NonSerialized] public bool runtimeActive = true;
        /// <summary>Set by LevelWorld.UpdateCulling when the object is far outside the play camera.</summary>
        [System.NonSerialized] public bool culled;
        /// <summary>Crowd reactions in play: extra rotation (degrees) and scale (fraction) on top of the authored transform.</summary>
        [System.NonSerialized] public float visualWobble;
        [System.NonSerialized] public float visualPulse;
        [System.NonSerialized] public float spinAngle;
        [System.NonSerialized] public Color pulseColor = Color.clear;

        public static LevelObjectView Create(Transform parent, LevelObject o, LevelData level, bool playMode)
        {
            var go = new GameObject(o.type + "#" + o.uid);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<LevelObjectView>();
            v.renderer2D = go.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(v.renderer2D);
            v.Bind(o, level, playMode);
            return v;
        }

        public void Bind(LevelObject o, LevelData lvl, bool play)
        {
            data = o;
            level = lvl;
            playMode = play;
            def = ObjectCatalog.Get(o.type);
            Refresh();
        }

        /// <summary>World position including any runtime movement.</summary>
        public Vector2 WorldPosition => new Vector2(data.x, data.y) + runtimeOffset;
        public float WorldRotation => data.rotation + runtimeRotation + spinAngle;

        /// <summary>Re-applies sprite, transform, colour and sorting from data.</summary>
        public void Refresh()
        {
            if (def == null) return;
            if (def.kind == ObjectKind.Text)
            {
                string text = data.GetString("text", "TEXT");
                var col = level != null ? level.ResolveColor(data.baseColor, def.primaryColor) : def.primaryColor;
                if (text != lastText || col != lastTextColor || renderer2D.sprite == null)
                {
                    renderer2D.sprite = PlaceholderSpriteFactory.ForText(text, Color.white);
                    lastText = text;
                    lastTextColor = col;
                }
            }
            else if (renderer2D.sprite == null)
            {
                renderer2D.sprite = SpriteLibrary.ForObject(def);
            }
            ApplyTransform();
            ApplyColor();
            renderer2D.sortingOrder = SortingBase + data.zLayer * 100 + data.zOrder;
            bool visible = runtimeActive;
            if (playMode && (def.HiddenInPlay || def.id == "invisible_block")) visible = false;
            renderer2D.enabled = visible;
        }

        public void ApplyTransform()
        {
            var pos = WorldPosition;
            transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            transform.localRotation = Quaternion.Euler(0, 0, WorldRotation + visualWobble);
            float textScale = (def.kind == ObjectKind.Text ? data.GetFloat("size", 1f) : 1f) * (1f + visualPulse);
            transform.localScale = new Vector3(data.scaleX * textScale * (data.flipX ? -1f : 1f), data.scaleY * textScale * (data.flipY ? -1f : 1f), 1f);
        }

        public void ApplyColor()
        {
            Color c = level != null ? level.ResolveColor(data.baseColor, Color.white) : Color.white;
            if (pulseColor.a > 0f) c = Color.Lerp(c, pulseColor, pulseColor.a);
            c.a *= runtimeAlpha;
            renderer2D.color = c;
        }

        /// <summary>Size in world units used for hit testing and outlines.</summary>
        public Vector2 Size
        {
            get
            {
                if (def.kind == ObjectKind.Text)
                {
                    var s = PlaceholderSpriteFactory.TextSize(data.GetString("text", "TEXT")) * data.GetFloat("size", 1f);
                    return new Vector2(s.x * Mathf.Abs(data.scaleX), s.y * Mathf.Abs(data.scaleY));
                }
                return GeoMath.ObjectSize(data, def);
            }
        }

        public bool ContainsPoint(Vector2 world, float padding = 0f)
        {
            var size = Size;
            var local = GeoMath.WorldToLocal(world, WorldPosition, WorldRotation);
            return Mathf.Abs(local.x) <= size.x / 2f + padding && Mathf.Abs(local.y) <= size.y / 2f + padding;
        }

        public Rect Bounds
        {
            get
            {
                var size = Size;
                float rad = WorldRotation * Mathf.Deg2Rad;
                float c = Mathf.Abs(Mathf.Cos(rad)), s = Mathf.Abs(Mathf.Sin(rad));
                float w = size.x * c + size.y * s;
                float h = size.x * s + size.y * c;
                var p = WorldPosition;
                return new Rect(p.x - w / 2f, p.y - h / 2f, w, h);
            }
        }

        public void SetOutline(Color color, bool visible)
        {
            if (!visible)
            {
                if (outline != null) outline.enabled = false;
                return;
            }
            if (outline == null)
            {
                var go = new GameObject("outline");
                go.transform.SetParent(transform, false);
                outline = go.AddComponent<SpriteRenderer>();
                outline.sprite = PlaceholderSpriteFactory.Outline();
                outline.drawMode = SpriteDrawMode.Sliced;
                outline.sortingOrder = OutlineSorting;
                SpriteLibrary.ApplyMaterial(outline);
            }
            // outline is a child, so counter the parent's scale to keep the border thickness constant
            var size = Size;
            float sx = Mathf.Abs(transform.localScale.x), sy = Mathf.Abs(transform.localScale.y);
            outline.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, sx), 1f / Mathf.Max(0.001f, sy), 1f);
            outline.size = new Vector2(size.x + 0.12f, size.y + 0.12f);
            outline.color = color;
            outline.enabled = true;
        }

        public void SetDimmed(bool dimmed)
        {
            ApplyColor();
            if (dimmed)
            {
                var c = renderer2D.color;
                c.a *= 0.25f;
                renderer2D.color = c;
            }
        }
    }
}
