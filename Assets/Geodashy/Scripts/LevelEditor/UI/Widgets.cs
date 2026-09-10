using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Labelled numeric input with -/+ buttons.</summary>
    public class NumberField
    {
        public RectTransform root;
        public InputField input;
        public float step = 1f;
        public float min = float.MinValue;
        public float max = float.MaxValue;
        public bool integer;
        public Action<float> onChange;
        float value;
        bool suppress;

        public float Value => value;

        public static NumberField Create(Transform parent, string label, float value, float step, float min, float max, Action<float> onChange, bool integer = false, float labelWidth = 110f, float height = 28f)
        {
            var f = new NumberField { step = step, min = min, max = max, integer = integer, onChange = onChange, value = value };
            f.root = UIFactory.Row(parent, height, 4);
            if (!string.IsNullOrEmpty(label))
            {
                // the label is a scrub handle: drag sideways to change the value, Shift for fine steps
                var l = UIFactory.Label(f.root, label, 13, TextAnchor.MiddleLeft, UIFactory.TextDim, labelWidth, height);
                l.raycastTarget = true;
                var scrub = l.gameObject.AddComponent<ScrubHandle>();
                scrub.onDelta = steps => f.Bump(steps);
                UIFactory.Tip(l, "Drag left or right to scrub " + label.ToLowerInvariant() + " (Shift = fine)");
            }
            UIFactory.Button(f.root, "-", () => f.Bump(-1), 26, height - 2, null, 16);
            f.input = UIFactory.Input(f.root, "", f.Format(value), f.Commit, -1, height - 2, InputField.ContentType.Standard, 13);
            UIFactory.Button(f.root, "+", () => f.Bump(1), 26, height - 2, null, 16);
            return f;
        }

        string Format(float v)
        {
            return integer ? Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture) : Math.Round(v, 3).ToString(CultureInfo.InvariantCulture);
        }

        void Bump(int dir)
        {
            SetAndNotify(value + step * dir);
        }

        void Commit(string s)
        {
            if (suppress) return;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) SetAndNotify(v);
            else Set(value);
        }

        void SetAndNotify(float v)
        {
            v = Mathf.Clamp(v, min, max);
            if (integer) v = Mathf.Round(v);
            value = v;
            suppress = true;
            input.SetTextWithoutNotify(Format(v));
            suppress = false;
            onChange?.Invoke(v);
        }

        public void Set(float v)
        {
            value = v;
            suppress = true;
            input.SetTextWithoutNotify(Format(v));
            suppress = false;
        }

        public void SetMixed()
        {
            suppress = true;
            input.SetTextWithoutNotify("");
            var ph = input.placeholder as Text;
            if (ph != null) ph.text = "— mixed —";
            suppress = false;
        }
    }

    public class TextField
    {
        public RectTransform root;
        public InputField input;
        public Action<string> onChange;

        public static TextField Create(Transform parent, string label, string value, Action<string> onChange, float labelWidth = 110f, float height = 28f, string placeholder = "")
        {
            var f = new TextField { onChange = onChange };
            f.root = UIFactory.Row(parent, height, 4);
            if (!string.IsNullOrEmpty(label)) UIFactory.Label(f.root, label, 13, TextAnchor.MiddleLeft, UIFactory.TextDim, labelWidth, height);
            f.input = UIFactory.Input(f.root, placeholder, value, s => f.onChange?.Invoke(s), -1, height - 2);
            return f;
        }

        public void Set(string v) => input.SetTextWithoutNotify(v ?? "");
    }

    public class BoolField
    {
        public Toggle toggle;
        public Action<bool> onChange;

        public static BoolField Create(Transform parent, string label, bool value, Action<bool> onChange, float height = 26f)
        {
            var f = new BoolField { onChange = onChange };
            f.toggle = UIFactory.Toggle(parent, label, value, v => f.onChange?.Invoke(v), height);
            return f;
        }

        public void Set(bool v) => toggle.SetIsOnWithoutNotify(v);
    }

    /// <summary>A button that opens a popup list of options.</summary>
    public class DropdownField
    {
        public RectTransform root;
        public Button button;
        public string[] options;
        public string[] labels;
        public int index;
        public Action<int> onChange;

        public static DropdownField Create(Transform parent, string label, string[] options, int index, Action<int> onChange, float labelWidth = 110f, float height = 28f, string[] labels = null)
        {
            var f = new DropdownField { options = options, labels = labels ?? options, index = index, onChange = onChange };
            f.root = UIFactory.Row(parent, height, 4);
            if (!string.IsNullOrEmpty(label)) UIFactory.Label(f.root, label, 13, TextAnchor.MiddleLeft, UIFactory.TextDim, labelWidth, height);
            f.button = UIFactory.Button(f.root, f.CurrentLabel() + "  ▾", null, -1, height - 2, UIFactory.InputBg, 13);
            f.button.onClick.AddListener(() =>
            {
                EditorUI.Instance.ShowDropdown(f.button.GetComponent<RectTransform>(), f.labels, f.index, i =>
                {
                    f.index = i;
                    UIFactory.SetButtonLabel(f.button, f.CurrentLabel() + "  ▾");
                    f.onChange?.Invoke(i);
                });
            });
            return f;
        }

        string CurrentLabel() => index >= 0 && index < labels.Length ? labels[index] : "—";

        public void Set(int i)
        {
            index = i;
            UIFactory.SetButtonLabel(button, CurrentLabel() + "  ▾");
        }

        public void SetByValue(string value)
        {
            int i = Array.IndexOf(options, value);
            Set(i);
        }

        public string Value => index >= 0 && index < options.Length ? options[index] : "";
    }

    /// <summary>Hex input + swatch that opens a colour picker popup.</summary>
    public class ColorField
    {
        public RectTransform root;
        public InputField hex;
        public Image swatch;
        public Action<Color> onChange;
        Color value;

        public static ColorField Create(Transform parent, string label, Color value, Action<Color> onChange, float labelWidth = 110f, float height = 28f)
        {
            var f = new ColorField { onChange = onChange, value = value };
            f.root = UIFactory.Row(parent, height, 4);
            if (!string.IsNullOrEmpty(label)) UIFactory.Label(f.root, label, 13, TextAnchor.MiddleLeft, UIFactory.TextDim, labelWidth, height);
            f.swatch = UIFactory.Swatch(f.root, value, height - 2);
            var swatchButton = f.swatch.gameObject.AddComponent<Button>();
            swatchButton.targetGraphic = f.swatch;
            swatchButton.onClick.AddListener(() =>
            {
                EditorUI.Instance.ShowColorPicker(f.swatch.rectTransform, f.value, c =>
                {
                    f.Set(c);
                    f.onChange?.Invoke(c);
                });
            });
            f.hex = UIFactory.Input(f.root, "#RRGGBBAA", "#" + ColorUtility.ToHtmlStringRGBA(value), s =>
            {
                if (ColorUtility.TryParseHtmlString(s.StartsWith("#") ? s : "#" + s, out var c))
                {
                    f.Set(c);
                    f.onChange?.Invoke(c);
                }
                else f.Set(f.value);
            }, -1, height - 2);
            return f;
        }

        public void Set(Color c)
        {
            value = c;
            swatch.color = c;
            hex.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGBA(c));
        }
    }
}
