using Geodashy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Edit-mode dock: move / rotate / flip / scale / clipboard / selection / align tools.</summary>
    public class EditPanel : MonoBehaviour
    {
        EditorUI ui;
        LevelEditor editor;
        Text selInfo, heatLabel;
        Toggle heatToggle;
        InputField groupInput;
        Button[] stepButtons;
        Button stepButton, heatButton;   // phone layout
        readonly float[] steps = { 0.125f, 0.5f, 1f, 5f };
        static readonly string[] stepNames = { "⅛", "½", "1", "5" };
        int stepIndex = 2;

        public static EditPanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "EditPanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<EditPanel>();
            p.ui = ui;
            p.editor = ui.editor;
            if (ui.IsPhone) p.BuildPhone(rt);
            else p.Build(rt);
            return p;
        }

        /// <summary>Phone dock: a horizontally scrolling strip of tool groups, each two rows of 44-unit buttons.</summary>
        void BuildPhone(RectTransform rt)
        {
            var scroll = UIFactory.ScrollView(rt, "Scroll", out var c, false, true, Color.clear);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>());
            UIFactory.HLayout(c, 10, 6, false, TextAnchor.UpperLeft);
            c.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            UIFactory.Fitter(c, false, true);
            const float h = 44f;

            RectTransform Group(string header, float width)
            {
                var col = UIFactory.Column(c, width, 4, 0);
                UIFactory.Label(col, header, 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 16);
                return col;
            }

            var move = Group("Move", 170);
            var m1 = UIFactory.Row(move, h, 4);
            UIFactory.Button(m1, "◀", () => Nudge(-1, 0), -1, h, null, 16);
            UIFactory.Button(m1, "▲", () => Nudge(0, 1), -1, h, null, 16);
            UIFactory.Button(m1, "▶", () => Nudge(1, 0), -1, h, null, 16);
            var m2 = UIFactory.Row(move, h, 4);
            stepButton = UIFactory.Button(m2, "step 1", () => SetStep((stepIndex + 1) % steps.Length), -1, h, null, 12);
            UIFactory.Button(m2, "▼", () => Nudge(0, -1), -1, h, null, 16);
            UIFactory.Button(m2, "Snap", editor.SnapSelectionToGrid, -1, h, null, 12);
            SetStep(2);

            var rot = Group("Rotate & flip", 230);
            var q1 = UIFactory.Row(rot, h, 4);
            UIFactory.Button(q1, "↺ 90", () => editor.RotateSelection(90), -1, h, null, 12);
            UIFactory.Button(q1, "↻ 90", () => editor.RotateSelection(-90), -1, h, null, 12);
            UIFactory.Button(q1, "↺ 45", () => editor.RotateSelection(45), -1, h, null, 12);
            UIFactory.Button(q1, "↻ 45", () => editor.RotateSelection(-45), -1, h, null, 12);
            var q2 = UIFactory.Row(rot, h, 4);
            UIFactory.Button(q2, "Flip H", () => editor.FlipSelection(true), -1, h, null, 12);
            UIFactory.Button(q2, "Flip V", () => editor.FlipSelection(false), -1, h, null, 12);
            UIFactory.Button(q2, "↺ 5", () => editor.RotateSelection(5), -1, h, null, 12);
            UIFactory.Button(q2, "↻ 5", () => editor.RotateSelection(-5), -1, h, null, 12);

            var scale = Group("Scale", 120);
            var s1 = UIFactory.Row(scale, h, 4);
            UIFactory.Button(s1, "× 2", () => editor.ScaleSelection(2f), -1, h, null, 12);
            UIFactory.Button(s1, "× ½", () => editor.ScaleSelection(0.5f), -1, h, null, 12);
            var s2 = UIFactory.Row(scale, h, 4);
            UIFactory.Button(s2, "× 1.25", () => editor.ScaleSelection(1.25f), -1, h, null, 12);
            UIFactory.Button(s2, "× 0.8", () => editor.ScaleSelection(0.8f), -1, h, null, 12);

            var clip = Group("Clipboard", 280);
            var c1 = UIFactory.Row(clip, h, 4);
            UIFactory.Button(c1, "Copy", editor.CopySelection, -1, h, null, 12);
            UIFactory.Button(c1, "Paste", () => editor.Paste(false), -1, h, null, 12);
            UIFactory.Button(c1, "Dup", editor.DuplicateSelection, -1, h, null, 12);
            UIFactory.Button(c1, "Stamp…", ui.PromptSaveStamp, -1, h, UIFactory.ButtonActive, 12);
            var c2 = UIFactory.Row(clip, h, 4);
            UIFactory.Button(c2, "Cut", editor.CutSelection, -1, h, null, 12);
            UIFactory.Button(c2, "Delete", editor.DeleteSelection, -1, h, UIFactory.Danger, 12);
            UIFactory.Button(c2, "Reset", () => editor.EditSelection(o =>
            {
                o.rotation = 0;
                o.scaleX = 1;
                o.scaleY = 1;
            }), -1, h, null, 12);

            var sel = Group("Select", 210);
            var e1 = UIFactory.Row(sel, h, 4);
            UIFactory.Button(e1, "All", editor.SelectAll, -1, h, null, 12);
            UIFactory.Button(e1, "None", editor.Deselect, -1, h, null, 12);
            UIFactory.Button(e1, "Invert", editor.InvertSelection, -1, h, null, 12);
            var e2 = UIFactory.Row(sel, h, 4);
            UIFactory.Button(e2, "Same type", () =>
            {
                var objs = editor.SelectedObjects();
                if (objs.Count > 0) editor.SelectByType(objs[0].type);
                else ui.Toast("Select an object first");
            }, -1, h, null, 12);
            UIFactory.Button(e2, "Group…", () => ui.Prompt("Select group", "Group number:", "1", v =>
            {
                if (int.TryParse(v, out var g)) editor.SelectByGroup(g);
            }), -1, h, null, 12);
            UIFactory.Button(e2, "→ Brush", editor.ReplaceSelectionWithBrush, -1, h, UIFactory.ButtonActive, 12);

            var align = Group("Align", 210);
            var a1 = UIFactory.Row(align, h, 4);
            UIFactory.Button(a1, "Left", () => editor.AlignSelection("left"), -1, h, null, 12);
            UIFactory.Button(a1, "Right", () => editor.AlignSelection("right"), -1, h, null, 12);
            UIFactory.Button(a1, "Ctr X", () => editor.AlignSelection("centerX"), -1, h, null, 12);
            var a2 = UIFactory.Row(align, h, 4);
            UIFactory.Button(a2, "Top", () => editor.AlignSelection("top"), -1, h, null, 12);
            UIFactory.Button(a2, "Bottom", () => editor.AlignSelection("bottom"), -1, h, null, 12);
            UIFactory.Button(a2, "Ctr Y", () => editor.AlignSelection("centerY"), -1, h, null, 12);

            var heat = Group("Defeat heatmap", 150);
            heatButton = UIFactory.Button(heat, "Deaths: off", () =>
            {
                editor.showDeathHeatmap = !editor.showDeathHeatmap;
                editor.NotifyViewOptionsChanged();
            }, -1, h, null, 12);
            UIFactory.Button(heat, "Clear", () =>
            {
                editor.ClearSessionDeaths();
                ui.Toast("Defeat heatmap cleared");
            }, -1, h, null, 12);

            editor.SelectionChanged += RefreshInfo;
            editor.DeathsChanged += RefreshHeat;
            editor.ViewOptionsChanged += RefreshHeat;
            RefreshInfo();
            RefreshHeat();
        }

        RectTransform Col(Transform parent, string header, float width)
        {
            var c = UIFactory.Column(parent, width, 4, 4);
            UIFactory.SectionHeader(c, header);
            return c;
        }

        void Build(RectTransform rt)
        {
            UIFactory.HLayout(rt, 10, 8, false, TextAnchor.UpperLeft);
            rt.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            // move ----------------------------------------------------------------
            var move = Col(rt, "Move", 170);
            var r1 = UIFactory.Row(move, 30, 4, TextAnchor.MiddleCenter);
            UIFactory.Spacer(r1, 28, 50);
            UIFactory.Button(r1, "▲", () => Nudge(0, 1), 50, 28);
            UIFactory.Spacer(r1, 28, 50);
            var r2 = UIFactory.Row(move, 30, 4, TextAnchor.MiddleCenter);
            UIFactory.Button(r2, "◀", () => Nudge(-1, 0), 50, 28);
            UIFactory.Button(r2, "▼", () => Nudge(0, -1), 50, 28);
            UIFactory.Button(r2, "▶", () => Nudge(1, 0), 50, 28);
            UIFactory.Label(move, "step", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 16);
            var r3 = UIFactory.Row(move, 26, 3);
            stepButtons = new Button[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                int idx = i;
                stepButtons[i] = UIFactory.Button(r3, stepNames[i], () => SetStep(idx), -1, 24, null, 12);
            }
            UIFactory.Button(move, "Snap to grid", editor.SnapSelectionToGrid, -1, 26, null, 12);
            SetStep(2);

            // rotate / flip ---------------------------------------------------------
            var rot = Col(rt, "Rotate & Flip", 190);
            var q1 = UIFactory.Row(rot, 30, 4);
            UIFactory.Button(q1, "↺ 90", () => editor.RotateSelection(90), -1, 28, null, 12);
            UIFactory.Button(q1, "↻ 90", () => editor.RotateSelection(-90), -1, 28, null, 12);
            var q2 = UIFactory.Row(rot, 30, 4);
            UIFactory.Button(q2, "↺ 45", () => editor.RotateSelection(45), -1, 28, null, 12);
            UIFactory.Button(q2, "↻ 45", () => editor.RotateSelection(-45), -1, 28, null, 12);
            var q3 = UIFactory.Row(rot, 30, 4);
            UIFactory.Button(q3, "↺ 5", () => editor.RotateSelection(5), -1, 28, null, 12);
            UIFactory.Button(q3, "↻ 5", () => editor.RotateSelection(-5), -1, 28, null, 12);
            var q4 = UIFactory.Row(rot, 30, 4);
            UIFactory.Button(q4, "Flip H", () => editor.FlipSelection(true), -1, 28, null, 12);
            UIFactory.Button(q4, "Flip V", () => editor.FlipSelection(false), -1, 28, null, 12);
            var q5 = UIFactory.Row(rot, 30, 4);
            UIFactory.Button(q5, "Rotate each", () => editor.RotateSelection(90, false), -1, 28, null, 11);
            UIFactory.Button(q5, "Reset rot", () => editor.EditSelection(o => o.rotation = 0), -1, 28, null, 11);

            // scale ------------------------------------------------------------------
            var scale = Col(rt, "Scale", 120);
            UIFactory.Button(scale, "× 2", () => editor.ScaleSelection(2f), -1, 28, null, 12);
            UIFactory.Button(scale, "× 1.25", () => editor.ScaleSelection(1.25f), -1, 28, null, 12);
            UIFactory.Button(scale, "× 0.8", () => editor.ScaleSelection(0.8f), -1, 28, null, 12);
            UIFactory.Button(scale, "× 0.5", () => editor.ScaleSelection(0.5f), -1, 28, null, 12);
            UIFactory.Button(scale, "Reset", () => editor.EditSelection(o =>
            {
                o.scaleX = 1;
                o.scaleY = 1;
            }), -1, 28, null, 12);

            // clipboard --------------------------------------------------------------
            var clip = Col(rt, "Clipboard", 150);
            UIFactory.Button(clip, "Copy (Ctrl+C)", editor.CopySelection, -1, 28, null, 12);
            UIFactory.Button(clip, "Cut (Ctrl+X)", editor.CutSelection, -1, 28, null, 12);
            UIFactory.Button(clip, "Paste (Ctrl+V)", () => editor.Paste(false), -1, 28, null, 12);
            UIFactory.Button(clip, "Duplicate (Ctrl+D)", editor.DuplicateSelection, -1, 28, null, 12);
            UIFactory.Button(clip, "Delete (Del)", editor.DeleteSelection, -1, 28, UIFactory.Danger, 12);
            UIFactory.Button(clip, "Save as stamp…", ui.PromptSaveStamp, -1, 28, UIFactory.ButtonActive, 12);

            // selection ---------------------------------------------------------------
            var sel = Col(rt, "Select", 190);
            var s1 = UIFactory.Row(sel, 30, 4);
            UIFactory.Button(s1, "All", editor.SelectAll, -1, 28, null, 12);
            UIFactory.Button(s1, "None", editor.Deselect, -1, 28, null, 12);
            UIFactory.Button(s1, "Invert", editor.InvertSelection, -1, 28, null, 12);
            UIFactory.Button(sel, "Same type as selection (T)", () =>
            {
                var objs = editor.SelectedObjects();
                if (objs.Count > 0) editor.SelectByType(objs[0].type);
            }, -1, 28, null, 11);
            var rp = UIFactory.Row(sel, 30, 4);
            UIFactory.Button(rp, "Replace with brush", editor.ReplaceSelectionWithBrush, -1, 28, UIFactory.ButtonActive, 11);
            UIFactory.Button(rp, "Replace all of type", () =>
            {
                var objs = editor.SelectedObjects();
                if (objs.Count == 0) { ui.Toast("Select an object of the type to replace"); return; }
                editor.ReplaceAllOfTypeWithBrush(objs[0].type);
            }, -1, 28, null, 11);
            var s2 = UIFactory.Row(sel, 30, 4);
            UIFactory.Label(s2, "Group", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, 44, 28);
            groupInput = UIFactory.Input(s2, "#", "1", null, 50, 26, InputField.ContentType.IntegerNumber);
            UIFactory.Button(s2, "Select", () =>
            {
                if (int.TryParse(groupInput.text, out var g)) editor.SelectByGroup(g);
            }, -1, 26, null, 11);
            UIFactory.Button(s2, "Add", () =>
            {
                if (int.TryParse(groupInput.text, out var g)) editor.AddGroupToSelection(g);
            }, -1, 26, null, 11);
            selInfo = UIFactory.Label(sel, "", 12, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 60);

            // align --------------------------------------------------------------------
            var align = Col(rt, "Align", 150);
            var a1 = UIFactory.Row(align, 30, 4);
            UIFactory.Button(a1, "Left", () => editor.AlignSelection("left"), -1, 28, null, 12);
            UIFactory.Button(a1, "Right", () => editor.AlignSelection("right"), -1, 28, null, 12);
            var a2 = UIFactory.Row(align, 30, 4);
            UIFactory.Button(a2, "Top", () => editor.AlignSelection("top"), -1, 28, null, 12);
            UIFactory.Button(a2, "Bottom", () => editor.AlignSelection("bottom"), -1, 28, null, 12);
            var a3 = UIFactory.Row(align, 30, 4);
            UIFactory.Button(a3, "Center X", () => editor.AlignSelection("centerX"), -1, 28, null, 12);
            UIFactory.Button(a3, "Center Y", () => editor.AlignSelection("centerY"), -1, 28, null, 12);
            UIFactory.Label(align, "Drag to move · Shift+click adds · drag on empty space box-selects", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 48);

            // defeat heatmap ------------------------------------------------------------
            var heat = Col(rt, "Defeat heatmap", 190);
            heatToggle = UIFactory.Toggle(heat, "Show where I died", editor.showDeathHeatmap, v =>
            {
                editor.showDeathHeatmap = v;
                editor.NotifyViewOptionsChanged();
            }, 26);
            heatLabel = UIFactory.Label(heat, "", 12, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 54);
            UIFactory.Button(heat, "Clear session deaths", () =>
            {
                editor.ClearSessionDeaths();
                ui.Toast("Defeat heatmap cleared");
            }, -1, 26, null, 12);
            UIFactory.Label(heat, "Red blobs stack where tests ended; dots mark the contact points.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 34);

            editor.SelectionChanged += RefreshInfo;
            editor.DeathsChanged += RefreshHeat;
            editor.ViewOptionsChanged += RefreshHeat;
            RefreshInfo();
            RefreshHeat();
        }

        void RefreshHeat()
        {
            int n = editor.sessionDeaths.Count;
            if (heatButton != null)
            {
                UIFactory.SetButtonActive(heatButton, editor.showDeathHeatmap);
                UIFactory.SetButtonLabel(heatButton, (editor.showDeathHeatmap ? "Deaths: on" : "Deaths: off") + (n > 0 ? " (" + n + ")" : ""));
            }
            if (heatLabel == null) return;
            heatToggle.SetIsOnWithoutNotify(editor.showDeathHeatmap);
            if (n == 0)
            {
                heatLabel.text = "No deaths recorded yet this session.";
                return;
            }
            var counts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var d in editor.sessionDeaths)
            {
                var k = string.IsNullOrEmpty(d.killer) ? "world" : d.killer;
                counts[k] = counts.TryGetValue(k, out var c) ? c + 1 : 1;
            }
            string worst = ""; int worstN = 0;
            foreach (var kv in counts) if (kv.Value > worstN) { worst = kv.Key; worstN = kv.Value; }
            heatLabel.text = n + " death" + (n == 1 ? "" : "s") + " this session\nMost by: " + worst + " (" + worstN + ")";
        }

        void SetStep(int idx)
        {
            stepIndex = idx;
            editor.nudgeStep = steps[idx];
            if (stepButtons != null) for (int i = 0; i < stepButtons.Length; i++) UIFactory.SetButtonActive(stepButtons[i], i == idx);
            if (stepButton != null) UIFactory.SetButtonLabel(stepButton, "step " + stepNames[idx]);
        }

        void Nudge(int x, int y)
        {
            editor.MoveSelection(new Vector2(x, y) * editor.nudgeStep);
        }

        void RefreshInfo()
        {
            if (selInfo == null) return;
            int n = editor.selection.Count;
            if (n == 0)
            {
                selInfo.text = "Nothing selected.";
                return;
            }
            var b = editor.SelectionBounds();
            selInfo.text = string.Format("{0} selected\nbounds {1:0.##}×{2:0.##}\ncentre {3:0.##}, {4:0.##}", n, b.width, b.height, b.center.x, b.center.y);
        }
    }
}
