using Geodashy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Mode tabs, undo/redo, playtest, file and settings buttons plus the status readout.</summary>
    public class TopBar : MonoBehaviour
    {
        EditorUI ui;
        LevelEditor editor;
        Button buildBtn, editBtn, deleteBtn;
        Button undoBtn, redoBtn;
        InputField nameInput;
        Text info;

        public static TopBar Create(EditorUI ui, RectTransform parent)
        {
            var tb = parent.gameObject.AddComponent<TopBar>();
            tb.ui = ui;
            tb.editor = ui.editor;
            tb.Build(parent);
            return tb;
        }

        void Build(RectTransform parent)
        {
            UIFactory.HLayout(parent, 6, 8, false, TextAnchor.MiddleLeft);

            buildBtn = UIFactory.Button(parent, "Build (1)", () => editor.SetMode(EditorMode.Build), 92, 36);
            editBtn = UIFactory.Button(parent, "Edit (2)", () => editor.SetMode(EditorMode.Edit), 92, 36);
            deleteBtn = UIFactory.Button(parent, "Delete (3)", () => editor.SetMode(EditorMode.Delete), 92, 36);
            UIFactory.Spacer(parent, 36, 10);
            undoBtn = UIFactory.Button(parent, "↶ Undo", editor.Undo, 84, 36);
            redoBtn = UIFactory.Button(parent, "↷ Redo", editor.Redo, 84, 36);
            UIFactory.Spacer(parent, 36, 10);
            UIFactory.Button(parent, "▶ Play", () => editor.StartPlaytest(false), 84, 36, UIFactory.Good);
            UIFactory.Button(parent, "▶ Marker", () => editor.StartPlaytest(true), 92, 36, UIFactory.Good);
            UIFactory.Button(parent, "▶ Squire", () => editor.StartPlaytest(false, true), 96, 36, UIFactory.Good);
            UIFactory.Button(parent, "⚑ Set", () => editor.SetPlaytestMarker(editor.editorCamera.Position), 64, 36);
            UIFactory.Spacer(parent, 36, 10);
            UIFactory.Button(parent, "Save", ui.SaveWithPrompt, 70, 36);
            UIFactory.Button(parent, "Files", ui.OpenFileDialog, 70, 36);
            UIFactory.Button(parent, "Settings", ui.OpenSettings, 84, 36);
            UIFactory.Button(parent, "Help (F1)", ui.OpenHelp, 84, 36);
            UIFactory.Button(parent, "Menu", ui.ReturnToMenu, 64, 36);
            UIFactory.Spacer(parent, 36, 10);
            nameInput = UIFactory.Input(parent, "Level name", editor.level.name, s =>
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                editor.level.name = s;
                editor.MarkDirty();
            }, 220, 32, InputField.ContentType.Standard, 15);
            UIFactory.Layout(nameInput.gameObject, 220, 32);
            info = UIFactory.Label(parent, "", 13, TextAnchor.MiddleRight, UIFactory.TextDim);
            UIFactory.Layout(info.gameObject, -1, 36, 1);

            editor.LevelChanged += () =>
            {
                if (nameInput != null && !nameInput.isFocused) nameInput.SetTextWithoutNotify(editor.level.name);
            };
        }

        public void RefreshModeButtons()
        {
            UIFactory.SetButtonActive(buildBtn, editor.Mode == EditorMode.Build);
            UIFactory.SetButtonActive(editBtn, editor.Mode == EditorMode.Edit);
            UIFactory.SetButtonActive(deleteBtn, editor.Mode == EditorMode.Delete);
        }

        public void Tick()
        {
            if (info == null || editor.level == null) return;
            var c = editor.CursorSnapped;
            float len = editor.level.GetFinishX();
            float secs = len / MountCatalog.Speed(editor.level.settings.startSpeed);
            info.text = string.Format("{0}{1}  |  {2} objects  |  {3} selected  |  x {4:0.##}  y {5:0.##}  |  zoom {6:0}%  |  layer {7}  |  ~{8:0}s",
                editor.Dirty ? "* " : "", "", editor.level.objects.Count, editor.selection.Count, c.x, c.y, editor.editorCamera.zoom * 100f,
                editor.showAllLayers ? "all" : editor.currentEditorLayer.ToString(), secs);
            undoBtn.interactable = editor.undo.CanUndo;
            redoBtn.interactable = editor.undo.CanRedo;
        }
    }
}
