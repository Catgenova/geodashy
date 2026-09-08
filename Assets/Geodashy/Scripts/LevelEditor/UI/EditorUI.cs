using System;
using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Root of the editor interface: docks, panels, modal dialogs, popups and toasts.</summary>
    public class EditorUI : MonoBehaviour
    {
        public static EditorUI Instance { get; private set; }

        public const float TopBarHeight = 56f;
        public const float BottomDockHeight = 260f;
        public const float LeftDockWidth = 250f;
        public const float RightDockWidth = 360f;
        public const string UIScalePref = "geodashy.uiScale";
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        /// <summary>Interface scale multiplier (0.7 - 2). Persisted in PlayerPrefs and shared with the play HUD.</summary>
        public static float UIScale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(UIScalePref, 1f), 0.7f, 2f);
            set => PlayerPrefs.SetFloat(UIScalePref, Mathf.Clamp(value, 0.7f, 2f));
        }

        public static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution / UIScale;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        CanvasScaler canvasScaler;
        public event Action UIScaleChanged;

        public void SetUIScale(float scale)
        {
            UIScale = scale;
            PlayerPrefs.Save();
            if (canvasScaler != null) ConfigureScaler(canvasScaler);
            UIScaleChanged?.Invoke();
        }

        public LevelEditor editor;
        public Canvas canvas;
        public RectTransform root;
        public RectTransform modalLayer;
        public RectTransform popupLayer;
        public RectTransform toastLayer;
        public RectTransform bottomDock;
        public RectTransform rightDock;
        public RectTransform leftDock;

        public TopBar topBar;
        public PalettePanel palettePanel;
        public EditPanel editPanel;
        public DeletePanel deletePanel;
        public ViewPanel viewPanel;
        public PropertiesPanel propertiesPanel;

        readonly List<GameObject> modals = new List<GameObject>();
        GameObject popup;
        Text toastText;
        float toastTimer;
        bool playMode;

        public bool ModalOpen => modals.Count > 0;

        public bool PointerOverUI
        {
            get
            {
                if (EventSystem.current == null) return false;
                return EventSystem.current.IsPointerOverGameObject();
            }
        }

        public bool IsTyping
        {
            get
            {
                var es = EventSystem.current;
                if (es == null || es.currentSelectedGameObject == null) return false;
                var f = es.currentSelectedGameObject.GetComponent<InputField>();
                return f != null && f.isFocused;
            }
        }

        public static EditorUI Create(LevelEditor editor)
        {
            var go = new GameObject("Editor UI");
            go.transform.SetParent(editor.transform, false);
            var ui = go.AddComponent<EditorUI>();
            ui.editor = editor;
            ui.Build();
            return ui;
        }

        void Awake()
        {
            Instance = this;
        }

        void Build()
        {
            EnsureEventSystem();

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
            ConfigureScaler(canvasScaler);
            gameObject.AddComponent<GraphicRaycaster>();

            root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);

            // docks -----------------------------------------------------------
            var top = UIFactory.Panel(root, "TopBar", UIFactory.PanelBg);
            UIFactory.Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -TopBarHeight), new Vector2(0, 0));
            topBar = TopBar.Create(this, top);

            bottomDock = UIFactory.Panel(root, "BottomDock", UIFactory.PanelBg);
            UIFactory.Anchor(bottomDock, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, BottomDockHeight));

            leftDock = UIFactory.Panel(root, "LeftDock", UIFactory.PanelBg);
            UIFactory.Anchor(leftDock, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, BottomDockHeight), new Vector2(LeftDockWidth, -TopBarHeight));

            rightDock = UIFactory.Panel(root, "RightDock", UIFactory.PanelBg);
            UIFactory.Anchor(rightDock, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-RightDockWidth, BottomDockHeight), new Vector2(0, -TopBarHeight));

            palettePanel = PalettePanel.Create(this, bottomDock);
            editPanel = EditPanel.Create(this, bottomDock);
            deletePanel = DeletePanel.Create(this, bottomDock);
            viewPanel = ViewPanel.Create(this, leftDock);
            propertiesPanel = PropertiesPanel.Create(this, rightDock);

            modalLayer = UIFactory.Rect(transform, "Modals");
            UIFactory.Stretch(modalLayer);
            popupLayer = UIFactory.Rect(transform, "Popups");
            UIFactory.Stretch(popupLayer);
            toastLayer = UIFactory.Rect(transform, "Toasts");
            UIFactory.Stretch(toastLayer);
            var toastBg = UIFactory.Panel(toastLayer, "Toast", new Color(0, 0, 0, 0.75f));
            UIFactory.Anchor(toastBg, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-260, BottomDockHeight + 16), new Vector2(260, BottomDockHeight + 56));
            toastBg.GetComponent<Image>().raycastTarget = false;
            toastText = UIFactory.Label(toastBg, "", 16, TextAnchor.MiddleCenter, UIFactory.Accent);
            UIFactory.Stretch(toastText.rectTransform, 8, 4, 8, 4);
            toastBg.gameObject.SetActive(false);

            editor.ModeChanged += OnModeChanged;
            editor.SelectionChanged += OnSelectionChanged;
            OnModeChanged();
            OnSelectionChanged();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = es.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void OnModeChanged()
        {
            palettePanel.gameObject.SetActive(editor.Mode == EditorMode.Build);
            editPanel.gameObject.SetActive(editor.Mode == EditorMode.Edit);
            deletePanel.gameObject.SetActive(editor.Mode == EditorMode.Delete);
            topBar.RefreshModeButtons();
        }

        void OnSelectionChanged()
        {
            rightDock.gameObject.SetActive(editor.selection.Count > 0 && !playMode);
        }

        void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastText.transform.parent.gameObject.SetActive(false);
            }
            topBar?.Tick();
            viewPanel?.Tick();
        }

        // ---- public helpers ---------------------------------------------------

        public void Toast(string message, float seconds = 2.2f)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.transform.parent.gameObject.SetActive(true);
            toastTimer = seconds;
        }

        public void SetHidden(bool hidden)
        {
            root.gameObject.SetActive(!hidden);
            if (hidden) ClosePopup();
        }

        public void SetPlayMode(bool playing)
        {
            playMode = playing;
            root.gameObject.SetActive(!playing && !editor.uiHidden);
            ClosePopup();
            while (modals.Count > 0) CloseTopModal();
        }

        public void OpenHelp() => HelpDialog.Open(this);
        public void OpenFileDialog() => FileDialog.Open(this, editor);
        public void OpenSettings() => LevelSettingsDialog.Open(this, editor);

        public void PromptSaveAs()
        {
            Prompt("Save As", "Name for the copy:", editor.level.name, name => editor.SaveAs(name));
        }

        // ---- modals --------------------------------------------------------------

        /// <summary>Creates a modal window and returns its content column.</summary>
        public RectTransform OpenModal(string title, float width, float height, bool scroll = false)
        {
            var backdrop = UIFactory.Panel(modalLayer, "Modal " + title, new Color(0, 0, 0, 0.55f));
            var window = UIFactory.Panel(backdrop, "Window", UIFactory.PanelBg2);
            UIFactory.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-width / 2f, -height / 2f), new Vector2(width / 2f, height / 2f));
            var header = UIFactory.Panel(window, "Header", UIFactory.PanelBg3);
            UIFactory.Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, 0));
            var t = UIFactory.Label(header, title, 18, TextAnchor.MiddleLeft, UIFactory.Accent, -1, -1, true);
            UIFactory.Stretch(t.rectTransform, 14, 0, 50, 0);
            var close = UIFactory.Button(header, "✕", () => CloseModal(backdrop.gameObject), 36, 30, UIFactory.Danger, 16);
            var crt = close.GetComponent<RectTransform>();
            UIFactory.Anchor(crt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-42, -15), new Vector2(-6, 15));

            RectTransform content;
            if (scroll)
            {
                var scrollRt = UIFactory.Rect(window, "ScrollHost");
                UIFactory.Anchor(scrollRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -44));
                UIFactory.ScrollView(scrollRt, "Scroll", out content, true, false, Color.clear);
                var sr = scrollRt.GetComponentInChildren<ScrollRect>();
                UIFactory.Stretch(sr.GetComponent<RectTransform>());
                UIFactory.VLayout(content, 6, 8);
                UIFactory.Fitter(content, true, false);
            }
            else
            {
                content = UIFactory.Rect(window, "Content");
                UIFactory.Anchor(content, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -44));
                UIFactory.VLayout(content, 6, 8);
            }
            modals.Add(backdrop.gameObject);
            return content;
        }

        public void CloseModal(GameObject modal)
        {
            modals.Remove(modal);
            Destroy(modal);
        }

        public void CloseTopModal()
        {
            if (modals.Count == 0) return;
            var m = modals[modals.Count - 1];
            modals.RemoveAt(modals.Count - 1);
            Destroy(m);
        }

        /// <summary>The backdrop GameObject of the most recently opened modal (used by dialogs to close themselves).</summary>
        public GameObject TopModal => modals.Count > 0 ? modals[modals.Count - 1] : null;

        public void Confirm(string title, string message, Action onYes, string yesLabel = "OK")
        {
            var content = OpenModal(title, 460, 190);
            var modal = TopModal;
            var t = UIFactory.Label(content, message, 15, TextAnchor.UpperLeft, UIFactory.TextColor, -1, 70);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var row = UIFactory.Row(content, 36, 8, TextAnchor.MiddleRight);
            UIFactory.Button(row, "Cancel", () => CloseModal(modal), 110, 34);
            UIFactory.Button(row, yesLabel, () =>
            {
                CloseModal(modal);
                onYes?.Invoke();
            }, 110, 34, UIFactory.ButtonActive);
        }

        public void Prompt(string title, string message, string initial, Action<string> onOk)
        {
            var content = OpenModal(title, 460, 200);
            var modal = TopModal;
            UIFactory.Label(content, message, 15, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 30);
            var input = UIFactory.Input(content, "", initial, null, -1, 32);
            var row = UIFactory.Row(content, 36, 8, TextAnchor.MiddleRight);
            UIFactory.Button(row, "Cancel", () => CloseModal(modal), 110, 34);
            UIFactory.Button(row, "OK", () =>
            {
                var v = input.text;
                CloseModal(modal);
                onOk?.Invoke(v);
            }, 110, 34, UIFactory.ButtonActive);
            input.ActivateInputField();
        }

        // ---- popups -------------------------------------------------------------------

        public void ClosePopup()
        {
            if (popup != null) Destroy(popup);
            popup = null;
        }

        RectTransform CreatePopupFrame(RectTransform anchor, float width, float height)
        {
            ClosePopup();
            var backdrop = UIFactory.Panel(popupLayer, "PopupBackdrop", Color.clear);
            var bb = backdrop.gameObject.AddComponent<Button>();
            bb.transition = Selectable.Transition.None;
            bb.onClick.AddListener(ClosePopup);
            popup = backdrop.gameObject;

            var frame = UIFactory.Panel(backdrop, "Popup", UIFactory.PanelBg3);
            // position below the anchor (or above if there is no room)
            var canvasRt = canvas.GetComponent<RectTransform>();
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector2 bl, tl;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out bl);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, RectTransformUtility.WorldToScreenPoint(null, corners[1]), null, out tl);
            float canvasH = canvasRt.rect.height, canvasW = canvasRt.rect.width;
            float x = Mathf.Clamp(bl.x, -canvasW / 2f, canvasW / 2f - width);
            float top = bl.y - 2f;
            if (top - height < -canvasH / 2f) top = tl.y + height + 2f;
            top = Mathf.Min(top, canvasH / 2f);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0, 1);
            frame.anchoredPosition = new Vector2(x, top);
            frame.sizeDelta = new Vector2(width, height);
            return frame;
        }

        public void ShowDropdown(RectTransform anchor, string[] options, int current, Action<int> onPick)
        {
            float rowH = 28f;
            float height = Mathf.Min(340f, options.Length * (rowH + 2f) + 8f);
            float width = Mathf.Max(anchor.rect.width, 160f);
            var frame = CreatePopupFrame(anchor, width, height);
            var scroll = UIFactory.ScrollView(frame, "List", out var content, true, false, Color.clear);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>(), 2, 2, 2, 2);
            UIFactory.VLayout(content, 2, 2);
            UIFactory.Fitter(content, true, false);
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var b = UIFactory.Button(content, options[i], () =>
                {
                    ClosePopup();
                    onPick?.Invoke(idx);
                }, -1, rowH, idx == current ? UIFactory.ButtonActive : UIFactory.ButtonBg, 13);
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            }
            // scroll to current
            if (options.Length > 0) scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(current / (float)options.Length);
        }

        public void ShowColorPicker(RectTransform anchor, Color current, Action<Color> onPick)
        {
            var frame = CreatePopupFrame(anchor, 300, 300);
            var col = UIFactory.Rect(frame, "Col");
            UIFactory.Stretch(col, 6, 6, 6, 6);
            UIFactory.VLayout(col, 4, 4);

            var preview = UIFactory.Swatch(col, current, 34);
            UIFactory.Layout(preview.gameObject, -1, 34, 1);
            Color c = current;

            Slider r = null, g = null, b = null, a = null;
            void Push()
            {
                preview.color = c;
                onPick?.Invoke(c);
            }
            Slider Make(string name, float v, Action<float> set)
            {
                var row = UIFactory.Row(col, 22, 6);
                UIFactory.Label(row, name, 12, TextAnchor.MiddleLeft, UIFactory.TextDim, 16, 22);
                var s = UIFactory.Slider(row, 0, 1, v, x =>
                {
                    set(x);
                    Push();
                }, false, 22);
                return s;
            }
            r = Make("R", c.r, v => c.r = v);
            g = Make("G", c.g, v => c.g = v);
            b = Make("B", c.b, v => c.b = v);
            a = Make("A", c.a, v => c.a = v);

            UIFactory.SectionHeader(col, "Presets");
            var gridRt = UIFactory.Rect(col, "Presets");
            UIFactory.Grid(gridRt, new Vector2(26, 26), new Vector2(4, 4), 0);
            UIFactory.Layout(gridRt.gameObject, -1, 110, 1);
            string[] presets =
            {
                "ffffff", "d0d0d0", "8a8a8a", "3a3a3a", "000000", "e0b64a", "b8302c", "ff8a2a", "ffd23a",
                "5fff7a", "2f8a3a", "6ad4ff", "3fa8ff", "2c4fb8", "b070ff", "6a3cff", "ff7fd1", "a9743d",
                "5a3a1a", "8f8c94", "4d4a5a", "a9dcf5", "d9b97c", "6fbf3a", "ff4a4a", "1e2447", "5a4f8a"
            };
            foreach (var hex in presets)
            {
                var pc = ObjectCatalog.Hex(hex);
                var sw = UIFactory.Swatch(gridRt, pc, 26);
                var bt = sw.gameObject.AddComponent<Button>();
                bt.targetGraphic = sw;
                bt.onClick.AddListener(() =>
                {
                    c = new Color(pc.r, pc.g, pc.b, c.a);
                    r.SetValueWithoutNotify(c.r);
                    g.SetValueWithoutNotify(c.g);
                    b.SetValueWithoutNotify(c.b);
                    Push();
                });
            }
        }
    }
}
