using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Programmatic uGUI builders with a consistent medieval-dark theme. No prefabs required.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelBg = new Color(0.11f, 0.09f, 0.14f, 0.96f);
        public static readonly Color PanelBg2 = new Color(0.17f, 0.14f, 0.21f, 0.98f);
        public static readonly Color PanelBg3 = new Color(0.23f, 0.19f, 0.28f, 1f);
        public static readonly Color Accent = new Color(0.88f, 0.71f, 0.29f, 1f);
        public static readonly Color AccentDim = new Color(0.55f, 0.45f, 0.2f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.91f, 0.84f, 1f);
        public static readonly Color TextDim = new Color(0.65f, 0.62f, 0.6f, 1f);
        public static readonly Color ButtonBg = new Color(0.27f, 0.22f, 0.33f, 1f);
        public static readonly Color ButtonActive = new Color(0.62f, 0.48f, 0.18f, 1f);
        public static readonly Color Danger = new Color(0.65f, 0.2f, 0.2f, 1f);
        public static readonly Color Good = new Color(0.2f, 0.55f, 0.3f, 1f);
        public static readonly Color InputBg = new Color(0.06f, 0.05f, 0.08f, 1f);

        static Font font;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                try
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch (Exception)
                {
                    font = null;
                }
                if (font == null)
                {
                    try
                    {
                        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                    catch (Exception)
                    {
                        font = null;
                    }
                }
                if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "DejaVu Sans", "Liberation Sans" }, 16);
                return font;
            }
        }

        // ---- rect helpers ----------------------------------------------------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void SetSize(RectTransform rt, float w, float h)
        {
            rt.sizeDelta = new Vector2(w, h);
        }

        public static RectTransform Panel(Transform parent, string name, Color bg)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;
            Stretch(rt);
            return rt;
        }

        public static Image Spacer(Transform parent, float height, float width = -1)
        {
            var rt = Rect(parent, "Spacer");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            Layout(rt.gameObject, width, height);
            return img;
        }

        public static Image Divider(Transform parent, float height = 1f)
        {
            var rt = Rect(parent, "Divider");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            img.raycastTarget = false;
            Layout(rt.gameObject, -1, height);
            return img;
        }

        // ---- layout -----------------------------------------------------------

        public static LayoutElement Layout(GameObject go, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, float minW = -1, float minH = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            le.minWidth = minW;
            le.minHeight = minH;
            return le;
        }

        public static VerticalLayoutGroup VLayout(RectTransform rt, float spacing = 6f, int pad = 8, bool expandWidth = true, bool controlHeight = true, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childForceExpandWidth = expandWidth;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childControlHeight = controlHeight;
            v.childAlignment = align;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(RectTransform rt, float spacing = 6f, int pad = 0, bool expandWidth = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childForceExpandWidth = expandWidth;
            h.childForceExpandHeight = false;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childAlignment = align;
            return h;
        }

        public static GridLayoutGroup Grid(RectTransform rt, Vector2 cell, Vector2 spacing, int pad = 8)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = TextAnchor.UpperLeft;
            g.startCorner = GridLayoutGroup.Corner.UpperLeft;
            g.startAxis = GridLayoutGroup.Axis.Horizontal;
            return g;
        }

        public static ContentSizeFitter Fitter(RectTransform rt, bool vertical = true, bool horizontal = false)
        {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        /// <summary>A horizontal row container for a vertical layout.</summary>
        public static RectTransform Row(Transform parent, float height = 32f, float spacing = 6f, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = Rect(parent, "Row");
            HLayout(rt, spacing, 0, false, align);
            Layout(rt.gameObject, -1, height, 1);
            return rt;
        }

        /// <summary>A vertical column container for a horizontal layout.</summary>
        public static RectTransform Column(Transform parent, float width = -1f, float spacing = 6f, int pad = 0)
        {
            var rt = Rect(parent, "Column");
            VLayout(rt, spacing, pad);
            Layout(rt.gameObject, width, -1, width < 0 ? 1 : -1, 1);
            return rt;
        }

        // ---- widgets ---------------------------------------------------------

        public static Text Label(Transform parent, string text, int size = 15, TextAnchor align = TextAnchor.MiddleLeft, Color? color = null, float width = -1f, float height = -1f, bool bold = false)
        {
            var rt = Rect(parent, "Label");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.text = text;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            if (width >= 0 || height >= 0) Layout(rt.gameObject, width, height, width < 0 ? 1 : -1);
            return t;
        }

        public static Text SectionHeader(Transform parent, string text)
        {
            var t = Label(parent, text.ToUpperInvariant(), 12, TextAnchor.MiddleLeft, AccentDim, -1, 18, true);
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, float width = -1f, float height = 32f, Color? bg = null, int fontSize = 14)
        {
            var rt = Rect(parent, "Button " + label);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg ?? ButtonBg;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Label(rt, label, fontSize, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 4, 2, 4, 2);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Layout(rt.gameObject, width, height, width < 0 ? 1 : -1);
            return b;
        }

        public static void SetButtonActive(Button b, bool active)
        {
            var img = b.targetGraphic as Image;
            if (img != null) img.color = active ? ButtonActive : ButtonBg;
        }

        public static void SetButtonLabel(Button b, string label)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = label;
        }

        public static Toggle Toggle(Transform parent, string label, bool on, UnityAction<bool> onChange, float height = 26f, float width = -1f)
        {
            var rt = Rect(parent, "Toggle " + label);
            var tg = rt.gameObject.AddComponent<Toggle>();
            var bgRt = Rect(rt, "Background");
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.color = InputBg;
            Anchor(bgRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -9), new Vector2(18, 9));
            var ckRt = Rect(bgRt, "Checkmark");
            var ck = ckRt.gameObject.AddComponent<Image>();
            ck.color = Accent;
            Stretch(ckRt, 4, 4, 4, 4);
            tg.targetGraphic = bg;
            tg.graphic = ck;
            tg.isOn = on;
            if (onChange != null) tg.onValueChanged.AddListener(onChange);
            var t = Label(rt, label, 14);
            Stretch(t.rectTransform, 24, 0, 0, 0);
            Layout(rt.gameObject, width, height, width < 0 ? 1 : -1);
            return tg;
        }

        public static Slider Slider(Transform parent, float min, float max, float value, UnityAction<float> onChange, bool whole = false, float height = 22f, float width = -1f)
        {
            var rt = Rect(parent, "Slider");
            var s = rt.gameObject.AddComponent<Slider>();
            var bgRt = Rect(rt, "Background");
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.color = InputBg;
            Anchor(bgRt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -4), new Vector2(0, 4));
            var fillArea = Rect(rt, "Fill Area");
            Anchor(fillArea, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(6, -4), new Vector2(-6, 4));
            var fillRt = Rect(fillArea, "Fill");
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = AccentDim;
            Stretch(fillRt);
            var handleArea = Rect(rt, "Handle Slide Area");
            Anchor(handleArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 0), new Vector2(-8, 0));
            var handleRt = Rect(handleArea, "Handle");
            var handle = handleRt.gameObject.AddComponent<Image>();
            handle.color = Accent;
            Anchor(handleRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-8, 0), new Vector2(8, 0));
            s.fillRect = fillRt;
            s.handleRect = handleRt;
            s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = min;
            s.maxValue = max;
            s.wholeNumbers = whole;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            Layout(rt.gameObject, width, height, width < 0 ? 1 : -1);
            return s;
        }

        public static InputField Input(Transform parent, string placeholder, string value, UnityAction<string> onEndEdit, float width = -1f, float height = 28f,
            InputField.ContentType contentType = InputField.ContentType.Standard, int fontSize = 14)
        {
            var rt = Rect(parent, "Input");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = InputBg;
            var f = rt.gameObject.AddComponent<InputField>();
            f.targetGraphic = img;
            var textRt = Rect(rt, "Text");
            var t = textRt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.color = TextColor;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(textRt, 6, 2, 6, 2);
            var phRt = Rect(rt, "Placeholder");
            var ph = phRt.gameObject.AddComponent<Text>();
            ph.font = Font;
            ph.fontSize = fontSize;
            ph.color = TextDim;
            ph.fontStyle = FontStyle.Italic;
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = placeholder;
            Stretch(phRt, 6, 2, 6, 2);
            f.textComponent = t;
            f.placeholder = ph;
            f.contentType = contentType;
            f.text = value ?? "";
            f.caretColor = Accent;
            f.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.4f);
            if (onEndEdit != null) f.onEndEdit.AddListener(onEndEdit);
            Layout(rt.gameObject, width, height, width < 0 ? 1 : -1);
            return f;
        }

        public static ScrollRect ScrollView(Transform parent, string name, out RectTransform content, bool vertical = true, bool horizontal = false, Color? bg = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg ?? new Color(0, 0, 0, 0.25f);
            var sr = rt.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect(rt, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = Color.clear;
            Stretch(viewport, 2, 2, 2, 2);
            content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            if (horizontal && !vertical)
            {
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 0.5f);
            }
            sr.content = content;
            sr.viewport = viewport;
            sr.vertical = vertical;
            sr.horizontal = horizontal;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40f;
            sr.inertia = true;
            Layout(rt.gameObject, -1, -1, 1, 1);
            return sr;
        }

        public static Image Icon(Transform parent, Sprite sprite, float size, Color? tint = null)
        {
            var rt = Rect(parent, "Icon");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            Layout(rt.gameObject, size, size);
            return img;
        }

        public static Image Swatch(Transform parent, Color c, float size = 24f)
        {
            var rt = Rect(parent, "Swatch");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            Layout(rt.gameObject, size, size);
            return img;
        }

        /// <summary>A tile button showing a sprite with a caption, used by the palette.</summary>
        public static Button TileButton(Transform parent, Sprite sprite, string caption, UnityAction onClick, float size = 76f)
        {
            var rt = Rect(parent, "Tile " + caption);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = ButtonBg;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = colors;
            b.onClick.AddListener(onClick);
            var iconRt = Rect(rt, "Icon");
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Anchor(iconRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(6, 18), new Vector2(-6, -4));
            var t = Label(rt, caption, 10, TextAnchor.MiddleCenter, TextColor);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Anchor(t.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 1), new Vector2(-2, 18));
            SetSize(rt, size, size);
            return b;
        }

        public static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }
    }
}
