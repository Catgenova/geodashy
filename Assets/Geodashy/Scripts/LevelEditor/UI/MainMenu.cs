using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Gameplay;
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
        RectTransform root, titleScreen, levelScreen, heraldryScreen, optionsScreen, campaignScreen;
        Button jumpKeyButton;
        bool remapping;
        AudioSource menuMusic;
        Image titleCrest, heraldryCrestPreview, heraldryMountPreview;
        readonly List<Button> crestButtons = new List<Button>();
        RectTransform listContent, detailPane;
        Canvas canvas;
        float drift;
        List<LevelFileInfo> levels = new List<LevelFileInfo>();
        LevelFileInfo selected;
        readonly List<Button> rowButtons = new List<Button>();
        // level select filter and sort (cycle buttons; persisted)
        static readonly string[] FilterNames = { "All quests", "Not cleared", "Cleared", "Easy", "Normal", "Hard", "Harder", "Insane", "Demon", "Unrated" };
        static readonly string[] SortNames = { "Default", "Name", "Difficulty", "Length", "Newest" };
        int filterIndex = PlayerPrefs.GetInt("geodashy.levelFilter", 0);
        int sortIndex = PlayerPrefs.GetInt("geodashy.levelSort", 0);
        Button filterButton, sortButton;
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
            UIFactory.Button(titleCard, L10n.T("Campaign"), ShowCampaign, -1, phone ? 44 : 50, UIFactory.Good, phone ? 17 : 20);
            var playRow = UIFactory.Row(titleCard, phone ? 42 : 46, 8);
            UIFactory.Button(playRow, L10n.T("All quests"), ShowLevelSelect, -1, phone ? 42 : 46, UIFactory.Good, phone ? 15 : 17);
            UIFactory.Button(playRow, L10n.T("Daily Quest"), PlayDaily, -1, phone ? 42 : 46, UIFactory.ButtonActive, phone ? 15 : 17);
            UIFactory.Button(titleCard, L10n.T("Level Editor"), () => app.OpenEditor(null, null), -1, phone ? 42 : 46, UIFactory.ButtonActive, phone ? 16 : 18);
            var smallRow = UIFactory.Row(titleCard, phone ? 36 : 40, 8);
            UIFactory.Button(smallRow, L10n.T("Heraldry"), ShowHeraldry, -1, phone ? 36 : 40, null, phone ? 14 : 16);
            UIFactory.Button(smallRow, L10n.T("Options"), ShowOptions, -1, phone ? 36 : 40, null, phone ? 14 : 16);
            UIFactory.Button(smallRow, L10n.T("Quit"), app.Quit, -1, phone ? 36 : 40, UIFactory.Danger, phone ? 14 : 16);
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
            var filters = UIFactory.Rect(frame, "Filters");
            UIFactory.Anchor(filters, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(16, -96), new Vector2(-8, -62));
            UIFactory.HLayout(filters, 6, 0, true, TextAnchor.MiddleLeft);
            filterButton = UIFactory.Button(filters, "", () =>
            {
                filterIndex = (filterIndex + 1) % FilterNames.Length;
                PlayerPrefs.SetInt("geodashy.levelFilter", filterIndex);
                RefreshLevels();
            }, -1, 32, null, 13);
            sortButton = UIFactory.Button(filters, "", () =>
            {
                sortIndex = (sortIndex + 1) % SortNames.Length;
                PlayerPrefs.SetInt("geodashy.levelSort", sortIndex);
                RefreshLevels();
            }, -1, 32, null, 13);

            var listHost = UIFactory.Rect(frame, "ListHost");
            UIFactory.Anchor(listHost, new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(16, 16), new Vector2(-8, -100));
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

        void PlayDaily()
        {
            var level = DailyQuest.Generate(DailyQuest.TodayKey);
            app.PlayLevel(level, Difficulty.Champion);
        }

        // ---- campaign map -------------------------------------------------------

        void ShowCampaign()
        {
            titleScreen.gameObject.SetActive(false);
            levelScreen.gameObject.SetActive(false);
            if (heraldryScreen != null) heraldryScreen.gameObject.SetActive(false);
            if (optionsScreen != null) optionsScreen.gameObject.SetActive(false);
            if (campaignScreen != null) Destroy(campaignScreen.gameObject);
            BuildCampaign();
            campaignScreen.gameObject.SetActive(true);
        }

        /// <summary>The built-in quests as castles along a road: each unlocks when the one before it is cleared.</summary>
        void BuildCampaign()
        {
            campaignScreen = UIFactory.Rect(root, "Campaign");
            UIFactory.Stretch(campaignScreen);
            var frame = UIFactory.Panel(campaignScreen, "Frame", new Color(0.11f, 0.09f, 0.14f, 0.92f));
            if (phone) UIFactory.Stretch(frame, 12, 12, 12, 12);
            else UIFactory.Anchor(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-620, -300), new Vector2(620, 300));
            var header = UIFactory.Rect(frame, "Header");
            UIFactory.Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -56), new Vector2(-16, -8));
            UIFactory.HLayout(header, 10, 0, false, TextAnchor.MiddleLeft);
            UIFactory.Button(header, "◀ " + L10n.T("Back"), ShowTitle, 100, 40);
            UIFactory.Label(header, L10n.T("The Realm's Road"), phone ? 22 : 26, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 40, true);

            var host = UIFactory.Rect(frame, "RoadHost");
            UIFactory.Anchor(host, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 16), new Vector2(-16, -64));
            var scroll = UIFactory.ScrollView(host, "Road", out var content, false, true, Color.clear);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>());
            UIFactory.HLayout(content, 0, 24, false, TextAnchor.MiddleLeft);
            UIFactory.Fitter(content, false, true);

            var all = LevelStorage.ListLevels();
            var campaign = new List<LevelFileInfo>();
            foreach (var l in all) if (l.builtIn) campaign.Add(l);
            campaign.Sort((a, b) => a.campaignOrder != b.campaignOrder ? a.campaignOrder.CompareTo(b.campaignOrder) : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            var castle = ObjectCatalog.Get("tower_cap_slate");
            var gate = ObjectCatalog.Get("gate_deco");
            bool previousCleared = true;
            for (int i = 0; i < campaign.Count; i++)
            {
                var info = campaign[i];
                var stats = LevelStatsStorage.Load(info.id);
                bool cleared = stats.completions > 0 || stats.checkpointCompletions > 0;
                bool unlocked = i == 0 || previousCleared;
                previousCleared = cleared;

                var node = UIFactory.Rect(content, "Node");
                UIFactory.Layout(node.gameObject, 190, -1, -1, 1);
                UIFactory.VLayout(node, 6, 6, true, true, TextAnchor.MiddleCenter);
                // road segment drawn as a thick bar behind the node row
                var road = UIFactory.Panel(node, "Road", new Color(0.55f, 0.45f, 0.3f, 0.6f));
                UIFactory.Layout(road.gameObject, -1, 10);
                var icon = UIFactory.Icon(node, SpriteLibrary.ForObject(cleared ? castle : gate), 88, unlocked ? Color.white : new Color(0.35f, 0.35f, 0.4f, 1f));
                var name = UIFactory.Label(node, (i + 1) + ". " + info.name, 15, TextAnchor.MiddleCenter, unlocked ? UIFactory.Accent : UIFactory.TextDim, -1, 40, true);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                string state = !unlocked ? L10n.T("Locked: clear the quest before it") : (cleared ? L10n.T("Cleared") + (stats.completions > 0 ? " · " + L10n.T("Champion seal") : "") : (stats.bestProgress > 0f ? L10n.T("Best") + " " + (stats.bestProgress * 100f).ToString("0") + "%" : L10n.T("Unexplored")));
                UIFactory.Label(node, state + "\n" + LevelRating.Name(info.difficultyTag) + " · " + info.LengthTag, 11, TextAnchor.MiddleCenter, UIFactory.TextDim, -1, 34);
                if (stats.completions > 0)
                {
                    var sealRow = UIFactory.Row(node, 34, 0, TextAnchor.MiddleCenter);
                    var seal = UIFactory.Icon(sealRow, PlaceholderSpriteFactory.Circle(), 34, new Color(0.62f, 0.12f, 0.1f, 1f));
                    var crest = UIFactory.Icon(seal.transform, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), 20);
                    UIFactory.Anchor(crest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, -10), new Vector2(10, 10));
                    crest.color = new Color(1f, 0.85f, 0.7f, 0.9f);
                }
                else UIFactory.Spacer(node, 34);
                if (unlocked)
                {
                    var lvl = info;
                    var br = UIFactory.Row(node, 34, 4);
                    UIFactory.Button(br, "▶ " + L10n.T("Ride"), () => Launch(lvl, Difficulty.Checkpoints), -1, 32, UIFactory.Good, 12);
                    UIFactory.Button(br, L10n.T("Champion"), () => Launch(lvl, Difficulty.Champion), -1, 32, UIFactory.ButtonActive, 12);
                }
                else UIFactory.Label(node, "🔒", 22, TextAnchor.MiddleCenter, UIFactory.TextDim, -1, 34);
            }
            if (campaign.Count == 0) UIFactory.Label(content, L10n.T("No campaign quests are shipped yet."), 14, TextAnchor.MiddleLeft, UIFactory.TextDim, 400, 40);
        }

        void ShowTitle()
        {
            titleScreen.gameObject.SetActive(true);
            levelScreen.gameObject.SetActive(false);
            if (heraldryScreen != null) heraldryScreen.gameObject.SetActive(false);
            if (optionsScreen != null) optionsScreen.gameObject.SetActive(false);
            if (campaignScreen != null) campaignScreen.gameObject.SetActive(false);
            titleCrest.sprite = PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary);
        }

        void ShowLevelSelect()
        {
            if (campaignScreen != null) campaignScreen.gameObject.SetActive(false);
            titleScreen.gameObject.SetActive(false);
            levelScreen.gameObject.SetActive(true);
            if (heraldryScreen != null) heraldryScreen.gameObject.SetActive(false);
            if (optionsScreen != null) optionsScreen.gameObject.SetActive(false);
            RefreshLevels();
        }

        // ---- heraldry -----------------------------------------------------------

        void ShowOptions()
        {
            if (campaignScreen != null) campaignScreen.gameObject.SetActive(false);
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

            if (Haptics.Supported)
            {
                UIFactory.Toggle(frame, "Vibration on jumps, deaths and waystones", Haptics.Enabled, v => Haptics.Enabled = v, 30);
            }
            UIFactory.Button(frame, L10n.T("Language") + ": " + L10n.LanguageName + "  ▸", () =>
            {
                L10n.CycleLanguage();
                reopenOptions = true;
                app.ShowMenu();
            }, -1, 34, null, 13);
            UIFactory.Label(frame, "Add a language by dropping <code>.json into Assets/Geodashy/Resources/Strings (see es.json).", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 18);

            UIFactory.SectionHeader(frame, L10n.T("Controls"));
            jumpKeyButton = UIFactory.Button(frame, "", () =>
            {
                remapping = true;
                UIFactory.SetButtonLabel(jumpKeyButton, L10n.T("Press the new jump key… (Esc cancels)"));
            }, -1, 34, UIFactory.ButtonActive, 13);
            RefreshJumpKeyLabel();
            UIFactory.Label(frame, "Space, Up and W always jump too. Any gamepad face button, trigger or shoulder jumps; Start pauses. Menus follow the gamepad d-pad.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);

            UIFactory.SectionHeader(frame, L10n.T("Accessibility"));
            UIFactory.Button(frame, L10n.T("Edge colours") + ": " + Accessibility.PaletteNames[Accessibility.Palette] + "  ▸", () =>
            {
                Accessibility.Palette = (Accessibility.Palette + 1) % Accessibility.PaletteNames.Length;
                reopenOptions = true;
                app.ShowMenu();
            }, -1, 34, null, 13);
            UIFactory.Toggle(frame, L10n.T("Reduce flashing and camera shake"), Accessibility.ReduceFlash, v => Accessibility.ReduceFlash = v, 30);
            UIFactory.Toggle(frame, L10n.T("Larger hitbox overlays in the editor"), Accessibility.BigOverlays, v => Accessibility.BigOverlays = v, 30);
            UIFactory.Toggle(frame, L10n.T("Hold-to-jump assist (cart keeps hopping, griffin keeps flapping)"), Accessibility.HoldAssist, v => Accessibility.HoldAssist = v, 30);

            UIFactory.SectionHeader(frame, L10n.T("Captures and performance"));
            UIFactory.Toggle(frame, L10n.T("Record the last 5 seconds for clips (costs some performance)"), GameRunner.RecordClips, v => GameRunner.RecordClips = v, 30);
            UIFactory.Toggle(frame, L10n.T("Performance overlay in play"), GameRunner.PerfHud, v => GameRunner.PerfHud = v, 30);
            var capRow = UIFactory.Row(frame, 30, 6);
            UIFactory.Label(capRow, L10n.T("Captures folder") + ": " + CaptureRecorder.CapturesFolder, 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 30);
            if (!Application.isMobilePlatform) UIFactory.Button(capRow, L10n.T("Open"), () => Application.OpenURL("file://" + CaptureRecorder.CapturesFolder), 70, 28, null, 12);
            UIFactory.Label(frame, "Pause during a run for Screenshot and Save 5 s clip; both are stamped with your crest and the quest name.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 18);

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
            if (campaignScreen != null) campaignScreen.gameObject.SetActive(false);
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

        bool PassesFilter(LevelFileInfo i, LevelStats stats)
        {
            switch (filterIndex)
            {
                case 0: return true;
                case 1: return stats.completions == 0 && stats.checkpointCompletions == 0;
                case 2: return stats.completions > 0 || stats.checkpointCompletions > 0;
                case 9: return string.IsNullOrEmpty(i.difficultyTag);
                default: return LevelRating.Name(i.difficultyTag) == FilterNames[filterIndex];
            }
        }

        void SortLevels(List<LevelFileInfo> list)
        {
            switch (sortIndex)
            {
                case 1: list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase)); break;
                case 2: list.Sort((a, b) => { int c = LevelRating.Rank(a.difficultyTag).CompareTo(LevelRating.Rank(b.difficultyTag)); return c != 0 ? c : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase); }); break;
                case 3: list.Sort((a, b) => a.lengthSeconds.CompareTo(b.lengthSeconds)); break;
                case 4: list.Sort((a, b) => b.modified.CompareTo(a.modified)); break;
            }
        }

        void RefreshLevels()
        {
            var all = LevelStorage.ListLevels();
            var statsById = new Dictionary<string, LevelStats>();
            levels = new List<LevelFileInfo>();
            foreach (var i in all)
            {
                var st = LevelStatsStorage.Load(i.id);
                statsById[i.id] = st;
                if (PassesFilter(i, st)) levels.Add(i);
            }
            SortLevels(levels);
            if (filterButton != null) UIFactory.SetButtonLabel(filterButton, "Filter: " + FilterNames[filterIndex] + " ▸");
            if (sortButton != null) UIFactory.SetButtonLabel(sortButton, "Sort: " + SortNames[sortIndex] + " ▸");
            foreach (Transform child in listContent) Destroy(child.gameObject);
            rowButtons.Clear();
            if (levels.Count == 0) UIFactory.Label(listContent, all.Count == 0 ? "No quests yet. Press + New quest to build one." : "Nothing matches this filter.", 14, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 40);
            foreach (var info in levels)
            {
                var i = info;
                var stats = statsById[i.id];
                string status = stats.completions > 0 ? "✔ cleared" : (stats.bestProgress > 0f ? (stats.bestProgress * 100f).ToString("0") + "%" : "new");
                if (stats.fullLoot) status += " · all loot";
                string tags = (string.IsNullOrEmpty(i.difficultyTag) ? "" : LevelRating.Name(i.difficultyTag) + " · ") + i.LengthTag;
                var b = UIFactory.Button(listContent, (i.builtIn ? "★ " : "") + i.name + "\n<size=11>" + (string.IsNullOrEmpty(i.author) ? "unknown author" : i.author) + " · " + status + "   [" + tags + "]</size>",
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
            UIFactory.Label(col, LevelRating.Name(info.difficultyTag) + " · " + info.LengthTag + " (" + Mathf.RoundToInt(info.lengthSeconds) + "s)", 12, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 20);
            UIFactory.SectionHeader(detailPane, "Your record");
            string best = stats.completions > 0 ? "Champion: cleared " + stats.completions + "×" : (stats.bestProgress > 0f ? "Champion best " + (stats.bestProgress * 100f).ToString("0.0") + "%" : "Champion: not attempted");
            UIFactory.Label(detailPane, best + "  ·  " + stats.attempts + " attempts" + (stats.checkpointCompletions > 0 ? "  ·  Checkpoints: cleared " + stats.checkpointCompletions + "×" : "") + (stats.fullLoot ? "  ·  all loot gathered" : ""), 13, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 22);
            BuildDeathChart(detailPane, stats);
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

        /// <summary>Deaths per 10% of the level as a small bar chart, with the average survival and the top killer.</summary>
        void BuildDeathChart(Transform parent, LevelStats stats)
        {
            if (stats.deaths.Count == 0)
            {
                UIFactory.Label(parent, "No Champion runs recorded yet: the death chart appears after the first attempts.", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 30);
                return;
            }
            var hist = stats.DeathHistogram(10);
            int max = 1;
            foreach (var h in hist) max = Mathf.Max(max, h);
            var chart = UIFactory.Rect(parent, "DeathChart");
            UIFactory.Layout(chart.gameObject, -1, 64);
            UIFactory.HLayout(chart, 3, 0, true, TextAnchor.LowerLeft);
            chart.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
            chart.GetComponent<HorizontalLayoutGroup>().childControlHeight = false;
            for (int i = 0; i < hist.Length; i++)
            {
                var barHost = UIFactory.Rect(chart, "Bar" + i);
                UIFactory.Layout(barHost.gameObject, -1, 64, 1);
                barHost.sizeDelta = new Vector2(0, 64);
                var track = UIFactory.Panel(barHost, "Track", new Color(1, 1, 1, 0.06f));
                UIFactory.Anchor(track, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 14), new Vector2(0, 0));
                float frac = hist[i] / (float)max;
                var bar = UIFactory.Panel(barHost, "Fill", Color.Lerp(PlayHUD.SessionDeathColor, PlayHUD.AllTimeDeathColor, frac));
                UIFactory.Anchor(bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 14), new Vector2(-1, 14 + 48f * frac));
                bar.GetComponent<Image>().raycastTarget = false;
                var lbl = UIFactory.Label(barHost, (i * 10) + "%", 9, TextAnchor.MiddleCenter, UIFactory.TextDim);
                UIFactory.Anchor(lbl.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 13));
                if (hist[i] > 0)
                {
                    var count = UIFactory.Label(barHost, hist[i].ToString(), 9, TextAnchor.LowerCenter, UIFactory.TextColor);
                    UIFactory.Anchor(count.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 14 + 48f * frac), new Vector2(0, 28 + 48f * frac));
                }
            }
            string killer = stats.TopKiller(out int kills);
            string summary = "Deaths per 10% of the quest (" + stats.deaths.Count + " recorded)  ·  average run reaches " + (stats.AverageDeathProgress() * 100f).ToString("0") + "%";
            if (!string.IsNullOrEmpty(killer)) summary += "  ·  most often slain by " + killer + " (" + kills + "×)";
            UIFactory.Label(parent, summary, 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 32);
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

        void RefreshJumpKeyLabel()
        {
            if (jumpKeyButton != null) UIFactory.SetButtonLabel(jumpKeyButton, L10n.T("Jump key") + ": " + InputConfig.JumpKeyName + "   (" + L10n.T("click to rebind") + ")");
        }

        void Update()
        {
            // slow camera drift so the parallax layers move behind the menu
            drift += Time.deltaTime * 2.5f;
            cam.transform.position = new Vector3(drift, 4f + Mathf.Sin(drift * 0.15f) * 0.5f, -10f);
            if (remapping)
            {
                var k = InputConfig.PollNewKey();
                var kbd = Keyboard.current;
                if (kbd != null && kbd.escapeKey.wasPressedThisFrame)
                {
                    remapping = false;
                    RefreshJumpKeyLabel();
                    return;
                }
                if (k != Key.None)
                {
                    InputConfig.JumpKey = k;
                    remapping = false;
                    RefreshJumpKeyLabel();
                }
                return;
            }
            var kb = Keyboard.current;
            bool onSub = (levelScreen != null && levelScreen.gameObject.activeSelf) || (campaignScreen != null && campaignScreen.gameObject.activeSelf) || (optionsScreen != null && optionsScreen.gameObject.activeSelf) || (heraldryScreen != null && heraldryScreen.gameObject.activeSelf);
            if (kb != null && kb[Key.Escape].wasPressedThisFrame && onSub) ShowTitle();
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame && onSub) ShowTitle();
        }
    }
}
