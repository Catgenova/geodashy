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
        Button viewBtn, propsBtn, dockBtn, menuBtn;
        InputField nameInput;
        Text info;

        public static TopBar Create(EditorUI ui, RectTransform parent)
        {
            var tb = parent.gameObject.AddComponent<TopBar>();
            tb.ui = ui;
            tb.editor = ui.editor;
            if (ui.IsPhone) tb.BuildPhone(parent);
            else tb.Build(parent);
            return tb;
        }

        void Build(RectTransform parent)
        {
            UIFactory.HLayout(parent, 6, 8, false, TextAnchor.MiddleLeft);

            buildBtn = UIFactory.IconButton(parent, "build", "Build", () => editor.SetMode(EditorMode.Build), 92, 36, null, 14, "Build mode (1): place the brush object");
            editBtn = UIFactory.IconButton(parent, "edit", "Edit", () => editor.SetMode(EditorMode.Edit), 84, 36, null, 14, "Edit mode (2): select, move and change objects");
            deleteBtn = UIFactory.IconButton(parent, "delete", "Delete", () => editor.SetMode(EditorMode.Delete), 96, 36, null, 14, "Delete mode (3): remove objects");
            UIFactory.Spacer(parent, 36, 10);
            undoBtn = UIFactory.IconButton(parent, "undo", null, editor.Undo, 40, 36, null, 14, "Undo (Ctrl+Z)");
            redoBtn = UIFactory.IconButton(parent, "redo", null, editor.Redo, 40, 36, null, 14, "Redo (Ctrl+Y)");
            UIFactory.Spacer(parent, 36, 10);
            UIFactory.IconButton(parent, "play", "Play", () => editor.StartPlaytest(false), 84, 36, UIFactory.Good, 14, "Play from the start (P)");
            UIFactory.IconButton(parent, "marker", "Marker", () => editor.StartPlaytest(true), 96, 36, UIFactory.Good, 14, "Play from the test marker (Shift+P)");
            UIFactory.IconButton(parent, "train", "Train", () => editor.StartPlaytest(false, true), 88, 36, UIFactory.Good, 14, "Training run: raise your own waystones (Alt+P)");
            UIFactory.IconButton(parent, "marker", null, () => editor.SetPlaytestMarker(editor.editorCamera.Position), 40, 36, null, 14, "Set the test marker at the camera (M at the cursor)");
            UIFactory.Spacer(parent, 36, 10);
            UIFactory.IconButton(parent, "save", null, ui.SaveWithPrompt, 40, 36, null, 14, "Save (Ctrl+S)");
            UIFactory.IconButton(parent, "files", null, ui.OpenFileDialog, 40, 36, null, 14, "Files: load, share, packs (Ctrl+O)");
            UIFactory.IconButton(parent, "settings", null, ui.OpenSettings, 40, 36, null, 14, "Level settings: song, tempo, backgrounds, colours");
            UIFactory.IconButton(parent, "history", null, ui.OpenHistory, 40, 36, null, 14, "Undo history");
            UIFactory.IconButton(parent, "help", null, ui.OpenHelp, 40, 36, null, 14, "Help and shortcuts (F1)");
            UIFactory.IconButton(parent, "menu", null, ui.ReturnToMenu, 40, 36, null, 14, "Back to the main menu");
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

        /// <summary>
        /// Compact touch bar: mode tabs, undo/redo, play, drawer toggles and a ⋯ menu holding everything that
        /// does not need to be one tap away. Buttons are 44 units tall so they stay finger-sized at phone scale.
        /// </summary>
        void BuildPhone(RectTransform parent)
        {
            UIFactory.HLayout(parent, 4, 4, false, TextAnchor.MiddleLeft);
            const float h = 44f;

            buildBtn = UIFactory.IconButton(parent, "build", null, () => editor.SetMode(EditorMode.Build), 48, h, null, 13, "Build");
            editBtn = UIFactory.IconButton(parent, "edit", null, () => editor.SetMode(EditorMode.Edit), 48, h, null, 13, "Edit");
            deleteBtn = UIFactory.IconButton(parent, "delete", null, () => editor.SetMode(EditorMode.Delete), 48, h, null, 13, "Delete");
            UIFactory.Spacer(parent, h, 4);
            undoBtn = UIFactory.IconButton(parent, "undo", null, editor.Undo, 44, h, null, 18, "Undo");
            redoBtn = UIFactory.IconButton(parent, "redo", null, editor.Redo, 44, h, null, 18, "Redo");
            UIFactory.Spacer(parent, h, 4);
            UIFactory.IconButton(parent, "play", "Play", () => editor.StartPlaytest(false), 80, h, UIFactory.Good, 13, "Play from the start");
            UIFactory.IconButton(parent, "marker", null, () => editor.StartPlaytest(true), 46, h, UIFactory.Good, 13, "Play from the marker");
            UIFactory.Spacer(parent, h, 4);
            viewBtn = UIFactory.IconButton(parent, "view", null, ui.ToggleViewDrawer, 46, h, null, 13, "View drawer");
            propsBtn = UIFactory.IconButton(parent, "props", null, ui.TogglePropsDrawer, 46, h, null, 13, "Properties drawer");
            dockBtn = UIFactory.IconButton(parent, "dock", null, ui.ToggleDock, 44, h, null, 14, "Fold the dock away");
            menuBtn = UIFactory.IconButton(parent, "more", null, ShowMenuPopup, 44, h, UIFactory.ButtonActive, 20, "More: save, files, settings, tools");
            info = UIFactory.Label(parent, "", 12, TextAnchor.MiddleRight, UIFactory.TextDim);
            UIFactory.Layout(info.gameObject, -1, h, 1);
            RefreshDrawerButtons();
        }

        void ShowMenuPopup()
        {
            string[] options =
            {
                "Save", "Save as…", "Files…", "Level settings…", "▶ Training test", "⚑ Marker at camera", "Clear marker", "Rename quest…", "Help", "◀ Main menu", "History…", "Save selection as stamp…",
                "♪ Sync check test", "Auto-decorate…", "Save brush as preset…", "+ Bookmark here", "Editor tour"
            };
            ui.ShowDropdown(menuBtn.GetComponent<RectTransform>(), options, -1, i =>
            {
                switch (i)
                {
                    case 0: ui.SaveWithPrompt(); break;
                    case 1: ui.PromptSaveAs(); break;
                    case 2: ui.OpenFileDialog(); break;
                    case 3: ui.OpenSettings(); break;
                    case 4: editor.StartPlaytest(false, true); break;
                    case 5: editor.SetPlaytestMarker(editor.editorCamera.Position); break;
                    case 6: editor.SetPlaytestMarker(null); break;
                    case 7:
                        ui.Prompt("Rename quest", "Quest name:", editor.level.name, name =>
                        {
                            if (string.IsNullOrWhiteSpace(name)) return;
                            editor.level.name = name.Trim();
                            editor.MarkDirty();
                        });
                        break;
                    case 8: ui.OpenHelp(); break;
                    case 9: ui.ReturnToMenu(); break;
                    case 10: ui.OpenHistory(); break;
                    case 11: ui.PromptSaveStamp(); break;
                    case 12: editor.StartSyncCheck(); break;
                    case 13: AutoDecorateDialog.Open(ui, editor); break;
                    case 14: ui.palettePanel?.PromptSavePreset(); break;
                    case 15:
                        ui.Prompt("New bookmark", "Name for x " + editor.editorCamera.Position.x.ToString("0.#") + ":", "Section " + ((editor.level.bookmarks != null ? editor.level.bookmarks.Count : 0) + 1),
                            n => editor.AddBookmark(n, new Vector2(Mathf.Max(0f, editor.editorCamera.Position.x - editor.editorCamera.HalfWidth * 0.5f), editor.level.settings.groundY + 0.5f)));
                        break;
                    case 16: EditorTour.Begin(ui); break;
                }
            });
        }

        public void RefreshModeButtons()
        {
            UIFactory.SetButtonActive(buildBtn, editor.Mode == EditorMode.Build);
            UIFactory.SetButtonActive(editBtn, editor.Mode == EditorMode.Edit);
            UIFactory.SetButtonActive(deleteBtn, editor.Mode == EditorMode.Delete);
        }

        /// <summary>Phone layout only: reflects which drawers are open and whether Props has anything to show.</summary>
        public void RefreshDrawerButtons()
        {
            if (viewBtn == null) return;
            UIFactory.SetButtonActive(viewBtn, ui.ViewDrawerOpen);
            UIFactory.SetButtonActive(propsBtn, ui.PropsDrawerOpen || editor.selection.Count > 0);
            var glyph = dockBtn.transform.Find("Glyph");
            if (glyph != null) glyph.localScale = new Vector3(1f, ui.DockCollapsed ? -1f : 1f, 1f);
        }

        public void Tick()
        {
            if (info == null || editor.level == null) return;
            var c = editor.CursorSnapped;
            if (ui.IsPhone)
            {
                info.text = string.Format("{0}{1} obj · {2} sel · {3:0}% · L{4}", editor.Dirty ? "* " : "", editor.level.objects.Count, editor.selection.Count,
                    editor.editorCamera.zoom * 100f, editor.showAllLayers ? "all" : editor.currentEditorLayer.ToString());
            }
            else
            {
                float len = editor.level.GetFinishX();
                float secs = len / MountCatalog.Speed(editor.level.settings.startSpeed);
                info.text = string.Format("{0}{1}  |  {2} objects  |  {3} selected  |  x {4:0.##}  y {5:0.##}  |  zoom {6:0}%  |  layer {7}  |  ~{8:0}s",
                    editor.Dirty ? "* " : "", "", editor.level.objects.Count, editor.selection.Count, c.x, c.y, editor.editorCamera.zoom * 100f,
                    editor.showAllLayers ? "all" : editor.currentEditorLayer.ToString(), secs);
            }
            undoBtn.interactable = editor.undo.CanUndo;
            redoBtn.interactable = editor.undo.CanRedo;
            // object budget: the info text turns amber, then red, as the level grows heavy for phones
            int n = editor.level.objects.Count;
            info.color = n >= LevelEditor.BudgetHigh ? new Color(1f, 0.4f, 0.35f) : (n >= LevelEditor.BudgetWarn ? new Color(1f, 0.75f, 0.3f) : UIFactory.TextDim);
        }
    }
}
