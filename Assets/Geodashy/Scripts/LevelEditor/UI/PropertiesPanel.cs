using System;
using System.Collections.Generic;
using System.Globalization;
using Geodashy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Right dock: transform, layering, colour, groups, flags and per-type properties of the selection.</summary>
    public class PropertiesPanel : MonoBehaviour
    {
        EditorUI ui;
        LevelEditor editor;
        RectTransform content, body, pinned;
        Image pinnedIcon;
        Text pinnedTitle, pinnedSub;
        ScrollRect scroll;
        bool applying;
        // collapsed section names persist between sessions
        const string CollapsedPref = "geodashy.props.collapsed";
        static readonly HashSet<string> collapsed = new HashSet<string>(PlayerPrefs.GetString("geodashy.props.collapsed", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        // copied properties for Paste props
        static LevelObject propClipboard;

        NumberField xField, yField, rotField, sxField, syField, zOrderField, layerField;
        BoolField flipXField, flipYField;
        DropdownField zLayerField, baseColorField, detailColorField;
        RectTransform groupChips;

        static readonly string[] zLayerNames = { "B4", "B3", "B2", "B1", "T1", "T2", "T3", "T4" };
        static readonly int[] zLayerValues = { -4, -3, -2, -1, 1, 2, 3, 4 };

        public static PropertiesPanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "PropertiesPanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<PropertiesPanel>();
            p.ui = ui;
            p.editor = ui.editor;
            p.Build(rt);
            return p;
        }

        void Build(RectTransform rt)
        {
            // pinned header: object icon, name and count stay put while the fields scroll
            pinned = UIFactory.Panel(rt, "Pinned", new Color(0, 0, 0, 0.25f));
            UIFactory.Anchor(pinned, new Vector2(0, 1), new Vector2(1, 1), new Vector2(6, -48), new Vector2(-6, -6));
            pinned.GetComponent<Image>().raycastTarget = false;
            pinnedIcon = UIFactory.Icon(pinned, null, 36);
            Destroy(pinnedIcon.GetComponent<LayoutElement>());
            UIFactory.Anchor(pinnedIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, -18), new Vector2(40, 18));
            pinnedTitle = UIFactory.Label(pinned, "", 16, TextAnchor.MiddleLeft, UIFactory.Accent, -1, -1, true);
            UIFactory.Anchor(pinnedTitle.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(46, -2), new Vector2(-4, -2));
            pinnedTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            pinnedSub = UIFactory.Label(pinned, "", 11, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Anchor(pinnedSub.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(46, 2), new Vector2(-4, 2));
            pinnedSub.horizontalOverflow = HorizontalWrapMode.Overflow;
            scroll = UIFactory.ScrollView(rt, "Scroll", out content, true, false, Color.clear);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>(), 0, 0, 0, 50);
            UIFactory.VLayout(content, 4, 8);
            UIFactory.Fitter(content, true, false);
            editor.SelectionChanged += Rebuild;
            editor.LevelChanged += OnLevelChanged;
            Rebuild();
        }

        void OnLevelChanged()
        {
            if (!applying) RefreshValues();
        }

        List<LevelObject> Selected => editor.SelectedObjects();

        void Apply(Action<LevelObject> edit)
        {
            applying = true;
            editor.EditSelection(edit, false);
            applying = false;
            RefreshValues();
        }

        static string ColorChannelName(int id) => ColorChannelIds.Name(id);

        static readonly int[] colorChannelValues =
        {
            ColorChannelIds.Default, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            ColorChannelIds.Background, ColorChannelIds.Ground, ColorChannelIds.Line, ColorChannelIds.Object, ColorChannelIds.Player1, ColorChannelIds.Player2, ColorChannelIds.Detail
        };

        static string[] ColorChannelLabels()
        {
            var l = new string[colorChannelValues.Length];
            for (int i = 0; i < l.Length; i++) l[i] = ColorChannelName(colorChannelValues[i]);
            return l;
        }

        /// <summary>A collapsible section: a clickable ribbon header and a body the fields go into.</summary>
        RectTransform Section(string title)
        {
            bool closed = collapsed.Contains(title);
            var row = UIFactory.Row(content, 22, 4);
            var b = UIFactory.Button(row, (closed ? "▸ " : "▾ ") + title.ToUpperInvariant(), () =>
            {
                if (!collapsed.Remove(title)) collapsed.Add(title);
                PlayerPrefs.SetString(CollapsedPref, string.Join("|", collapsed));
                Rebuild();
            }, -1, 20, new Color(0, 0, 0, 0.001f), 11);
            var t = b.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.color = UIFactory.AccentDim;
            t.fontStyle = FontStyle.Bold;
            UIFactory.Tip(b, (closed ? "Expand " : "Collapse ") + title.ToLowerInvariant());
            var sec = UIFactory.Column(content, -1, 4, 0);
            if (closed) sec.gameObject.SetActive(false);
            body = sec;
            return sec;
        }

        void CopyProps(LevelObject from)
        {
            propClipboard = from.Clone();
            ui.Notify("Properties copied from " + ObjectCatalog.Get(from.type).name + " — Paste props applies them to any selection", 3f, "copy");
        }

        void PasteProps(List<LevelObject> objs)
        {
            if (propClipboard == null)
            {
                ui.Toast("Nothing copied yet: use Copy props on an object first");
                return;
            }
            var src = propClipboard;
            applying = true;
            editor.EditSelection(o =>
            {
                o.rotation = src.rotation;
                o.scaleX = src.scaleX;
                o.scaleY = src.scaleY;
                o.flipX = src.flipX;
                o.flipY = src.flipY;
                o.zLayer = src.zLayer;
                o.zOrder = src.zOrder;
                o.editorLayer = src.editorLayer;
                o.baseColor = src.baseColor;
                o.detailColor = src.detailColor;
                o.dontFade = src.dontFade;
                o.dontEnter = src.dontEnter;
                o.noTouch = src.noTouch;
                o.highDetail = src.highDetail;
                if (o.type == src.type) foreach (var p in src.props) o.SetProp(p.key, p.value);
            }, false, "Paste properties");
            applying = false;
            Rebuild();
            ui.Notify("Pasted properties onto " + objs.Count + (objs.Count == 1 ? " object" : " objects"), 3f, "paste", "Undo", editor.Undo);
        }

        public void Rebuild()
        {
            if (content == null) return;
            foreach (Transform child in content) Destroy(child.gameObject);
            body = content;
            var objs = Selected;
            if (pinned != null) pinned.gameObject.SetActive(objs.Count > 0);
            if (objs.Count == 0) return;

            var first = objs[0];
            bool sameType = true;
            foreach (var o in objs) if (o.type != first.type) sameType = false;
            var def = ObjectCatalog.Get(first.type);

            // pinned header --------------------------------------------------------------
            pinnedIcon.sprite = sameType ? Geodashy.Rendering.SpriteLibrary.ForObject(def) : null;
            pinnedIcon.enabled = sameType;
            pinnedTitle.text = sameType ? (objs.Count > 1 ? objs.Count + " × " + def.name : def.name) : objs.Count + " objects";
            pinnedSub.text = objs.Count == 1 ? "uid " + first.uid + " · " + def.category + " · " + def.kind : (sameType ? def.category + " · " + def.kind : "mixed types · blank fields differ");
            var header = UIFactory.Row(content, 26, 6);
            if (sameType) UIFactory.Button(header, "Select all " + UIFactory.Trunc(def.name, 12), () => editor.SelectByType(def.id), -1, 24, null, 11);
            UIFactory.Tip(UIFactory.IconButton(header, "copy", "Copy props", () => CopyProps(first), -1, 24, null, 11), "Copy this object's transform, colours, flags and properties");
            UIFactory.Tip(UIFactory.IconButton(header, "paste", "Paste props", () => PasteProps(objs), -1, 24, propClipboard != null ? UIFactory.ButtonActive : (Color?)null, 11), "Apply the copied properties to the selection");
            if (sameType && !string.IsNullOrEmpty(def.description)) UIFactory.Label(content, def.description, 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 34);

            // transform ---------------------------------------------------------------
            Section("Transform");
            var center = editor.SelectionCenter();
            xField = NumberField.Create(body, "X", objs.Count == 1 ? first.x : center.x, editor.gridSize, -1000, 100000, v =>
            {
                if (objs.Count == 1) Apply(o => o.x = v);
                else
                {
                    float d = v - editor.SelectionCenter().x;
                    Apply(o => o.x += d);
                }
            }, false, 70);
            yField = NumberField.Create(body, "Y", objs.Count == 1 ? first.y : center.y, editor.gridSize, -1000, 100000, v =>
            {
                if (objs.Count == 1) Apply(o => o.y = v);
                else
                {
                    float d = v - editor.SelectionCenter().y;
                    Apply(o => o.y += d);
                }
            }, false, 70);
            // with several objects, a field only shows a value when they all agree; blank means "mixed" and
            // typing a number sets just that field on every object
            bool Mixed(Func<LevelObject, float> get)
            {
                if (objs.Count < 2) return false;
                float v0 = get(objs[0]);
                foreach (var o in objs) if (Mathf.Abs(get(o) - v0) > 0.0001f) return true;
                return false;
            }
            rotField = NumberField.Create(body, "Rotation", first.rotation, 15, -3600, 3600, v => Apply(o => o.rotation = GeoMath.NormalizeAngle(v)), false, 70);
            if (Mixed(o => o.rotation)) rotField.SetMixed();
            sxField = NumberField.Create(body, "Scale X", first.scaleX, 0.25f, 0.125f, 16, v => Apply(o => o.scaleX = v), false, 70);
            if (Mixed(o => o.scaleX)) sxField.SetMixed();
            syField = NumberField.Create(body, "Scale Y", first.scaleY, 0.25f, 0.125f, 16, v => Apply(o => o.scaleY = v), false, 70);
            if (Mixed(o => o.scaleY)) syField.SetMixed();
            if (objs.Count > 1) UIFactory.Label(body, "Blank fields differ between the selected objects; enter a value to set it on all of them.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            var fr = UIFactory.Row(body, 26, 8);
            flipXField = BoolField.Create(fr, "Flip X", first.flipX, v => Apply(o => o.flipX = v));
            flipYField = BoolField.Create(fr, "Flip Y", first.flipY, v => Apply(o => o.flipY = v));

            // layering ------------------------------------------------------------------
            Section("Layering");
            int zIdx = Array.IndexOf(zLayerValues, first.zLayer == 0 ? 1 : first.zLayer);
            zLayerField = DropdownField.Create(body, "Z layer", zLayerNames, Mathf.Max(0, zIdx), i => Apply(o => o.zLayer = zLayerValues[i]), 70);
            zOrderField = NumberField.Create(body, "Z order", first.zOrder, 1, -50, 50, v => Apply(o => o.zOrder = Mathf.RoundToInt(v)), true, 70);
            if (Mixed(o => o.zOrder)) zOrderField.SetMixed();
            layerField = NumberField.Create(body, "Editor layer", first.editorLayer, 1, 0, 99, v => Apply(o => o.editorLayer = Mathf.RoundToInt(v)), true, 70);
            if (Mixed(o => o.editorLayer)) layerField.SetMixed();

            // colour ----------------------------------------------------------------------
            Section("Look");
            var labels = ColorChannelLabels();
            baseColorField = DropdownField.Create(body, "Base", labels, Mathf.Max(0, Array.IndexOf(colorChannelValues, first.baseColor)), i => Apply(o => o.baseColor = colorChannelValues[i]), 70);
            ChannelSwatch(baseColorField, () => first.baseColor);
            detailColorField = DropdownField.Create(body, "Detail", labels, Mathf.Max(0, Array.IndexOf(colorChannelValues, first.detailColor)), i => Apply(o => o.detailColor = colorChannelValues[i]), 70);
            ChannelSwatch(detailColorField, () => first.detailColor);
            UIFactory.Label(body, "The swatch shows the channel's colour; click it to recolour the channel for every object using it.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);

            // groups ------------------------------------------------------------------------
            Section("Groups");
            groupChips = UIFactory.Rect(body, "Chips");
            UIFactory.Grid(groupChips, new Vector2(64, 24), new Vector2(4, 4), 0);
            UIFactory.Layout(groupChips.gameObject, -1, 56, 1);
            var gr = UIFactory.Row(body, 28, 4);
            var groupInput = UIFactory.Input(gr, "group #", editor.NextFreeGroup().ToString(), null, 70, 26, InputField.ContentType.IntegerNumber);
            UIFactory.Button(gr, "Add", () =>
            {
                if (int.TryParse(groupInput.text, out var g) && g > 0)
                {
                    Apply(o => o.AddGroup(g));
                    Rebuild();
                }
            }, -1, 26, null, 12);
            UIFactory.Button(gr, "Next free", () => groupInput.text = editor.NextFreeGroup().ToString(), -1, 26, null, 12);
            UIFactory.Button(gr, "Select", () =>
            {
                if (int.TryParse(groupInput.text, out var g)) editor.SelectByGroup(g);
            }, -1, 26, null, 12);
            RebuildGroupChips(objs);

            // flags ---------------------------------------------------------------------------
            Section("Behaviour");
            var f1 = UIFactory.Row(body, 26, 8);
            BoolField.Create(f1, "Don't fade", first.dontFade, v => Apply(o => o.dontFade = v));
            BoolField.Create(f1, "Don't enter", first.dontEnter, v => Apply(o => o.dontEnter = v));
            var f2 = UIFactory.Row(body, 26, 8);
            BoolField.Create(f2, "No touch", first.noTouch, v => Apply(o => o.noTouch = v));
            BoolField.Create(f2, "High detail", first.highDetail, v => Apply(o => o.highDetail = v));

            // object specific ------------------------------------------------------------------
            if (sameType && def.props.Count > 0)
            {
                Section(def.kind == ObjectKind.Trigger ? "Trigger" : "Properties");
                foreach (var prop in def.props) BuildPropField(prop, objs, first);
            }
            body = content;

            UIFactory.Spacer(content, 20);
            var actions = UIFactory.Row(content, 28, 4);
            UIFactory.Button(actions, "Duplicate", editor.DuplicateSelection, -1, 26, null, 12);
            UIFactory.Button(actions, "Copy", editor.CopySelection, -1, 26, null, 12);
            UIFactory.Button(actions, "Delete", editor.DeleteSelection, -1, 26, UIFactory.Danger, 12);
            scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Small swatch beside a colour-channel dropdown; click to recolour the channel itself.</summary>
        void ChannelSwatch(DropdownField field, Func<int> channel)
        {
            int ch = channel();
            var col = editor.level.ResolveColor(ch, Color.white);
            var sw = UIFactory.Swatch(field.root, col, 22);
            UIFactory.Layout(sw.gameObject, 22, 22);
            var b = sw.gameObject.AddComponent<Button>();
            b.targetGraphic = sw;
            UIFactory.Tip(sw, ch == ColorChannelIds.Default ? "Default: the object's own colours" : "Channel " + ColorChannelIds.Name(ch) + " · click to recolour");
            b.onClick.AddListener(() =>
            {
                int c = channel();
                if (c == ColorChannelIds.Default)
                {
                    ui.Toast("Pick a channel first; Default keeps the object's own colours");
                    return;
                }
                var chan = editor.level.GetOrCreateColorChannel(c);
                ui.ShowColorPicker(sw.rectTransform, chan.color, picked =>
                {
                    chan.color = new Color(picked.r, picked.g, picked.b, 1f);
                    chan.opacity = picked.a;
                    sw.color = picked;
                    editor.MarkDirty();
                    editor.RefreshAllViews();
                });
            });
        }

        void RebuildGroupChips(List<LevelObject> objs)
        {
            foreach (Transform child in groupChips) Destroy(child.gameObject);
            var groups = new SortedSet<int>();
            foreach (var o in objs) foreach (var g in o.groups) groups.Add(g);
            if (groups.Count == 0)
            {
                UIFactory.Label(groupChips, "none", 12, TextAnchor.MiddleLeft, UIFactory.TextDim);
                return;
            }
            foreach (var g in groups)
            {
                int gg = g;
                bool all = true;
                foreach (var o in objs) if (!o.InGroup(gg)) all = false;
                var b = UIFactory.Button(groupChips, (all ? "" : "~") + gg + " ✕", () =>
                {
                    Apply(o => o.groups.Remove(gg));
                    Rebuild();
                }, 64, 24, all ? UIFactory.ButtonActive : UIFactory.ButtonBg, 12);
            }
        }

        void BuildPropField(PropDef prop, List<LevelObject> objs, LevelObject first)
        {
            string key = prop.key;
            switch (prop.type)
            {
                case PropType.Float:
                    NumberField.Create(body, prop.label, first.GetFloat(key, Parse(prop.defaultValue)), prop.step, prop.min, prop.max, v => Apply(o => o.SetProp(key, v)), false, 120);
                    break;
                case PropType.Int:
                    NumberField.Create(body, prop.label, first.GetInt(key, (int)Parse(prop.defaultValue)), Mathf.Max(1, prop.step), prop.min, prop.max, v => Apply(o => o.SetProp(key, Mathf.RoundToInt(v))), true, 120);
                    break;
                case PropType.Group:
                {
                    var row = UIFactory.Row(body, 28, 4);
                    var nf = NumberField.Create(row, prop.label, first.GetInt(key, 0), 1, 0, 9999, v => Apply(o => o.SetProp(key, Mathf.RoundToInt(v))), true, 110);
                    UIFactory.Layout(nf.root.gameObject, -1, 28, 1);
                    UIFactory.Button(row, "sel", () => editor.SelectByGroup(first.GetInt(key, 0)), 40, 26, null, 11);
                    break;
                }
                case PropType.Bool:
                    BoolField.Create(body, prop.label + (string.IsNullOrEmpty(prop.help) ? "" : "  ⓘ"), first.GetBool(key, prop.defaultValue == "1"), v => Apply(o => o.SetProp(key, v)));
                    if (!string.IsNullOrEmpty(prop.help)) UIFactory.Label(body, prop.help, 10, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 16);
                    break;
                case PropType.Color:
                    ColorField.Create(body, prop.label, first.GetColor(key, Color.white), c => Apply(o => o.SetProp(key, c)), 120);
                    break;
                case PropType.Text:
                    TextField.Create(body, prop.label, first.GetString(key, prop.defaultValue), s => Apply(o => o.SetProp(key, s)), 120);
                    break;
                case PropType.Enum:
                {
                    var values = prop.enumValues ?? new string[0];
                    int idx = Mathf.Max(0, Array.IndexOf(values, first.GetString(key, prop.defaultValue)));
                    DropdownField.Create(body, prop.label, values, idx, i => Apply(o => o.SetProp(key, values[i])), 120);
                    break;
                }
                case PropType.Easing:
                {
                    var values = Enum.GetNames(typeof(Easing));
                    int idx = Mathf.Max(0, Array.IndexOf(values, first.GetString(key, "Linear")));
                    DropdownField.Create(body, prop.label, values, idx, i => Apply(o => o.SetProp(key, values[i])), 120);
                    break;
                }
                case PropType.Mount:
                {
                    var ids = MountCatalog.Ids;
                    var names = new string[ids.Length];
                    for (int i = 0; i < ids.Length; i++) names[i] = MountCatalog.Get(ids[i]).name;
                    int idx = Mathf.Max(0, Array.IndexOf(ids, first.GetString(key, "horse")));
                    DropdownField.Create(body, prop.label, ids, idx, i => Apply(o => o.SetProp(key, ids[i])), 120, 28, names);
                    break;
                }
                case PropType.Speed:
                {
                    int idx = Mathf.Clamp(first.GetInt(key, 1), 0, 4);
                    DropdownField.Create(body, prop.label, MountCatalog.SpeedLabels, idx, i => Apply(o => o.SetProp(key, i)), 120);
                    break;
                }
                case PropType.Theme:
                {
                    var ids = ThemeCatalog.BackgroundIds;
                    var names = new string[ids.Length];
                    for (int i = 0; i < ids.Length; i++) names[i] = ThemeCatalog.GetBackground(ids[i]).name;
                    int idx = Mathf.Max(0, Array.IndexOf(ids, first.GetString(key, "castle")));
                    DropdownField.Create(body, prop.label, ids, idx, i => Apply(o => o.SetProp(key, ids[i])), 120, 28, names);
                    break;
                }
            }
        }

        static float Parse(string s)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;
        }

        /// <summary>Updates the transform fields without rebuilding the panel (after nudges, drags, undo).</summary>
        public void RefreshValues()
        {
            var objs = Selected;
            if (objs.Count == 0 || xField == null) return;
            var first = objs[0];
            var center = editor.SelectionCenter();
            xField.Set(objs.Count == 1 ? first.x : center.x);
            yField.Set(objs.Count == 1 ? first.y : center.y);
            rotField.Set(first.rotation);
            sxField.Set(first.scaleX);
            syField.Set(first.scaleY);
            flipXField.Set(first.flipX);
            flipYField.Set(first.flipY);
            zOrderField.Set(first.zOrder);
            layerField.Set(first.editorLayer);
        }
    }
}
