using System;
using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Level settings: metadata, start state, backgrounds, ground, colours, music.</summary>
    public static class LevelSettingsDialog
    {
        public static void Open(EditorUI ui, LevelEditor editor)
        {
            var c = ui.OpenModal("Level Settings", 760, 900, true);
            var s = editor.level.settings;
            var level = editor.level;

            void Changed()
            {
                editor.RecordUndo();
                editor.ApplySettings();
                editor.MarkDirty();
            }

            UIFactory.SectionHeader(c, "Quest");
            TextField.Create(c, "Name", level.name, v =>
            {
                level.name = v;
                editor.MarkDirty();
            }, 140);
            TextField.Create(c, "Author", level.author, v =>
            {
                level.author = v;
                editor.MarkDirty();
            }, 140);
            TextField.Create(c, "Description", level.description, v =>
            {
                level.description = v;
                editor.MarkDirty();
            }, 140);

            NumberField.Create(c, "Campaign order", level.campaignOrder, 1, 0, 9999, v =>
            {
                level.campaignOrder = Mathf.RoundToInt(v);
                editor.MarkDirty();
            }, true, 140);
            UIFactory.Label(c, "Sort position in the level select once the map ships as a campaign map (lower comes first).", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 18);
            DropdownField.Create(c, "Difficulty rating", LevelRating.Tags, Mathf.Max(0, System.Array.IndexOf(LevelRating.Tags, s.difficultyTag)), i =>
            {
                s.difficultyTag = LevelRating.Tags[i];
                editor.MarkDirty();
            }, 140, 28, LevelRating.TagNames);
            UIFactory.Label(c, "Shown in the level select next to the length (" + LevelRating.LengthName(level.GetFinishX() / MountCatalog.Speed(s.startSpeed)) + " for this quest) and used by its filters.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            UIFactory.Button(c, "Reset play stats (deaths, personal best)", () =>
            {
                ui.Confirm("Reset play stats?", "Death markers and the personal best for this quest will be cleared.", () =>
                {
                    LevelStatsStorage.Reset(level.id);
                    ui.Toast("Play stats reset");
                }, "Reset");
            }, -1, 28, null, 12);

            UIFactory.SectionHeader(c, "Start");
            var mountIds = MountCatalog.Ids;
            var mountNames = new string[mountIds.Length];
            for (int i = 0; i < mountIds.Length; i++) mountNames[i] = MountCatalog.Get(mountIds[i]).name + " — " + MountCatalog.Get(mountIds[i]).control;
            DropdownField.Create(c, "Mount", mountIds, Mathf.Max(0, Array.IndexOf(mountIds, s.startMount)), i =>
            {
                s.startMount = mountIds[i];
                Changed();
            }, 140, 28, mountNames);
            DropdownField.Create(c, "Speed", MountCatalog.SpeedLabels, Mathf.Clamp(s.startSpeed, 0, 4), i =>
            {
                s.startSpeed = i;
                Changed();
            }, 140);
            var r1 = UIFactory.Row(c, 26, 12);
            BoolField.Create(r1, "Gravity flipped", s.startGravityFlipped, v =>
            {
                s.startGravityFlipped = v;
                Changed();
            });
            BoolField.Create(r1, "Mini", s.startMini, v =>
            {
                s.startMini = v;
                Changed();
            });
            BoolField.Create(r1, "Mirror", s.startMirror, v =>
            {
                s.startMirror = v;
                Changed();
            });
            NumberField.Create(c, "Ceiling height", s.ceilingHeight, 1, 4, 60, v =>
            {
                s.ceilingHeight = v;
                Changed();
            }, false, 140);
            NumberField.Create(c, "Finish padding", s.finishPadding, 1, 0, 100, v =>
            {
                s.finishPadding = v;
                Changed();
            }, false, 140);

            UIFactory.SectionHeader(c, "Background (3 parallax layers)");
            LevelSerializer.MigrateParallax(s);
            var themeIds = ThemeCatalog.BackgroundIds;
            var themeNames = new string[themeIds.Length];
            for (int i = 0; i < themeIds.Length; i++) themeNames[i] = ThemeCatalog.GetBackground(themeIds[i]).name;
            ColorField bgColorField = null;
            var layerIds = new List<string> { "" };
            layerIds.AddRange(ThemeCatalog.LayerIds);
            var layerNames = new List<string> { "(theme default)" };
            foreach (var id in ThemeCatalog.LayerIds) layerNames.Add(ThemeCatalog.GetLayer(id).name);
            var layerArtFields = new DropdownField[3];
            var layerSpeedFields = new NumberField[3];
            var layerYFields = new NumberField[3];
            var layerScaleFields = new NumberField[3];
            var layerTintFields = new ColorField[3];
            var layerVisibleFields = new BoolField[3];
            var layerFlipFields = new BoolField[3];
            DropdownField.Create(c, "Theme preset", themeIds, Mathf.Max(0, Array.IndexOf(themeIds, s.backgroundTheme)), i =>
            {
                s.backgroundTheme = themeIds[i];
                s.ResetLayersToTheme();
                s.backgroundColor = ThemeCatalog.GetBackground(s.backgroundTheme).skyBottom;
                bgColorField?.Set(s.backgroundColor);
                for (int k = 0; k < 3; k++)
                {
                    var l = s.Layer(k);
                    layerArtFields[k]?.Set(0);
                    layerSpeedFields[k]?.Set(l.parallax);
                    layerYFields[k]?.Set(l.yOffset);
                    layerScaleFields[k]?.Set(l.scale);
                    layerTintFields[k]?.Set(l.tint);
                    layerVisibleFields[k]?.Set(l.visible);
                    layerFlipFields[k]?.Set(l.flipX);
                }
                Changed();
            }, 140, 28, themeNames);
            UIFactory.Label(c, "Each layer scrolls at a fraction of the camera speed: 0 sticks to the camera, 1 moves with the world. Far layers should be slow, near layers fast.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            string[] layerTitles = { "Far layer", "Mid layer", "Near layer" };
            for (int k = 0; k < 3; k++)
            {
                int idx = k;
                var l = s.Layer(k);
                UIFactory.Label(c, layerTitles[k], 13, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 22, true);
                layerArtFields[k] = DropdownField.Create(c, "Art", layerIds.ToArray(), Mathf.Max(0, layerIds.IndexOf(l.layerId)), i =>
                {
                    s.Layer(idx).layerId = layerIds[i];
                    Changed();
                }, 140, 28, layerNames.ToArray());
                layerSpeedFields[k] = NumberField.Create(c, "Scroll speed", l.parallax, 0.05f, -1f, 2f, v =>
                {
                    s.Layer(idx).parallax = v;
                    Changed();
                }, false, 140);
                layerYFields[k] = NumberField.Create(c, "Height offset", l.yOffset, 0.5f, -30f, 60f, v =>
                {
                    s.Layer(idx).yOffset = v;
                    Changed();
                }, false, 140);
                layerScaleFields[k] = NumberField.Create(c, "Size", l.scale, 0.1f, 0.2f, 5f, v =>
                {
                    s.Layer(idx).scale = v;
                    Changed();
                }, false, 140);
                layerTintFields[k] = ColorField.Create(c, "Tint", l.tint, v =>
                {
                    s.Layer(idx).tint = v;
                    Changed();
                }, 140);
                var lr = UIFactory.Row(c, 26, 12);
                layerVisibleFields[k] = BoolField.Create(lr, "Visible", l.visible, v =>
                {
                    s.Layer(idx).visible = v;
                    Changed();
                });
                layerFlipFields[k] = BoolField.Create(lr, "Mirror horizontally", l.flipX, v =>
                {
                    s.Layer(idx).flipX = v;
                    Changed();
                });
            }

            UIFactory.SectionHeader(c, "Ground");
            var groundIds = ThemeCatalog.GroundIds;
            var groundNames = new string[groundIds.Length];
            for (int i = 0; i < groundIds.Length; i++) groundNames[i] = ThemeCatalog.GetGround(groundIds[i]).name;
            DropdownField.Create(c, "Ground theme", groundIds, Mathf.Max(0, Array.IndexOf(groundIds, s.groundTheme)), i =>
            {
                s.groundTheme = groundIds[i];
                Changed();
            }, 140, 28, groundNames);
            BoolField.Create(c, "Automatic scenery along the ground (torches, banners, trees…)", s.groundProps, v =>
            {
                s.groundProps = v;
                Changed();
            });

            UIFactory.SectionHeader(c, "Colours");
            bgColorField = ColorField.Create(c, "Background", s.backgroundColor, v =>
            {
                s.backgroundColor = v;
                Changed();
            }, 140);
            ColorField.Create(c, "Ground", s.groundColor, v =>
            {
                s.groundColor = v;
                Changed();
            }, 140);
            ColorField.Create(c, "Line", s.lineColor, v =>
            {
                s.lineColor = v;
                Changed();
            }, 140);
            ColorField.Create(c, "Object", s.objectColor, v =>
            {
                s.objectColor = v;
                Changed();
            }, 140);
            level.EnsureDefaultColors();
            for (int ch = 1; ch <= 10; ch++)
            {
                var channel = level.GetOrCreateColorChannel(ch);
                ColorField.Create(c, "Channel " + ch, channel.color, v =>
                {
                    channel.color = v;
                    Changed();
                }, 140);
            }
            var addRow = UIFactory.Row(c, 28, 4);
            var chInput = UIFactory.Input(addRow, "channel #", "11", null, 80, 26, InputField.ContentType.IntegerNumber);
            UIFactory.Button(addRow, "Add / edit channel", () =>
            {
                if (int.TryParse(chInput.text, out var id) && id > 0 && id < 1000)
                {
                    var channel = level.GetOrCreateColorChannel(id);
                    ColorField.Create(c, "Channel " + id, channel.color, v =>
                    {
                        channel.color = v;
                        Changed();
                    }, 140);
                }
            }, -1, 26, null, 12);

            UIFactory.SectionHeader(c, "Music");
            Text songLabel = null;
            void RefreshSongLabel()
            {
                if (songLabel == null) return;
                if (!string.IsNullOrEmpty(s.songFile)) songLabel.text = "Imported song: " + s.songFile + (LevelStorage.AssetExists(level.id, s.songFile) ? "" : "  (file missing!)");
                else if (!string.IsNullOrEmpty(s.songId)) songLabel.text = "Built-in song: " + s.songId;
                else songLabel.text = "No song. Import an mp3, ogg or wav to give the quest a soundtrack.";
            }
            songLabel = UIFactory.Label(c, "", 13, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 24);
            var songRow = UIFactory.Row(c, 32, 6);
            void ImportSongFrom(string path)
            {
                if (string.IsNullOrEmpty(path)) return;
                try
                {
                    s.songFile = LevelStorage.ImportAsset(level.id, path);
                    editor.MarkDirty();
                    RefreshSongLabel();
                    ui.Toast("Imported " + s.songFile);
                    editor.PreviewSong(s.songOffset);
                }
                catch (Exception e)
                {
                    ui.Toast("Import failed: " + e.Message);
                }
            }
            UIFactory.Button(songRow, "Import song…", () =>
            {
                // phones use the system document picker (works with scoped storage); desktops the in-app browser
                if (SongPicker.Available)
                {
                    ui.Toast("Choose an audio file…");
                    SongPicker.Pick(editor, path =>
                    {
                        if (string.IsNullOrEmpty(path)) ui.Toast("No song chosen");
                        else ImportSongFrom(path);
                    });
                    return;
                }
                FileBrowserDialog.Open(ui, "Choose a song", LevelStorage.AudioExtensions, ImportSongFrom);
            }, -1, 30, UIFactory.ButtonActive, 13);
            UIFactory.Button(songRow, "▶ Preview", () => editor.PreviewSong(s.songOffset), -1, 30, UIFactory.Good, 13);
            UIFactory.Button(songRow, "■ Stop", editor.StopSongPreview, -1, 30, null, 13);
            UIFactory.Button(songRow, "Remove", () =>
            {
                s.songFile = "";
                editor.StopSongPreview();
                editor.MarkDirty();
                RefreshSongLabel();
            }, -1, 30, UIFactory.Danger, 13);
            RefreshSongLabel();
            var starterIds = new List<string> { "" };
            starterIds.AddRange(LevelStorage.StarterSongIds());
            var starterNames = new List<string> { "(none)" };
            for (int i = 1; i < starterIds.Count; i++) starterNames.Add(starterIds[i]);
            DropdownField.Create(c, "Starter song", starterIds.ToArray(), Mathf.Max(0, starterIds.IndexOf(s.songId)), i =>
            {
                s.songId = starterIds[i];
                s.songFile = "";
                editor.MarkDirty();
                RefreshSongLabel();
                if (!string.IsNullOrEmpty(s.songId)) editor.PreviewSong(s.songOffset);
            }, 140, 28, starterNames.ToArray());
            UIFactory.Label(c, "Starter songs ship with the game from Assets/Geodashy/Resources/Songs. Campaign maps must use one of these.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            NumberField.Create(c, "Song offset (s)", s.songOffset, 0.5f, 0, 6000, v =>
            {
                s.songOffset = v;
                editor.MarkDirty();
            }, false, 140);
            NumberField.Create(c, "BPM", s.bpm, 1, 20, 400, v =>
            {
                s.bpm = v;
                editor.MarkDirty();
            }, false, 140);
            BoolField.Create(c, "Pulse the ground line and sky on every beat", s.beatPulse, v =>
            {
                s.beatPulse = v;
                editor.MarkDirty();
            });
            var mr = UIFactory.Row(c, 26, 12);
            BoolField.Create(mr, "Fade in", s.fadeIn, v =>
            {
                s.fadeIn = v;
                editor.MarkDirty();
            });
            BoolField.Create(mr, "Fade out", s.fadeOut, v =>
            {
                s.fadeOut = v;
                editor.MarkDirty();
            });
            UIFactory.Label(c, "Imported songs are copied into the level's asset folder and play from Song offset + travel time. Exported JSON does not include the audio file.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            UIFactory.Spacer(c, 12);
            UIFactory.Button(c, "Close", ui.CloseTopModal, -1, 32, UIFactory.ButtonActive);
        }
    }

    /// <summary>New / save / load / delete / import / export.</summary>
    /// <summary>Static checks on the level with click-to-jump results.</summary>
    public static class LintDialog
    {
        public static void Open(EditorUI ui, LevelEditor editor)
        {
            var issues = LevelLint.Check(editor.level);
            var c = ui.OpenModal("Quest check", 620, 520);
            var modal = ui.TopModal;
            int errors = 0;
            foreach (var i in issues) if (i.severity == "error") errors++;
            UIFactory.Label(c, issues.Count == 0 ? "No problems found. Ride on!" : issues.Count + " finding" + (issues.Count == 1 ? "" : "s") + " (" + errors + " serious). Click one to jump there.", 13, TextAnchor.MiddleLeft, issues.Count == 0 ? UIFactory.Good : UIFactory.TextColor, -1, 26);
            var scroll = UIFactory.ScrollView(c, "List", out var list, true, false);
            UIFactory.VLayout(list, 3, 4);
            UIFactory.Fitter(list, true, false);
            int rowH = ui.IsPhone ? 44 : 34;
            foreach (var issue in issues)
            {
                var it = issue;
                var b = UIFactory.Button(list, (it.severity == "error" ? "✖  " : "⚠  ") + it.message, () =>
                {
                    ui.CloseModal(modal);
                    editor.editorCamera.Position = new Vector2(it.x, Mathf.Max(editor.level.settings.groundY + 3f, it.y));
                    if (it.uid > 0)
                    {
                        editor.Select(it.uid, false);
                        if (editor.Mode != EditorMode.Edit) editor.SetMode(EditorMode.Edit);
                    }
                }, -1, rowH, it.severity == "error" ? new Color(0.5f, 0.18f, 0.18f) : UIFactory.ButtonBg, 12);
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            UIFactory.Label(c, "Checks: jump gaps for the current mount and speed (runes, shrooms and ledges excuse them), hazards hidden inside blocks, triggers aimed at empty groups, start positions past the finish, objects under the ground or beyond the finish.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 44);
        }
    }

    /// <summary>The undo history: every recorded change, newest at the bottom; click one to jump back or forward.</summary>
    public static class UndoHistoryDialog
    {
        public static void Open(EditorUI ui, LevelEditor editor)
        {
            var c = ui.OpenModal("History", 460, 560);
            var modal = ui.TopModal;
            UIFactory.Label(c, "Click an entry to return the level to that point. Entries below the current state are redo steps.", 12, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 34);
            var scroll = UIFactory.ScrollView(c, "List", out var list, true, false);
            UIFactory.VLayout(list, 2, 4);
            UIFactory.Fitter(list, true, false);
            var undos = editor.undo.UndoEntries;
            var redos = editor.undo.RedoEntries;
            int rowH = ui.IsPhone ? 40 : 28;
            UIFactory.Button(list, "Start of session", () =>
            {
                ui.CloseModal(modal);
                editor.JumpHistory(-undos.Count);
            }, -1, rowH, null, 12);
            for (int i = 0; i < undos.Count; i++)
            {
                int stepsBack = undos.Count - (i + 1);
                string label = (i + 1) + ".  " + undos[i].label;
                if (stepsBack == 0)
                {
                    UIFactory.Button(list, label + "   ◀ current", null, -1, rowH, UIFactory.ButtonActive, 12);
                    continue;
                }
                UIFactory.Button(list, label, () =>
                {
                    ui.CloseModal(modal);
                    editor.JumpHistory(-stepsBack);
                }, -1, rowH, null, 12);
            }
            for (int i = redos.Count - 1; i >= 0; i--)
            {
                int stepsForward = redos.Count - i;
                UIFactory.Button(list, "↷  " + redos[i].label, () =>
                {
                    ui.CloseModal(modal);
                    editor.JumpHistory(stepsForward);
                }, -1, rowH, UIFactory.PanelBg3, 12);
            }
            if (undos.Count == 0 && redos.Count == 0) UIFactory.Label(list, "No changes recorded yet.", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 30);
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    public static class FileDialog
    {
        public static void Open(EditorUI ui, LevelEditor editor)
        {
            var c = ui.OpenModal("Levels", 760, 620);
            var modal = ui.TopModal;

            var top = UIFactory.Row(c, 34, 6);
            UIFactory.Button(top, "New level", () =>
            {
                ui.Prompt("New level", "Name your quest:", "Untitled Quest", name =>
                {
                    editor.NewLevel(name);
                    ui.CloseModal(modal);
                });
            }, 120, 32);
            UIFactory.Button(top, "Save", ui.SaveWithPrompt, 90, 32, UIFactory.Good);
            UIFactory.Button(top, "Save as…", ui.PromptSaveAs, 100, 32);
            UIFactory.Button(top, "Export JSON", editor.ExportToClipboard, 120, 32);
            UIFactory.Button(top, "Import JSON", () =>
            {
                ui.Confirm("Import from clipboard", "Replace the current level with the JSON in your clipboard?", () =>
                {
                    if (editor.ImportFromClipboard()) ui.CloseModal(modal);
                }, "Import");
            }, 120, 32);
            UIFactory.Label(c, "Folder: " + LevelStorage.LevelsDirectory, 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            var share = UIFactory.Row(c, 34, 6);
            UIFactory.Button(share, "Copy share code", () =>
            {
                try
                {
                    var code = LevelShare.Encode(editor.level);
                    GUIUtility.systemCopyBuffer = code;
                    ui.Toast("Share code copied (" + (code.Length / 1024) + " KB). Paste it in a message; the other side imports it here.", 5f);
                }
                catch (Exception e)
                {
                    ui.Toast("Could not make a share code: " + e.Message);
                }
            }, 160, 32, UIFactory.ButtonActive);
            UIFactory.Button(share, "Import share code…", () =>
            {
                ui.Prompt("Import share code", "Paste the code (it starts with " + LevelShare.Prefix + "). Your clipboard is filled in below.", GUIUtility.systemCopyBuffer ?? "", text =>
                {
                    if (!LevelShare.TryDecode(text, out var data, out var err))
                    {
                        ui.Toast(err, 4f);
                        return;
                    }
                    Action load = () =>
                    {
                        data.id = System.Guid.NewGuid().ToString("N");
                        editor.RecordUndo("Import share code");
                        editor.LoadLevel(data, null);
                        editor.MarkDirty();
                        ui.CloseModal(modal);
                        ui.Toast("Imported " + data.name + " — songs are not carried by share codes; pick one in Settings if it had an imported file.", 6f);
                    };
                    if (editor.Dirty) ui.Confirm("Unsaved changes", "The current level has unsaved changes. Import anyway?", load, "Import");
                    else load();
                });
            }, 170, 32);
            UIFactory.Label(share, "A share code is the whole level as text: send it in a chat and the other phone imports it.", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 34);
            var camp = UIFactory.Row(c, 34, 6);
            UIFactory.Button(camp, "Export as campaign map", () =>
            {
                try
                {
                    var path = LevelStorage.ExportToCampaign(editor.level);
                    ui.Toast("Campaign map written: " + path, 5f);
                    Debug.Log("Geodashy: campaign map exported to " + path);
                }
                catch (Exception e)
                {
                    ui.Toast("Export failed: " + e.Message);
                }
            }, 200, 32, UIFactory.ButtonActive);
            UIFactory.Label(camp, Application.isEditor
                ? "Writes the JSON into Assets/Geodashy/Resources/Levels so it ships with the game. Commit it to git and it becomes a ★ campaign map."
                : "Writes the JSON into the campaign-exports folder next to your saves. Copy it into Assets/Geodashy/Resources/Levels in the project and commit.", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 34);

            var scroll = UIFactory.ScrollView(c, "List", out var list, true, false);
            UIFactory.VLayout(list, 3, 4);
            UIFactory.Fitter(list, true, false);
            Populate(ui, editor, list, modal);
        }

        static void Populate(EditorUI ui, LevelEditor editor, RectTransform list, GameObject modal)
        {
            foreach (Transform child in list) UnityEngine.Object.Destroy(child.gameObject);
            var levels = LevelStorage.ListLevels();
            if (levels.Count == 0) UIFactory.Label(list, "No saved levels yet. Press Save to store the current one.", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 30);
            foreach (var info in levels)
            {
                var i = info;
                var row = UIFactory.Row(list, 34, 6);
                row.gameObject.AddComponent<Image>().color = i.id == editor.level.id ? UIFactory.PanelBg3 : new Color(0, 0, 0, 0.15f);
                UIFactory.Label(row, (i.builtIn ? "★ " : "") + i.name, 14, TextAnchor.MiddleLeft, UIFactory.TextColor, 250, 34, true);
                UIFactory.Label(row, i.author, 12, TextAnchor.MiddleLeft, UIFactory.TextDim, 120, 34);
                UIFactory.Label(row, i.objectCount + " objs", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, 70, 34);
                UIFactory.Label(row, i.modified.ToString("yyyy-MM-dd HH:mm"), 12, TextAnchor.MiddleLeft, UIFactory.TextDim, 120, 34);
                UIFactory.Button(row, "Load", () =>
                {
                    Action load = () =>
                    {
                        try
                        {
                            editor.LoadLevel(LevelStorage.Load(i.path), i.path);
                            ui.CloseModal(modal);
                        }
                        catch (Exception e)
                        {
                            ui.Toast("Load failed: " + e.Message);
                        }
                    };
                    if (editor.Dirty) ui.Confirm("Unsaved changes", "The current level has unsaved changes. Load anyway?", load, "Load");
                    else load();
                }, 70, 30, UIFactory.Good, 12);
                if (!i.builtIn)
                {
                    UIFactory.Button(row, "Delete", () =>
                    {
                        ui.Confirm("Delete " + i.name + "?", "The file will be removed from disk.", () =>
                        {
                            LevelStorage.Delete(i.path);
                            try
                            {
                                LevelStorage.DeleteAssets(i.id);
                            }
                            catch (Exception)
                            {
                            }
                            Populate(ui, editor, list, modal);
                        }, "Delete");
                    }, 70, 30, UIFactory.Danger, 12);
                }
                else UIFactory.Spacer(row, 30, 70);
            }
        }
    }

    public static class HelpDialog
    {
        const string HelpText =
            "MODES\n" +
            "1 / 2 / 3 or Tab — Build / Edit / Delete\n" +
            "Esc — cancel drag, deselect, clear brush, close dialog\n\n" +
            "BUILD\n" +
            "Click — place brush object (drag to paint)\n" +
            "Ctrl+click — select instead of placing\n" +
            "Q / E — rotate brush or selection 90° (Shift 45°, Alt 5°)\n" +
            "F / V — flip horizontally / vertically\n" +
            "R — reset brush transform\n\n" +
            "EDIT\n" +
            "Click / drag — select and move\n" +
            "Shift+click — add or remove from selection\n" +
            "Drag on empty space — box select (Shift adds)\n" +
            "Arrows — nudge selection (Shift ×5, Alt ÷8)\n" +
            "Ctrl+A / Ctrl+I — select all / invert\n" +
            "T — select everything of the same type\n" +
            "Delete / Backspace — delete selection\n" +
            "Ctrl+C / X / V / D — copy / cut / paste at cursor / duplicate\n" +
            "Ctrl+Z / Ctrl+Y — undo / redo\n" +
            "X — transform gizmo: drag corners to scale (edges for one axis), the ring to rotate, the centre to move; Shift = fine steps\n" +
            "Ctrl + / Ctrl − — scale selection\n" +
            "Edit dock ▸ Defeat heatmap — overlay of every death from this session's playtests\n\n" +
            "VIEW\n" +
            "W A S D — pan   ·   Mouse wheel — zoom   ·   Middle / right drag or Space+drag — pan\n" +
            "Touch: one finger — place / select / drag   ·   two fingers — pan and pinch to zoom\n" +
            "+ / − — zoom   ·   Ctrl+0 — reset zoom   ·   Home / End — level start / end\n" +
            "G — toggle grid snap   ·   Ctrl+G — toggle grid   ·   [ ] — editor layer   ·   \\ — show all layers\n" +
            "H — hide interface   ·   B — hitbox edges on every object (green safe, red kills, blue interacts)\n\n" +
            "SPAWN\n" +
            "The green ghost rider marks where you spawn: x 0 on the ground, or the right-most enabled Start Position object.\n" +
            "Place a Start Position (Special category) to move it and to override the mount, speed and gravity.\n" +
            "Home — jump the camera to the spawn\n\n" +
            "PLAYTEST\n" +
            "P or Enter — play from the start   ·   Shift+P or Ctrl+Enter — play from the marker\n" +
            "M — set marker at cursor   ·   Shift+M — clear marker\n" +
            "Alt+P or ▶ Train — Training difficulty (your own waystones). Test as… in the View dock picks Checkpoints or Champion for ▶ Play.\n" +
            "In play: click / Space — action   ·   Esc — pause   ·   R — restart\n" +
            "Difficulties: Training (place your own waystones), Checkpoints (respawn at Waystone objects you placed in the level), Champion (no checkpoints; sets the record)\n" +
            "Training: Z — raise a waystone   ·   X — remove the last one   ·   ← → — scrub between waystones   ·   C — toggle\n" +
            "Waystones restore everything: mount, gravity, speed, moved objects, colours, loot. Auto waystones rise every few seconds on solid ground.\n\n" +
            "EVEN MORE TOOLS\n" +
            "Palette ▸ Path tool — click points, then Lay (grid) or Lay (beat) places the brush along the line\n" +
            "Edit dock ▸ Replace with brush / Replace all of type — swap object types in place\n" +
            "View dock ▸ Colour channels — click a swatch to select its users, Recolour to edit it   ·   Check quest (lint) — finds impossible gaps, buried spikes, empty trigger targets\n" +
            "View dock ▸ Show last playtest path — the rider's route and death point drawn over the level\n" +
            "Properties with several objects — blank fields differ; typing a value sets it on all of them\n" +
            "Volley trigger — boss fire: shows a group again and again on a beat (pair with spawn-triggered Move triggers)\n\n" +
            "MORE TOOLS\n" +
            "Right-click (or long-press on touch) an object — context menu: copy, duplicate, delete, properties, select same type, save as stamp\n" +
            "View dock ▸ Snap to neighbours — dragging snaps edges and centres to nearby objects with pink guide lines\n" +
            "Stamps shelf (palette) — reusable groups: select objects, Save as stamp, then place them like a brush\n" +
            "Timeline strip under the top bar — bar lines, triggers and portals along the level; click to jump, drag a trigger to retime it\n" +
            "View dock ▸ Groups — hide or lock a group while you work   ·   History — jump to any earlier state\n" +
            "Files ▸ Copy share code / Import share code — the whole level as text you can paste in a chat\n\n" +
            "PHONE LAYOUT (automatic on Android; Options ▸ Switch layout elsewhere)\n" +
            "One finger — place, select, drag   ·   two fingers — pan and pinch to zoom\n" +
            "View / Props — slide-in drawers   ·   ▼ hides the dock   ·   ⋯ holds save, files, settings, marker and help\n" +
            "Build dock: category ▾ picks a shelf, Search… filters, the strip scrolls sideways, Swipe paints while dragging\n" +
            "In play: tap anywhere — action   ·   ❚❚ — pause   ·   the Android back button also pauses\n\n" +
            "FILES\n" +
            "Ctrl+S — save   ·   Ctrl+Shift+S — save as   ·   Ctrl+O — files\n" +
            "Levels autosave every minute. JSON lives in the persistent data folder and can be exported to the clipboard.";

        public static void Open(EditorUI ui)
        {
            var c = ui.OpenModal("Help & Shortcuts", 820, 760, true);
            var t = UIFactory.Label(c, HelpText, 14, TextAnchor.UpperLeft, UIFactory.TextColor, -1, 1200);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
