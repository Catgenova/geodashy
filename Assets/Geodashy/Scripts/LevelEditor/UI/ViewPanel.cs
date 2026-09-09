using Geodashy.Core;
using Geodashy.Rendering;
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
        RectTransform groupsHost, bookmarksHost;
        Text groupsEmpty, budgetLabel, bookmarksEmpty;
        string groupsKey = "", bookmarksKey = "";
        readonly System.Collections.Generic.HashSet<int> expandedGroups = new System.Collections.Generic.HashSet<int>();
        RectTransform colorStrip;
        Toggle traceToggle, songEndToggle;
        Text songEndLabel;
        int lastColorChannel = 1;
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
            UIFactory.Button(c, "♪ Sync check (pulse on the beat)", editor.StartSyncCheck, -1, 28, UIFactory.Good, 12);
            UIFactory.Label(c, "Plays from the start with a flash on every beat; afterwards each jump shows as a dot: green on the beat, amber close, red off.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 32);
            var diffNames = new[] { "Training", "Checkpoints", "Champion" };
            DropdownField.Create(c, "Test as", diffNames, (int)editor.TestDifficulty, i => editor.TestDifficulty = (Difficulty)i, 60, 26);
            var qr2 = UIFactory.Row(c, 30, 4);
            UIFactory.Button(qr2, "Save", ui.SaveWithPrompt, -1, 28, UIFactory.ButtonActive, 13);
            UIFactory.Button(qr2, "Save as…", ui.PromptSaveAs, -1, 28, null, 13);
            UIFactory.Button(qr2, "Files", ui.OpenFileDialog, -1, 28, null, 13);
            UIFactory.Button(qr2, "History", ui.OpenHistory, -1, 28, null, 13);
            UIFactory.Button(c, "Check quest (lint)", () => LintDialog.Open(ui, editor), -1, 26, null, 12);
            var ar = UIFactory.Row(c, 26, 3);
            UIFactory.Button(ar, "Auto-decorate…", () => AutoDecorateDialog.Open(ui, editor), -1, 24, null, 12);
            UIFactory.Button(ar, "Editor tour", () => EditorTour.Begin(ui), -1, 24, null, 12);
            songEndToggle = UIFactory.Toggle(c, "Song end flag", editor.showSongEnd, v =>
            {
                editor.showSongEnd = v;
                PlayerPrefs.SetInt(LevelEditor.SongEndPref, v ? 1 : 0);
            });
            songEndLabel = UIFactory.Label(c, "", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 32);
            UIFactory.Button(c, "Place banner at song end", editor.PlaceSongEndBanner, -1, 26, null, 12);
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
            traceToggle = UIFactory.Toggle(c, "Show last playtest path", editor.showRunTrace, v =>
            {
                editor.showRunTrace = v;
                PlayerPrefs.SetInt(LevelEditor.RunTracePref, v ? 1 : 0);
            });
            guideToggle = UIFactory.Toggle(c, "Camera frame", editor.grid.showCameraGuide, v => editor.grid.showCameraGuide = v);
            bpmToggle = UIFactory.Toggle(c, "BPM beat lines", editor.grid.showBpmGuide, v => editor.grid.showBpmGuide = v);

            UIFactory.SectionHeader(c, "Editor layer");
            var lr = UIFactory.Row(c, 28, 4);
            UIFactory.Button(lr, "◀", () => editor.SetEditorLayer(editor.currentEditorLayer - 1), 34, 26);
            layerLabel = UIFactory.Label(lr, "", 14, TextAnchor.MiddleCenter, UIFactory.TextColor, -1, 26);
            UIFactory.Button(lr, "▶", () => editor.SetEditorLayer(editor.currentEditorLayer + 1), 34, 26);
            allLayersToggle = UIFactory.Toggle(c, "Show all layers (\\)", editor.showAllLayers, v => editor.SetShowAllLayers(v));
            UIFactory.Button(c, "Move selection to this layer", () => editor.EditSelection(o => o.editorLayer = editor.currentEditorLayer), -1, 26, null, 12);

            UIFactory.SectionHeader(c, "Colour channels");
            UIFactory.Label(c, "Click a swatch to select every object using it; Recolour edits the last clicked channel.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            colorStrip = UIFactory.Rect(c, "Colours");
            UIFactory.Grid(colorStrip, new Vector2(22, 22), new Vector2(3, 3), 0);
            UIFactory.Layout(colorStrip.gameObject, -1, 50, 1);
            var cr = UIFactory.Row(c, 26, 3);
            UIFactory.Button(cr, "Recolour…", () =>
            {
                var ch = editor.level.GetOrCreateColorChannel(lastColorChannel);
                ui.ShowColorPicker(colorStrip, ch.color, col =>
                {
                    ch.color = new Color(col.r, col.g, col.b, 1f);
                    ch.opacity = col.a;
                    editor.MarkDirty();
                    editor.RefreshAllViews();
                    RefreshColorStrip();
                });
            }, -1, 24, null, 11);
            UIFactory.Button(cr, "Apply to selection", () => editor.EditSelection(o => o.baseColor = lastColorChannel, true, "Recolour"), -1, 24, null, 11);
            editor.LevelChanged += RefreshColorStrip;

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

            UIFactory.SectionHeader(c, "Bookmarks");
            UIFactory.Label(c, "Named spots in the quest: Go jumps the camera there and sets the test marker, ▶ plays from it.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            bookmarksHost = UIFactory.Rect(c, "Bookmarks");
            UIFactory.VLayout(bookmarksHost, 2, 0);
            UIFactory.Fitter(bookmarksHost, true, false);
            bookmarksEmpty = UIFactory.Label(bookmarksHost, "No bookmarks yet.", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            UIFactory.Button(c, "+ Bookmark at camera", () =>
            {
                ui.Prompt("New bookmark", "Name for x " + editor.editorCamera.Position.x.ToString("0.#") + ":", "Section " + ((editor.level.bookmarks != null ? editor.level.bookmarks.Count : 0) + 1), n =>
                {
                    editor.AddBookmark(n, new Vector2(Mathf.Max(0f, editor.editorCamera.Position.x - editor.editorCamera.HalfWidth * 0.5f), editor.level.settings.groundY + 0.5f));
                    RefreshBookmarks(true);
                });
            }, -1, 26, null, 12);

            editor.ViewOptionsChanged += Refresh;
            ui.UIScaleChanged += Refresh;
            editor.LevelChanged += RefreshQuest;
            editor.LevelChanged += () => RefreshGroups(false);
            editor.LevelChanged += () => RefreshBookmarks(false);
            editor.LevelChanged += () => { if (songEndLabel != null) songEndLabel.text = SongEndText(); };
            Refresh();
            RefreshQuest();
            RefreshGroups(true);
            RefreshBookmarks(true);
            RefreshColorStrip();
        }

        void RefreshBookmarks(bool force)
        {
            if (bookmarksHost == null) return;
            var bms = editor.level.bookmarks;
            var sb = new System.Text.StringBuilder();
            if (bms != null) foreach (var b in bms) sb.Append(b.name).Append('@').Append(b.x.ToString("0.#")).Append(';');
            var key = sb.ToString();
            if (!force && key == bookmarksKey) return;
            bookmarksKey = key;
            foreach (Transform child in bookmarksHost) if (child.gameObject != bookmarksEmpty.gameObject) Destroy(child.gameObject);
            int n = bms != null ? bms.Count : 0;
            bookmarksEmpty.gameObject.SetActive(n == 0);
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var b = bms[i];
                var row = UIFactory.Row(bookmarksHost, 24, 4);
                var l = UIFactory.Label(row, b.name + "  (x " + b.x.ToString("0.#") + ")", 12, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 24);
                l.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.Button(row, "Go", () => editor.GoToBookmark(idx, false), 34, 22, null, 11);
                UIFactory.Button(row, "▶", () => editor.GoToBookmark(idx, true), 30, 22, UIFactory.Good, 11);
                UIFactory.Button(row, "✕", () =>
                {
                    editor.RemoveBookmark(idx);
                    RefreshBookmarks(true);
                }, 26, 22, UIFactory.Danger, 11);
            }
        }

        string colorKey = "";
        void RefreshColorStrip()
        {
            if (colorStrip == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= 20; i++) sb.Append(ColorUtility.ToHtmlStringRGBA(editor.level.ResolveColor(i, Color.white)));
            var key = sb.ToString() + lastColorChannel;
            if (key == colorKey) return;
            colorKey = key;
            foreach (Transform child in colorStrip) Destroy(child.gameObject);
            for (int i = 1; i <= 20; i++)
            {
                int ch = i;
                var sw = UIFactory.Swatch(colorStrip, editor.level.ResolveColor(i, Color.white), 22);
                var b = sw.gameObject.AddComponent<Button>();
                b.targetGraphic = sw;
                b.onClick.AddListener(() =>
                {
                    lastColorChannel = ch;
                    editor.SelectByColorChannel(ch);
                    RefreshColorStrip();
                });
                if (ch == lastColorChannel)
                {
                    var ring = UIFactory.Rect(sw.transform, "Ring");
                    var img = ring.gameObject.AddComponent<Image>();
                    img.sprite = PlaceholderSpriteFactory.Outline();
                    img.type = Image.Type.Sliced;
                    img.color = UIFactory.Accent;
                    img.raycastTarget = false;
                    UIFactory.Stretch(ring);
                }
            }
        }

        string SongEndText()
        {
            var s = editor.level.settings;
            if (string.IsNullOrEmpty(s.songFile) && string.IsNullOrEmpty(s.songId)) return "No song set: pick one in Level Settings to see where it ends.";
            if (editor.SongLength <= 0f) return "Reading the song length…";
            if (!editor.SongEndX.HasValue) return "The song offset is past the end of the song.";
            float endX = editor.SongEndX.Value;
            float finish = editor.level.GetFinishX();
            float remaining = editor.SongLength - s.songOffset;
            string cmp = finish <= 0.5f ? "" : (endX > finish + 0.5f ? " — " + (endX - finish).ToString("0.#") + " blocks past the finish" : (endX < finish - 0.5f ? " — " + (finish - endX).ToString("0.#") + " blocks before the finish" : " — right at the finish"));
            return "Song ends at x " + endX.ToString("0.#") + " (" + remaining.ToString("0.#") + "s of music, speed portals included)" + cmp;
        }

        /// <summary>Rebuilds the group rows only when the set of used groups changes (LevelChanged fires on every move).</summary>
        void RefreshGroups(bool force)
        {
            if (groupsHost == null) return;
            var used = editor.UsedGroups();
            var key = string.Join(",", used) + "|" + (editor.level.groupInfos != null ? editor.level.groupInfos.Count : 0) + "|" + expandedGroups.Count;
            if (!force && key == groupsKey) return;
            groupsKey = key;
            foreach (Transform child in groupsHost) if (child.gameObject != groupsEmpty.gameObject) Destroy(child.gameObject);
            groupsEmpty.gameObject.SetActive(used.Count == 0);
            int shown = 0;
            foreach (var g in used)
            {
                if (shown++ >= 24) break;
                int gid = g;
                var info = editor.level.GetGroupInfo(gid, false);
                var tint = editor.GroupTint(new LevelObject { groups = new System.Collections.Generic.List<int> { gid } });
                var row = UIFactory.Row(groupsHost, 24, 4);
                var expand = UIFactory.Button(row, expandedGroups.Contains(gid) ? "▾" : "▸", () =>
                {
                    if (!expandedGroups.Remove(gid)) expandedGroups.Add(gid);
                    RefreshGroups(true);
                }, 20, 22, null, 11);
                var nameLabel = UIFactory.Label(row, editor.GroupLabel(gid), 12, TextAnchor.MiddleLeft, tint.a > 0f ? tint : UIFactory.TextColor, -1, 24);
                nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.Toggle(row, "Hide", editor.hiddenGroups.Contains(gid), v => editor.SetGroupHidden(gid, v), 24, 58);
                UIFactory.Toggle(row, "Lock", editor.lockedGroups.Contains(gid), v => editor.SetGroupLocked(gid, v), 24, 58);
                UIFactory.Button(row, "Sel", () => editor.SelectByGroup(gid), 34, 22, null, 11);
                var more = UIFactory.Button(row, "⋯", null, 24, 22, null, 12);
                more.onClick.AddListener(() =>
                {
                    var pos = RectTransformUtility.WorldToScreenPoint(null, more.transform.position);
                    ui.ShowMenuAt(pos, new[] { "Rename group…", "Tint colour…", "Clear tint", "Select group", "Move group to this layer" }, i =>
                    {
                        switch (i)
                        {
                            case 0:
                                ui.Prompt("Rename group " + gid, "A name for this group (shown in Groups and Properties):", info != null ? info.name : "", n =>
                                {
                                    editor.SetGroupName(gid, n);
                                    RefreshGroups(true);
                                });
                                break;
                            case 1:
                                ui.ShowColorPicker(groupsHost, tint.a > 0f ? tint : UIFactory.Accent, col =>
                                {
                                    editor.SetGroupTint(gid, new Color(col.r, col.g, col.b, 1f));
                                    RefreshGroups(true);
                                });
                                break;
                            case 2:
                                editor.SetGroupTint(gid, null);
                                RefreshGroups(true);
                                break;
                            case 3: editor.SelectByGroup(gid); break;
                            case 4:
                                editor.SelectByGroup(gid);
                                editor.EditSelection(o => o.editorLayer = editor.currentEditorLayer, true, "Move group to layer");
                                break;
                        }
                    });
                });
                if (expandedGroups.Contains(gid))
                {
                    var parts = new System.Collections.Generic.List<string>();
                    int k = 0;
                    foreach (var kv in editor.GroupContents(gid))
                    {
                        if (k++ >= 6) { parts.Add("…"); break; }
                        parts.Add(kv.Value + " × " + kv.Key);
                    }
                    var contents = UIFactory.Label(groupsHost, "    " + (parts.Count > 0 ? string.Join(", ", parts) : "empty"), 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
                    contents.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
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
            if (songEndToggle != null) songEndToggle.SetIsOnWithoutNotify(editor.showSongEnd);
            if (songEndLabel != null) songEndLabel.text = SongEndText();
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
