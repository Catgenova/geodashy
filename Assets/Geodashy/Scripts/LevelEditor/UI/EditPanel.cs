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
        Text selInfo;
        InputField groupInput;
        Button[] stepButtons;
        readonly float[] steps = { 0.125f, 0.5f, 1f, 5f };

        public static EditPanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "EditPanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<EditPanel>();
            p.ui = ui;
            p.editor = ui.editor;
            p.Build(rt);
            return p;
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
            string[] stepNames = { "⅛", "½", "1", "5" };
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

            editor.SelectionChanged += RefreshInfo;
            RefreshInfo();
        }

        void SetStep(int idx)
        {
            editor.nudgeStep = steps[idx];
            for (int i = 0; i < stepButtons.Length; i++) UIFactory.SetButtonActive(stepButtons[i], i == idx);
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
