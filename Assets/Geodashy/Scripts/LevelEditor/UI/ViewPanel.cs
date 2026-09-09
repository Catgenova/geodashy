using Geodashy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Left dock: grid, guides, editor layers, zoom and navigation.</summary>
    public class ViewPanel : MonoBehaviour
    {
        EditorUI ui;
        LevelEditor editor;
        Toggle snapToggle, gridToggle, guideToggle, bpmToggle, allLayersToggle, hitboxToggle, beatToggle, gizmoToggle, snapGuidesToggle;
        RectTransform groupsHost;
        Text groupsEmpty, budgetLabel;
        string groupsKey = "";
        Button[] beatButtons;
        Button[] gridButtons;
        readonly float[] gridSizes = { 0.25f, 0.5f, 1f, 2f };
        Text layerLabel, zoomLabel, posLabel, uiScaleLabel, savedLabel;
        Button layoutButton;
        InputField nameInput;
        Slider zoomSlider, posSlider;
        bool suppress;

        public static ViewPanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "ViewPanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<ViewPanel>();
            p.ui = ui;
            p.editor = ui.editor;
            p.Build(rt);
            return p;
        }

        void Build(RectTransform rt)
        {
            var scroll = UIFactory.ScrollView(rt, "Scroll", out var c, true, false, Color.clear);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>());
            UIFactory.VLayout(c, 5, 8);
            UIFactory.Fitter(c, true, false);

            UIFactory.SectionHeader(c, "Quest");
            nameInput = UIFactory.Input(c, "Quest name", editor.level.name, v =>
            {
                if (string.IsNullOrWhiteSpace(v)) return;
                editor.level.name = v.Trim();
                editor.MarkDirty();
            }, -1, 30);
            var qr1 = UIFactory.Row(c, 34, 4);
            UIFactory.Button(qr1, "▶ Test", () => editor.StartPlaytest(false), -1, 32, UIFactory.Good, 14);
            UIFactory.Button(qr1, "▶ Marker", () => editor.StartPlaytest(true), -1, 32, UIFactory.Good, 13);
            UIFactory.Button(c, "▶ Training (your own waystones)", () => editor.StartPlaytest(false, true), -1, 30, UIFactory.Good, 13);
            var diffNames = new[] { "Training", "Checkpoints", "Champion" };
            DropdownField.Create(c, "Test as", diffNames, (int)editor.TestDifficulty, i => editor.TestDifficulty = (Difficulty)i, 60, 26);
            var qr2 = UIFactory.Row(c, 30, 4);
            UIFactory.Button(qr2, "Save", ui.SaveWithPrompt, -1, 28, UIFactory.ButtonActive, 13);
            UIFactory.Button(qr2, "Save as…", ui.PromptSaveAs, -1, 28, null, 13);
            UIFactory.Button(qr2, "Files", ui.OpenFileDialog, -1, 28, null, 13);
            UIFactory.Button(qr2, "History", ui.OpenHistory, -1, 28, null, 13);
            budgetLabel = UIFactory.Label(c, "", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 46);
            savedLabel = UIFactory.Label(c, "", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 32);
            UIFactory.Button(c, "◀ Main menu", ui.ReturnToMenu, -1, 26, null, 12);

            UIFactory.SectionHeader(c, "Grid");
            snapToggle = UIFactory.Toggle(c, "Snap to grid (G)", editor.snapToGrid, v =>
            {
                editor.snapToGrid = v;
                editor.NotifyViewOptionsChanged();
            });
            gridToggle = UIFactory.Toggle(c, "Show grid (Ctrl+G)", editor.grid.showGrid, v => editor.grid.showGrid = v);
            var gr = UIFactory.Row(c, 26, 3);
            string[] names = { "¼", "½", "1", "2" };
            gridButtons = new Button[gridSizes.Length];
            for (int i = 0; i < gridSizes.Length; i++)
            {
                int idx = i;
                gridButtons[i] = UIFactory.Button(gr, names[i], () => editor.SetGridSize(gridSizes[idx]), -1, 24, null, 12);
            }

            snapGuidesToggle = UIFactory.Toggle(c, "Snap to neighbours (guides)", editor.snapGuides, v =>
            {
                editor.snapGuides = v;
                PlayerPrefs.SetInt(LevelEditor.SnapGuidesPref, v ? 1 : 0);
                editor.NotifyViewOptionsChanged();
            });
            UIFactory.Label(c, "While dragging, edges and centres snap to nearby objects and pink guide lines show the match.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 32);
            beatToggle = UIFactory.Toggle(c, "Snap X to the beat", editor.beatSnap, v =>
            {
                editor.beatSnap = v;
                if (v) editor.grid.showBpmGuide = true;
                editor.NotifyViewOptionsChanged();
            });
            var br = UIFactory.Row(c, 26, 3);
            string[] beatNames = { "1 beat", "½", "¼" };
            int[] beatDivs = { 1, 2, 4 };
            beatButtons = new Button[beatDivs.Length];
            for (int i = 0; i < beatDivs.Length; i++)
            {
                int idx = i;
                beatButtons[i] = UIFactory.Button(br, beatNames[i], () =>
                {
                    editor.beatDivision = beatDivs[idx];
                    editor.NotifyViewOptionsChanged();
                }, -1, 24, null, 12);
            }
            UIFactory.Label(c, "Uses the level BPM and start speed, so every obstacle lands on the music.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 32);

            UIFactory.SectionHeader(c, "Guides");
            gizmoToggle = UIFactory.Toggle(c, "Transform gizmo on selection (X)", editor.GizmoVisible, v => editor.GizmoVisible = v);
            UIFactory.Label(c, "Corners scale, edges scale one axis, the ring rotates, the centre moves. Hold Shift for fine steps.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 36);
            hitboxToggle = UIFactory.Toggle(c, "Hitbox edges on all objects (B)", editor.showHitboxes, v =>
            {
                editor.showHitboxes = v;
                editor.NotifyViewOptionsChanged();
            });
            UIFactory.Label(c, "Green = safe to land on, red = kills, blue = interacts. The brush preview always shows them.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 36);
            guideToggle = UIFactory.Toggle(c, "Camera frame", editor.grid.showCameraGuide, v => editor.grid.showCameraGuide = v);
            bpmToggle = UIFactory.Toggle(c, "BPM beat lines", editor.grid.showBpmGuide, v => editor.grid.showBpmGuide = v);

            UIFactory.SectionHeader(c, "Editor layer");
            var lr = UIFactory.Row(c, 28, 4);
            UIFactory.Button(lr, "◀", () => editor.SetEditorLayer(editor.currentEditorLayer - 1), 34, 26);
            layerLabel = UIFactory.Label(lr, "", 14, TextAnchor.MiddleCenter, UIFactory.TextColor, -1, 26);
            UIFactory.Button(lr, "▶", () => editor.SetEditorLayer(editor.currentEditorLayer + 1), 34, 26);
            allLayersToggle = UIFactory.Toggle(c, "Show all layers (\\)", editor.showAllLayers, v => editor.SetShowAllLayers(v));
            UIFactory.Button(c, "Move selection to this layer", () => editor.EditSelection(o => o.editorLayer = editor.currentEditorLayer), -1, 26, null, 12);

            UIFactory.SectionHeader(c, "Groups");
            UIFactory.Label(c, "Hide a group to get it out of the way, lock it so taps pass through it.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            groupsHost = UIFactory.Rect(c, "Groups");
            UIFactory.VLayout(groupsHost, 2, 0);
            UIFactory.Fitter(groupsHost, true, false);
            groupsEmpty = UIFactory.Label(groupsHost, "No groups in this level yet (Properties ▸ Groups).", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            var gr2 = UIFactory.Row(c, 26, 3);
            UIFactory.Button(gr2, "Show all", () =>
            {
                foreach (var g in new System.Collections.Generic.List<int>(editor.hiddenGroups)) editor.SetGroupHidden(g, false);
                RefreshGroups(true);
            }, -1, 24, null, 11);
            UIFactory.Button(gr2, "Unlock all", () =>
            {
                foreach (var g in new System.Collections.Generic.List<int>(editor.lockedGroups)) editor.SetGroupLocked(g, false);
                RefreshGroups(true);
            }, -1, 24, null, 11);

            UIFactory.SectionHeader(c, "Zoom");
            zoomLabel = UIFactory.Label(c, "", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            zoomSlider = UIFactory.Slider(c, Mathf.Log(EditorCamera.MinZoom), Mathf.Log(EditorCamera.MaxZoom), Mathf.Log(editor.editorCamera.zoom), v =>
            {
                if (suppress) return;
                editor.editorCamera.SetZoom(Mathf.Exp(v));
            });
            var zr = UIFactory.Row(c, 26, 3);
            UIFactory.Button(zr, "−", () => editor.editorCamera.SetZoom(editor.editorCamera.zoom / 1.25f), -1, 24);
            UIFactory.Button(zr, "100%", () => editor.editorCamera.SetZoom(1f), -1, 24, null, 12);
            UIFactory.Button(zr, "+", () => editor.editorCamera.SetZoom(editor.editorCamera.zoom * 1.25f), -1, 24);

            UIFactory.SectionHeader(c, "Navigate");
            posLabel = UIFactory.Label(c, "", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            posSlider = UIFactory.Slider(c, 0, 1, 0, v =>
            {
                if (suppress) return;
                float len = Mathf.Max(10f, editor.level.GetFinishX() + 10f);
                editor.editorCamera.Position = new Vector2(v * len, editor.editorCamera.Position.y);
            });
            var nr = UIFactory.Row(c, 26, 3);
            UIFactory.Button(nr, "⇤ Spawn", editor.GoToSpawn, -1, 24, null, 12);
            UIFactory.Button(nr, "End ⇥", () => editor.editorCamera.Position = new Vector2(editor.level.GetFinishX(), editor.level.settings.groundY + 4f), -1, 24, null, 12);
            UIFactory.Button(c, "Go to ground", () => editor.editorCamera.Position = new Vector2(editor.editorCamera.Position.x, editor.level.settings.groundY + 4f), -1, 24, null, 12);

            UIFactory.SectionHeader(c, "Interface");
            uiScaleLabel = UIFactory.Label(c, "", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            var ur = UIFactory.Row(c, 26, 3);
            UIFactory.Button(ur, "−", () => ui.SetUIScale(EditorUI.UIScale - 0.1f), -1, 24);
            UIFactory.Button(ur, "100%", () => ui.SetUIScale(1f), -1, 24, null, 12);
            UIFactory.Button(ur, "+", () => ui.SetUIScale(EditorUI.UIScale + 0.1f), -1, 24);
            layoutButton = UIFactory.Button(c, "", () =>
            {
                EditorUI.CyclePhoneLayout();
                Refresh();
                ui.Toast("Layout: " + EditorUI.PhoneLayoutName + " — applies when the editor is reopened", 4f);
            }, -1, 26, null, 12);
            UIFactory.Label(c, ui.IsPhone
                ? "Phone layout: View and Props are drawers, ▼ hides the dock, ⋯ holds save, files and settings. One finger places or selects, two fingers pan and zoom."
                : "Tip: set the Game view to Free Aspect so the interface fits the window. The phone layout is picked automatically on Android.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 60);

            UIFactory.SectionHeader(c, "Playtest marker");
            var mr = UIFactory.Row(c, 26, 3);
            UIFactory.Button(mr, "Set at camera", () => editor.SetPlaytestMarker(editor.editorCamera.Position), -1, 24, null, 11);
            UIFactory.Button(mr, "Clear", () => editor.SetPlaytestMarker(null), 60, 24, null, 11);
            UIFactory.Label(c, "The green rider shows where you spawn (x 0 on the ground, or the right-most Start Position object). The blue rider is the test marker: M sets it at the cursor, Shift+M clears it, Shift+P or ▶ Marker plays from it.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 96);

            editor.ViewOptionsChanged += Refresh;
            ui.UIScaleChanged += Refresh;
            editor.LevelChanged += RefreshQuest;
            editor.LevelChanged += () => RefreshGroups(false);
            Refresh();
            RefreshQuest();
            RefreshGroups(true);
        }

        /// <summary>Rebuilds the group rows only when the set of used groups changes (LevelChanged fires on every move).</summary>
        void RefreshGroups(bool force)
        {
            if (groupsHost == null) return;
            var used = editor.UsedGroups();
            var key = string.Join(",", used);
            if (!force && key == groupsKey) return;
            groupsKey = key;
            foreach (Transform child in groupsHost) if (child.gameObject != groupsEmpty.gameObject) Destroy(child.gameObject);
            groupsEmpty.gameObject.SetActive(used.Count == 0);
            int shown = 0;
            foreach (var g in used)
            {
                if (shown++ >= 24) break;
                int gid = g;
                var row = UIFactory.Row(groupsHost, 24, 4);
                UIFactory.Label(row, "Group " + gid, 12, TextAnchor.MiddleLeft, UIFactory.TextColor, 70, 24);
                UIFactory.Toggle(row, "Hide", editor.hiddenGroups.Contains(gid), v => editor.SetGroupHidden(gid, v), 24, 62);
                UIFactory.Toggle(row, "Lock", editor.lockedGroups.Contains(gid), v => editor.SetGroupLocked(gid, v), 24, 62);
                UIFactory.Button(row, "Sel", () => editor.SelectByGroup(gid), 40, 22, null, 11);
            }
            if (used.Count > 24) UIFactory.Label(groupsHost, "… and " + (used.Count - 24) + " more groups", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
        }

        void RefreshQuest()
        {
            if (nameInput == null) return;
            if (!nameInput.isFocused) nameInput.SetTextWithoutNotify(editor.level.name);
            string state = string.IsNullOrEmpty(editor.currentFilePath) ? "Not saved yet" : (editor.Dirty ? "Unsaved changes" : "Saved");
            savedLabel.text = state + " · P tests, Ctrl+S saves, Esc returns from a test";
            if (budgetLabel != null)
            {
                int n = editor.level.objects.Count;
                var parts = new System.Collections.Generic.List<string>();
                int k = 0;
                foreach (var kv in editor.CountByCategory())
                {
                    if (k++ >= 4) break;
                    parts.Add(kv.Key.ToLowerInvariant() + " " + kv.Value);
                }
                string verdict = n >= LevelEditor.BudgetHigh ? " — very heavy, expect slowdown on phones" : (n >= LevelEditor.BudgetWarn ? " — getting heavy for phones" : "");
                budgetLabel.text = n + " objects" + (parts.Count > 0 ? " (" + string.Join(", ", parts) + ")" : "") + verdict;
                budgetLabel.color = n >= LevelEditor.BudgetHigh ? new Color(1f, 0.4f, 0.35f) : (n >= LevelEditor.BudgetWarn ? new Color(1f, 0.75f, 0.3f) : UIFactory.TextDim);
            }
        }

        void Refresh()
        {
            if (snapToggle == null) return;
            if (uiScaleLabel != null) uiScaleLabel.text = string.Format("Interface size {0:0}%", EditorUI.UIScale * 100f);
            if (layoutButton != null) UIFactory.SetButtonLabel(layoutButton, "Layout: " + EditorUI.PhoneLayoutName);
            snapToggle.SetIsOnWithoutNotify(editor.snapToGrid);
            if (snapGuidesToggle != null) snapGuidesToggle.SetIsOnWithoutNotify(editor.snapGuides);
            gridToggle.SetIsOnWithoutNotify(editor.grid.showGrid);
            hitboxToggle.SetIsOnWithoutNotify(editor.showHitboxes);
            gizmoToggle.SetIsOnWithoutNotify(editor.GizmoVisible);
            beatToggle.SetIsOnWithoutNotify(editor.beatSnap);
            bpmToggle.SetIsOnWithoutNotify(editor.grid.showBpmGuide);
            int[] divs = { 1, 2, 4 };
            for (int i = 0; i < beatButtons.Length; i++) UIFactory.SetButtonActive(beatButtons[i], editor.beatDivision == divs[i]);
            allLayersToggle.SetIsOnWithoutNotify(editor.showAllLayers);
            for (int i = 0; i < gridSizes.Length; i++) UIFactory.SetButtonActive(gridButtons[i], Mathf.Approximately(gridSizes[i], editor.gridSize));
            layerLabel.text = "Layer " + editor.currentEditorLayer + (editor.showAllLayers ? " (all shown)" : "");
        }

        public void Tick()
        {
            if (zoomLabel == null) return;
            suppress = true;
            zoomLabel.text = string.Format("{0:0}%", editor.editorCamera.zoom * 100f);
            zoomSlider.SetValueWithoutNotify(Mathf.Log(editor.editorCamera.zoom));
            float len = Mathf.Max(10f, editor.level.GetFinishX() + 10f);
            posSlider.SetValueWithoutNotify(Mathf.Clamp01(editor.editorCamera.Position.x / len));
            posLabel.text = string.Format("x {0:0.#} / {1:0.#}", editor.editorCamera.Position.x, editor.level.GetFinishX());
            suppress = false;
        }
    }
}
