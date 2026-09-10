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

        public static readonly Color Ink = new Color(0.28f, 0.17f, 0.08f, 1f);
        public static readonly Color Parchment = new Color(0.93f, 0.86f, 0.68f, 0.98f);

        /// <summary>Smallest font size any widget will use. Dense panels asked for 10-11px which reads badly at 1080p.</summary>
        public static int MinFontSize = 13;

        /// <summary>Parchment-and-iron skin (default) or the flat classic panels; Options toggles it.</summary>
        public const string SkinPref = "geodashy.uiSkin";
        public static bool Themed
        {
            get => PlayerPrefs.GetInt(SkinPref, 1) == 1;
            set => PlayerPrefs.SetInt(SkinPref, value ? 1 : 0);
        }

        public enum SkinKind { Iron, Parchment, Plate, Inset, Ribbon, BannerBar }

        /// <summary>Gives an image the skin sprite for its role; a no-op flat fill when the classic skin is on.</summary>
        public static void Skin(Image img, SkinKind kind, Color? tint = null)
        {
            if (img == null) return;
            if (!Themed)
            {
                if (tint.HasValue) img.color = tint.Value;
                return;
            }
            switch (kind)
            {
                case SkinKind.Iron: img.sprite = Geodashy.Rendering.UISkin.Iron(); break;
                case SkinKind.Parchment: img.sprite = Geodashy.Rendering.UISkin.Parchment(); break;
                case SkinKind.Plate: img.sprite = Geodashy.Rendering.UISkin.Plate(); break;
                case SkinKind.Inset: img.sprite = Geodashy.Rendering.UISkin.Inset(); break;
                case SkinKind.Ribbon: img.sprite = Geodashy.Rendering.UISkin.Ribbon(); break;
                case SkinKind.BannerBar: img.sprite = Geodashy.Rendering.UISkin.BannerBar(); break;
            }
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.5f;
            img.color = tint ?? (kind == SkinKind.Iron || kind == SkinKind.Parchment || kind == SkinKind.Ribbon || kind == SkinKind.BannerBar ? Color.white : img.color);
        }

        /// <summary>A riveted iron frame (dock, top bar, window). Keeps the light text palette.</summary>
        public static RectTransform Frame(Transform parent, string name, Color? fallback = null)
        {
            var rt = Panel(parent, name, fallback ?? PanelBg);
            Skin(rt.GetComponent<Image>(), SkinKind.Iron, Themed ? Color.white : (Color?)null);
            return rt;
        }

        /// <summary>A parchment card; pair it with Ink text.</summary>
        public static RectTransform Card(Transform parent, string name)
        {
            var rt = Panel(parent, name, Parchment);
            Skin(rt.GetComponent<Image>(), SkinKind.Parchment, Themed ? Color.white : (Color?)null);
            return rt;
        }

        /// <summary>Attaches a hover tooltip to any widget.</summary>
        public static T Tip<T>(T widget, string text) where T : Component
        {
            if (widget == null || string.IsNullOrEmpty(text)) return widget;
            var t = widget.gameObject.GetComponent<Tooltip>();
            if (t == null) t = widget.gameObject.AddComponent<Tooltip>();
            t.text = text;
            return widget;
        }

        static Font font;

        public static int FontSize(int requested) => Mathf.Max(MinFontSize, requested);

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
            t.fontSize = FontSize(size);
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
            if (!Themed)
            {
                return Label(parent, text.ToUpperInvariant(), 12, TextAnchor.MiddleLeft, AccentDim, -1, 18, true);
            }
            // a swallow-tailed ribbon with the title in gold, the strip fading out to the right
            var rt = Rect(parent, "Section " + text);
            Layout(rt.gameObject, -1, 20, 1);
            var ribbon = Rect(rt, "Ribbon");
            var img = ribbon.gameObject.AddComponent<Image>();
            Skin(img, SkinKind.Ribbon);
            img.raycastTarget = false;
            Anchor(ribbon, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(Mathf.Clamp(text.Length * 8f + 30f, 90f, 240f), 0));
            var t = Label(ribbon, text.ToUpperInvariant(), 11, TextAnchor.MiddleCenter, Accent, -1, -1, true);
            Stretch(t.rectTransform, 12, 0, 12, 0);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var line = Rect(rt, "Line");
            var li = line.gameObject.AddComponent<Image>();
            li.color = new Color(AccentDim.r, AccentDim.g, AccentDim.b, 0.35f);
            li.raycastTarget = false;
            Anchor(line, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(Mathf.Clamp(text.Length * 8f + 34f, 94f, 244f), -0.5f), new Vector2(0, 0.5f));
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, float width = -1f, float height = 32f, Color? bg = null, int fontSize = 14)
        {
            var rt = Rect(parent, "Button " + label);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg ?? ButtonBg;
            Skin(img, SkinKind.Plate);
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
            b.onClick.AddListener(() => Geodashy.Core.Sfx.Play("click", 0.5f, 1f, 0.1f));
            var t = Label(rt, label, fontSize, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 4, 2, 4, 2);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Layout(rt.gameObject, width, height, width < 0 ? 1 : -1);
            if (Themed) HoverGlow.Attach(rt);
            return b;
        }

        /// <summary>Button with a 16 px glyph on the left; pass a null label for an icon-only button. Tooltip optional.</summary>
        public static Button IconButton(Transform parent, string icon, string label, UnityAction onClick, float width = -1f, float height = 32f, Color? bg = null, int fontSize = 14, string tip = null)
        {
            var b = Button(parent, label ?? "", onClick, width, height, bg, fontSize);
            var rt = b.GetComponent<RectTransform>();
            var t = b.GetComponentInChildren<Text>();
            float iconSize = Mathf.Clamp(height * 0.55f, 14f, 22f);
            var iconRt = Rect(rt, "Glyph");
            var img = iconRt.gameObject.AddComponent<Image>();
            img.sprite = Geodashy.Rendering.EditorIcons.Get(icon);
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = TextColor;
            if (string.IsNullOrEmpty(label))
            {
                Anchor(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-iconSize / 2f, -iconSize / 2f), new Vector2(iconSize / 2f, iconSize / 2f));
                t.gameObject.SetActive(false);
            }
            else
            {
                Anchor(iconRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, -iconSize / 2f), new Vector2(6 + iconSize, iconSize / 2f));
                Stretch(t.rectTransform, 8 + iconSize, 2, 4, 2);
                t.alignment = TextAnchor.MiddleLeft;
            }
            if (!string.IsNullOrEmpty(tip)) Tip(b, tip);
            else if (string.IsNullOrEmpty(label)) Tip(b, icon);
            return b;
        }

        /// <summary>Swaps the glyph of an IconButton.</summary>
        public static void SetButtonIcon(Button b, string icon)
        {
            var g = b.transform.Find("Glyph");
            if (g == null) return;
            var img = g.GetComponent<Image>();
            if (img != null) img.sprite = Geodashy.Rendering.EditorIcons.Get(icon);
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
            if (Themed)
            {
                // a wax seal pressed into a dark socket: the seal is the toggle's check graphic
                bg.sprite = Geodashy.Rendering.UISkin.Inset();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
                ck.sprite = Geodashy.Rendering.UISkin.Seal();
                ck.color = Color.white;
                ck.preserveAspect = true;
                Stretch(ckRt, 1, 1, 1, 1);
            }
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
            if (Themed)
            {
                Skin(bg, SkinKind.Inset, Color.white);
                handle.sprite = Geodashy.Rendering.UISkin.Knob();
                handle.color = Color.white;
                handle.preserveAspect = true;
                Anchor(handleRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(-10, -10), new Vector2(10, 10));
            }
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
            Skin(img, SkinKind.Inset, Themed ? Color.white : (Color?)null);
            var f = rt.gameObject.AddComponent<InputField>();
            f.targetGraphic = img;
            var textRt = Rect(rt, "Text");
            var t = textRt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = FontSize(fontSize);
            t.color = TextColor;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(textRt, 6, 2, 6, 2);
            var phRt = Rect(rt, "Placeholder");
            var ph = phRt.gameObject.AddComponent<Text>();
            ph.font = Font;
            ph.fontSize = FontSize(fontSize);
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
            Skin(img, SkinKind.Plate);
            var b = rt.gameObject.AddComponent<Button>();
            if (Themed) HoverGlow.Attach(rt);
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
