using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Title screen and level select. Built entirely from code, like the editor UI.</summary>
    public class MainMenu : MonoBehaviour
    {
        AppController app;
        Camera cam;
        ParallaxBackground background;
        GroundRenderer ground;
        LevelSettings backdropSettings;
        RectTransform root, titleScreen, levelScreen;
        RectTransform listContent, detailPane;
        Canvas canvas;
        float drift;
        List<LevelFileInfo> levels = new List<LevelFileInfo>();
        LevelFileInfo selected;
        readonly List<Button> rowButtons = new List<Button>();

        public void Initialize(AppController controller, Camera camera)
        {
            app = controller;
            cam = camera;
            BuildBackdrop();
            BuildUI();
            ShowTitle();
        }

        void BuildBackdrop()
        {
            backdropSettings = new LevelSettings();
            string[] themes = ThemeCatalog.BackgroundIds;
            backdropSettings.backgroundTheme = themes[UnityEngine.Random.Range(0, themes.Length)];
            backdropSettings.backgroundColor = ThemeCatalog.GetBackground(backdropSettings.backgroundTheme).skyBottom;
            LevelSerializer.MigrateParallax(backdropSettings);
            background = ParallaxBackground.Create(transform, cam, backdropSettings);
            ground = GroundRenderer.Create(transform, cam, backdropSettings);
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.backgroundColor = backdropSettings.backgroundColor;
            cam.transform.position = new Vector3(0f, 4f, -10f);
        }

        void BuildUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                esGo.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            canvas.pixelPerfect = true;
            EditorUI.ConfigureScaler(gameObject.AddComponent<CanvasScaler>());
            gameObject.AddComponent<GraphicRaycaster>();
            root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);

            // ---- title screen ---------------------------------------------------
            titleScreen = UIFactory.Rect(root, "Title");
            UIFactory.Stretch(titleScreen);
            var titleCard = UIFactory.Panel(titleScreen, "Card", new Color(0.11f, 0.09f, 0.14f, 0.82f));
            UIFactory.Anchor(titleCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -250), new Vector2(300, 250));
            UIFactory.VLayout(titleCard, 12, 32, true, true, TextAnchor.UpperCenter);
            var title = UIFactory.Label(titleCard, "GEODASHY", 64, TextAnchor.MiddleCenter, UIFactory.Accent, -1, 90, true);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Label(titleCard, "One button. Seven mounts. A kingdom of spikes.", 16, TextAnchor.MiddleCenter, UIFactory.TextDim, -1, 28);
            UIFactory.Spacer(titleCard, 10);
            UIFactory.Button(titleCard, "Play", ShowLevelSelect, -1, 54, UIFactory.Good, 22);
            UIFactory.Button(titleCard, "Level Editor", () => app.OpenEditor(null, null), -1, 54, UIFactory.ButtonActive, 22);
            UIFactory.Button(titleCard, "Quit", app.Quit, -1, 44, UIFactory.Danger, 18);
            UIFactory.Spacer(titleCard, 6);
            UIFactory.Label(titleCard, "Click or Space to ride · Esc pauses", 13, TextAnchor.MiddleCenter, UIFactory.TextDim, -1, 24);

            // ---- level select ---------------------------------------------------
            levelScreen = UIFactory.Rect(root, "LevelSelect");
            UIFactory.Stretch(levelScreen);
            var frame = UIFactory.Panel(levelScreen, "Frame", new Color(0.11f, 0.09f, 0.14f, 0.9f));
            UIFactory.Anchor(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-620, -360), new Vector2(620, 360));
            var header = UIFactory.Rect(frame, "Header");
            UIFactory.Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -56), new Vector2(-16, -8));
            UIFactory.HLayout(header, 10, 0, false, TextAnchor.MiddleLeft);
            UIFactory.Button(header, "◀ Back", ShowTitle, 100, 40);
            UIFactory.Label(header, "Choose a quest", 26, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 40, true);
            UIFactory.Button(header, "Refresh", RefreshLevels, 100, 40);
            UIFactory.Button(header, "+ New quest", () => app.OpenEditor(LevelData.CreateNew("Untitled Quest"), null), 140, 40, UIFactory.Good);

            var listHost = UIFactory.Rect(frame, "ListHost");
            UIFactory.Anchor(listHost, new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(16, 16), new Vector2(-8, -64));
            var scroll = UIFactory.ScrollView(listHost, "Levels", out listContent, true, false);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>());
            UIFactory.VLayout(listContent, 4, 6);
            UIFactory.Fitter(listContent, true, false);

            detailPane = UIFactory.Panel(frame, "Detail", new Color(0.17f, 0.14f, 0.21f, 0.98f));
            UIFactory.Anchor(detailPane, new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(8, 16), new Vector2(-16, -64));
            UIFactory.VLayout(detailPane, 8, 18);
        }

        void ShowTitle()
        {
            titleScreen.gameObject.SetActive(true);
            levelScreen.gameObject.SetActive(false);
        }

        void ShowLevelSelect()
        {
            titleScreen.gameObject.SetActive(false);
            levelScreen.gameObject.SetActive(true);
            RefreshLevels();
        }

        void RefreshLevels()
        {
            levels = LevelStorage.ListLevels();
            foreach (Transform child in listContent) Destroy(child.gameObject);
            rowButtons.Clear();
            if (levels.Count == 0) UIFactory.Label(listContent, "No quests yet. Press + New quest to build one.", 14, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 40);
            foreach (var info in levels)
            {
                var i = info;
                var stats = LevelStatsStorage.Load(i.id);
                string status = stats.completions > 0 ? "✔ cleared" : (stats.bestProgress > 0f ? (stats.bestProgress * 100f).ToString("0") + "%" : "new");
                var b = UIFactory.Button(listContent, (i.builtIn ? "★ " : "") + i.name + "\n<size=11>" + (string.IsNullOrEmpty(i.author) ? "unknown author" : i.author) + " · " + status + "</size>",
                    () => Select(i), -1, 52, null, 15);
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.MiddleLeft;
                t.supportRichText = true;
                rowButtons.Add(b);
            }
            LevelFileInfo keep = null;
            if (selected != null) foreach (var l in levels) if (l.id == selected.id) keep = l;
            Select(keep ?? (levels.Count > 0 ? levels[0] : null));
        }

        void Select(LevelFileInfo info)
        {
            selected = info;
            for (int i = 0; i < rowButtons.Count && i < levels.Count; i++) UIFactory.SetButtonActive(rowButtons[i], levels[i] == info);
            foreach (Transform child in detailPane) Destroy(child.gameObject);
            if (info == null) return;
            var stats = LevelStatsStorage.Load(info.id);
            var mount = MountCatalog.Get(info.startMount);
            UIFactory.Label(detailPane, info.name, 24, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 36, true);
            UIFactory.Label(detailPane, "by " + (string.IsNullOrEmpty(info.author) ? "unknown" : info.author) + (info.builtIn ? "  ·  built-in" : ""), 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            if (!string.IsNullOrEmpty(info.description)) UIFactory.Label(detailPane, info.description, 13, TextAnchor.UpperLeft, UIFactory.TextColor, -1, 48);
            var row = UIFactory.Row(detailPane, 72, 12);
            UIFactory.Icon(row, SpriteLibrary.ForMount(mount), 72);
            var col = UIFactory.Column(row, -1, 2);
            UIFactory.Label(col, "Starts on the " + mount.name, 14, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 22, true);
            UIFactory.Label(col, mount.control, 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            UIFactory.Label(col, ThemeCatalog.GetBackground(info.backgroundTheme).name + " · " + info.objectCount + " objects · ~" + Mathf.RoundToInt(info.lengthSeconds) + "s", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            UIFactory.SectionHeader(detailPane, "Your record");
            string best = stats.completions > 0 ? "Cleared " + stats.completions + "×" : (stats.bestProgress > 0f ? "Best " + (stats.bestProgress * 100f).ToString("0.0") + "%" : "Not attempted");
            UIFactory.Label(detailPane, best + "  ·  " + stats.attempts + " attempts  ·  " + stats.deaths.Count + " recorded deaths", 13, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 22);
            UIFactory.Spacer(detailPane, 6);
            UIFactory.Button(detailPane, "▶ Play", () => Launch(info, false), -1, 50, UIFactory.Good, 20);
            UIFactory.Button(detailPane, "▶ Squire mode (waystones)", () => Launch(info, true), -1, 42, null, 16);
            UIFactory.Button(detailPane, "Edit in level editor", () =>
            {
                try
                {
                    app.OpenEditor(LevelStorage.Load(info.path), info.path);
                }
                catch (Exception e)
                {
                    Debug.LogError("Could not open level: " + e.Message);
                }
            }, -1, 42, UIFactory.ButtonActive, 16);
        }

        void Launch(LevelFileInfo info, bool practice)
        {
            try
            {
                app.PlayLevel(LevelStorage.Load(info.path), practice);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not load level: " + e.Message);
            }
        }

        void Update()
        {
            // slow camera drift so the parallax layers move behind the menu
            drift += Time.deltaTime * 2.5f;
            cam.transform.position = new Vector3(drift, 4f + Mathf.Sin(drift * 0.15f) * 0.5f, -10f);
            var kb = Keyboard.current;
            if (kb != null && kb[Key.Escape].wasPressedThisFrame && levelScreen != null && levelScreen.gameObject.activeSelf) ShowTitle();
        }
    }
}
