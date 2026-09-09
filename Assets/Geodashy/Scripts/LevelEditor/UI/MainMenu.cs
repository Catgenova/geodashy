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
        RectTransform root, titleScreen, levelScreen, heraldryScreen, optionsScreen;
        AudioSource menuMusic;
        Image titleCrest, heraldryCrestPreview, heraldryMountPreview;
        readonly List<Button> crestButtons = new List<Button>();
        RectTransform listContent, detailPane;
        Canvas canvas;
        float drift;
        List<LevelFileInfo> levels = new List<LevelFileInfo>();
        LevelFileInfo selected;
        readonly List<Button> rowButtons = new List<Button>();
        CanvasScaler scaler;
        bool phone;
        /// <summary>Set before the menu is rebuilt so it reopens on the Options screen (used by the layout switch).</summary>
        public static bool reopenOptions;

        public void Initialize(AppController controller, Camera camera)
        {
            app = controller;
            cam = camera;
            BuildBackdrop();
            BuildUI();
            if (reopenOptions)
            {
                reopenOptions = false;
                ShowOptions();
            }
            else ShowTitle();
            StartMenuMusic();
        }

        void StartMenuMusic()
        {
            var clip = Resources.Load<AudioClip>("Songs/the_kingdom");
            if (clip == null)
            {
                var all = Resources.LoadAll<AudioClip>("Songs");
                if (all.Length == 0) return;
                clip = all[UnityEngine.Random.Range(0, all.Length)];
            }
            menuMusic = gameObject.AddComponent<AudioSource>();
            menuMusic.clip = clip;
            menuMusic.loop = true;
            menuMusic.volume = Sfx.MusicVolume * 0.7f;
            menuMusic.playOnAwake = false;
            menuMusic.Play();
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
            scaler = gameObject.AddComponent<CanvasScaler>();
            EditorUI.ConfigureScaler(scaler);
            gameObject.AddComponent<GraphicRaycaster>();
            phone = EditorUI.PhoneLayout;
            root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);
            if (phone) EditorUI.ApplySafeArea(root);

            // ---- title screen ---------------------------------------------------
            titleScreen = UIFactory.Rect(root, "Title");
            UIFactory.Stretch(titleScreen);
            var titleCard = UIFactory.Panel(titleScreen, "Card", new Color(0.11f, 0.09f, 0.14f, 0.82f));
            if (phone) UIFactory.Anchor(titleCard, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-280, 12), new Vector2(280, -12));
            else UIFactory.Anchor(titleCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -330), new Vector2(300, 330));
            UIFactory.VLayout(titleCard, phone ? 6 : 12, phone ? 16 : 32, true, true, TextAnchor.UpperCenter);
            var crestRow = UIFactory.Row(titleCard, phone ? 44 : 64, 0, TextAnchor.MiddleCenter);
            titleCrest = UIFactory.Icon(crestRow, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), phone ? 44 : 64);
            var title = UIFactory.Label(titleCard, GameInfo.Title.ToUpperInvariant(), phone ? 46 : 64, TextAnchor.MiddleCenter, UIFactory.Accent, -1, phone ? 60 : 90, true);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Label(titleCard, "One button. Seven mounts. A kingdom of spikes.", phone ? 13 : 16, TextAnchor.MiddleCenter, UIFactory.TextDim, -1, phone ? 22 : 28);
            UIFactory.Spacer(titleCard, phone ? 4 : 10);
            UIFactory.Button(titleCard, "Play", ShowLevelSelect, -1, phone ? 46 : 54, UIFactory.Good, phone ? 18 : 22);
            UIFactory.Button(titleCard, "Level Editor", () => app.OpenEditor(null, null), -1, phone ? 46 : 54, UIFactory.ButtonActive, phone ? 18 : 22);
            UIFactory.Button(titleCard, "Heraldry", ShowHeraldry, -1, phone ? 40 : 44, null, phone ? 15 : 18);
            UIFactory.Button(titleCard, "Options", ShowOptions, -1, phone ? 40 : 44, null, phone ? 15 : 18);
            UIFactory.Button(titleCard, "Quit", app.Quit, -1, phone ? 40 : 44, UIFactory.Danger, phone ? 15 : 18);
            UIFactory.Spacer(titleCard, phone ? 2 : 6);
            UIFactory.Label(titleCard, phone ? "Tap anywhere to ride · ❚❚ pauses" : "Click or Space to ride · Esc pauses", 13, TextAnchor.MiddleCenter, UIFactory.TextDim, -1, phone ? 20 : 24);

            // ---- level select ---------------------------------------------------
            levelScreen = UIFactory.Rect(root, "LevelSelect");
            UIFactory.Stretch(levelScreen);
            var frame = UIFactory.Panel(levelScreen, "Frame", new Color(0.11f, 0.09f, 0.14f, 0.9f));
            if (phone) UIFactory.Stretch(frame, 12, 12, 12, 12);
            else UIFactory.Anchor(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-620, -400), new Vector2(620, 400));
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

            // the detail pane scrolls so the record, difficulty buttons and description fit on short screens
            var detailHost = UIFactory.Panel(frame, "Detail", new Color(0.17f, 0.14f, 0.21f, 0.98f));
            UIFactory.Anchor(detailHost, new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(8, 16), new Vector2(-16, -64));
            var detailScroll = UIFactory.ScrollView(detailHost, "Scroll", out detailPane, true, false, Color.clear);
            UIFactory.Stretch(detailScroll.GetComponent<RectTransform>());
            UIFactory.VLayout(detailPane, phone ? 6 : 8, phone ? 12 : 18);
            UIFactory.Fitter(detailPane, true, false);
        }

        /// <summary>A framed screen with a header row and a scrolling column of content, sized for the current layout.</summary>
        RectTransform BuildScreen(string name, string title, float halfWidth, float halfHeight, out RectTransform content)
        {
            var screen = UIFactory.Rect(root, name);
            UIFactory.Stretch(screen);
            var frame = UIFactory.Panel(screen, "Frame", new Color(0.11f, 0.09f, 0.14f, 0.92f));
            if (phone) UIFactory.Stretch(frame, 12, 12, 12, 12);
            else UIFactory.Anchor(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-halfWidth, -halfHeight), new Vector2(halfWidth, halfHeight));
            UIFactory.VLayout(frame, 8, phone ? 12 : 20, true, true, TextAnchor.UpperLeft);
            var header = UIFactory.Row(frame, 40, 10);
            UIFactory.Button(header, "◀ Back", ShowTitle, 100, 40);
            UIFactory.Label(header, title, phone ? 22 : 26, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 40, true);
            var scroll = UIFactory.ScrollView(frame, "Scroll", out content, true, false, Color.clear);
            UIFactory.Layout(scroll.gameObject, -1, -1, 1, 1);
            UIFactory.VLayout(content, phone ? 8 : 12, 4);
            UIFactory.Fitter(content, true, false);
            return screen;
        }

        void ShowTitle()
        {
            titleScreen.gameObject.SetActive(true);
            levelScreen.gameObject.SetActive(false);
            if (heraldryScreen != null) heraldryScreen.gameObject.SetActive(false);
            if (optionsScreen != null) optionsScreen.gameObject.SetActive(false);
            titleCrest.sprite = PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary);
        }

        void ShowLevelSelect()
        {
            titleScreen.gameObject.SetActive(false);
            levelScreen.gameObject.SetActive(true);
            if (heraldryScreen != null) heraldryScreen.gameObject.SetActive(false);
            if (optionsScreen != null) optionsScreen.gameObject.SetActive(false);
            RefreshLevels();
        }

        // ---- heraldry -----------------------------------------------------------

        void ShowOptions()
        {
            titleScreen.gameObject.SetActive(false);
            levelScreen.gameObject.SetActive(false);
            if (heraldryScreen != null) heraldryScreen.gameObject.SetActive(false);
            if (optionsScreen == null) BuildOptions();
            optionsScreen.gameObject.SetActive(true);
        }

        void BuildOptions()
        {
            optionsScreen = BuildScreen("Options", "Options", 320, 300, out var frame);

            UIFactory.SectionHeader(frame, "Interface");
            var uiLabel = UIFactory.Label(frame, "", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            void RefreshUiLabel() => uiLabel.text = "Interface size " + Mathf.RoundToInt(EditorUI.UIScale * 100f) + "%  ·  Layout: " + EditorUI.PhoneLayoutName;
            void SetScale(float v)
            {
                EditorUI.UIScale = v;
                PlayerPrefs.Save();
                EditorUI.ConfigureScaler(scaler);
                RefreshUiLabel();
            }
            var sizeRow = UIFactory.Row(frame, 36, 6);
            UIFactory.Button(sizeRow, "Smaller", () => SetScale(EditorUI.UIScale - 0.1f), -1, 34, null, 13);
            UIFactory.Button(sizeRow, "Reset", () => SetScale(phone ? 2f : 1f), -1, 34, null, 13);
            UIFactory.Button(sizeRow, "Larger", () => SetScale(EditorUI.UIScale + 0.1f), -1, 34, null, 13);
            UIFactory.Button(frame, "Switch layout (Auto / Phone / Desktop)", () =>
            {
                EditorUI.CyclePhoneLayout();
                reopenOptions = true;
                app.ShowMenu();   // the menu rebuilds itself in the new layout; the editor picks it up when opened
            }, -1, 36, UIFactory.ButtonActive, 13);
            UIFactory.Label(frame, "The phone layout has finger-sized controls, drawers instead of docks and touch gestures. Auto picks it on Android and the desktop layout elsewhere.", 12, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 34);
            RefreshUiLabel();

            UIFactory.SectionHeader(frame, "Music volume");
            var musicLabel = UIFactory.Label(frame, "", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            UIFactory.Slider(frame, 0f, 1f, Sfx.MusicVolume, v =>
            {
                Sfx.MusicVolume = v;
                if (menuMusic != null) menuMusic.volume = v * 0.7f;
                musicLabel.text = Mathf.RoundToInt(v * 100f) + "%";
            }, false, 26);
            musicLabel.text = Mathf.RoundToInt(Sfx.MusicVolume * 100f) + "%";

            UIFactory.SectionHeader(frame, "Sound effects volume");
            var sfxLabel = UIFactory.Label(frame, "", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            UIFactory.Slider(frame, 0f, 1f, Sfx.Volume, v =>
            {
                Sfx.Volume = v;
                sfxLabel.text = Mathf.RoundToInt(v * 100f) + "%";
            }, false, 26);
            sfxLabel.text = Mathf.RoundToInt(Sfx.Volume * 100f) + "%";
            var testRow = UIFactory.Row(frame, 34, 6);
            UIFactory.Button(testRow, "Test jump", () => Sfx.Play("jump_horse"), -1, 32, null, 13);
            UIFactory.Button(testRow, "Test rune", () => Sfx.Play("rune"), -1, 32, null, 13);
            UIFactory.Button(testRow, "Test death", () => Sfx.Play("death"), -1, 32, null, 13);
            UIFactory.Label(frame, "Effects live in Assets/Geodashy/Resources/SFX; replace any file to change a sound.", 12, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
        }

        void ShowHeraldry()
        {
            titleScreen.gameObject.SetActive(false);
            levelScreen.gameObject.SetActive(false);
            if (optionsScreen != null) optionsScreen.gameObject.SetActive(false);
            if (heraldryScreen == null) BuildHeraldry();
            heraldryScreen.gameObject.SetActive(true);
            RefreshHeraldry();
        }

        void BuildHeraldry()
        {
            heraldryScreen = BuildScreen("Heraldry", "Your heraldry", 460, 320, out var frame);
            UIFactory.Label(frame, "Your crest marks your scroll and your seal. Banner and trim colour your rider and the P1 / P2 colour channels that level makers can use.", 13, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 36);

            float preview = phone ? 100 : 150;
            var previewRow = UIFactory.Row(frame, preview, 24, TextAnchor.MiddleCenter);
            heraldryCrestPreview = UIFactory.Icon(previewRow, null, preview);
            heraldryMountPreview = UIFactory.Icon(previewRow, null, preview);

            UIFactory.SectionHeader(frame, "Crest");
            var crestRow = UIFactory.Row(frame, 44, 6);
            crestButtons.Clear();
            for (int i = 0; i < PlayerProfile.CrestNames.Length; i++)
            {
                int idx = i;
                crestButtons.Add(UIFactory.Button(crestRow, PlayerProfile.CrestNames[i], () =>
                {
                    PlayerProfile.Crest = idx;
                    RefreshHeraldry();
                }, -1, 40, null, 13));
            }
            UIFactory.SectionHeader(frame, "Banner colour");
            BuildSwatches(frame, c =>
            {
                PlayerProfile.Primary = c;
                RefreshHeraldry();
            });
            UIFactory.SectionHeader(frame, "Trim colour");
            BuildSwatches(frame, c =>
            {
                PlayerProfile.Secondary = c;
                RefreshHeraldry();
            });
        }

        void BuildSwatches(Transform parent, Action<Color> onPick)
        {
            var row = UIFactory.Row(parent, 40, 6);
            foreach (var hex in PlayerProfile.Palette)
            {
                var c = ObjectCatalog.Hex(hex);
                var sw = UIFactory.Swatch(row, c, 36);
                UIFactory.Layout(sw.gameObject, -1, 36, 1);
                var b = sw.gameObject.AddComponent<Button>();
                b.targetGraphic = sw;
                b.onClick.AddListener(() => onPick(c));
            }
        }

        void RefreshHeraldry()
        {
            PlaceholderSpriteFactory.ClearHeraldryArt();
            var crest = PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary, 128);
            heraldryCrestPreview.sprite = crest;
            MountDefinition previewMount = MountCatalog.Get("griffin");
            foreach (var m in MountCatalog.All) if (!SpriteLibrary.MountIsAnimated(m)) { previewMount = m; break; }
            heraldryMountPreview.sprite = PlaceholderSpriteFactory.ForMount(previewMount);
            titleCrest.sprite = crest;
            for (int i = 0; i < crestButtons.Count; i++) UIFactory.SetButtonActive(crestButtons[i], i == PlayerProfile.Crest);
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
            var mountIcon = UIFactory.Icon(row, SpriteLibrary.ForMount(mount), 72);
            mountIcon.rectTransform.localScale = new Vector3(SpriteLibrary.MountFacing(mount), 1f, 1f);
            var col = UIFactory.Column(row, -1, 2);
            UIFactory.Label(col, "Starts on the " + mount.name, 14, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 22, true);
            UIFactory.Label(col, mount.control, 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            UIFactory.Label(col, ThemeCatalog.GetBackground(info.backgroundTheme).name + " · " + info.objectCount + " objects · ~" + Mathf.RoundToInt(info.lengthSeconds) + "s", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            UIFactory.SectionHeader(detailPane, "Your record");
            string best = stats.completions > 0 ? "Champion: cleared " + stats.completions + "×" : (stats.bestProgress > 0f ? "Champion best " + (stats.bestProgress * 100f).ToString("0.0") + "%" : "Champion: not attempted");
            UIFactory.Label(detailPane, best + "  ·  " + stats.attempts + " attempts" + (stats.checkpointCompletions > 0 ? "  ·  Checkpoints: cleared " + stats.checkpointCompletions + "×" : ""), 13, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 22);
            UIFactory.SectionHeader(detailPane, "Ride out");
            UIFactory.Button(detailPane, "▶ Training", () => Launch(info, Difficulty.Training), -1, 40, null, 16);
            UIFactory.Label(detailPane, DifficultyInfo.Describe(Difficulty.Training), 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            UIFactory.Button(detailPane, "▶ Checkpoints", () => Launch(info, Difficulty.Checkpoints), -1, 40, UIFactory.ButtonActive, 16);
            UIFactory.Label(detailPane, DifficultyInfo.Describe(Difficulty.Checkpoints), 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
            UIFactory.Button(detailPane, "▶ Champion", () => Launch(info, Difficulty.Champion), -1, 40, UIFactory.Good, 16);
            UIFactory.Label(detailPane, DifficultyInfo.Describe(Difficulty.Champion), 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
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

        void Launch(LevelFileInfo info, Difficulty difficulty)
        {
            try
            {
                app.PlayLevel(LevelStorage.Load(info.path), difficulty);
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
