using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Editing;
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
        public const string PhoneUIScalePref = "geodashy.uiScale.phone";
        public const string PhoneLayoutPref = "geodashy.phoneLayout";
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        // phone (landscape) layout: bigger controls, collapsible docks, overlay drawers
        public const float PhoneTopBarHeight = 52f;
        public const float PhoneDockHeight = 150f;
        public const float PhoneLeftDockWidth = 300f;
        public const float PhoneRightDockWidth = 340f;

        /// <summary>-1 = automatic (phone layout on handhelds), 0 = desktop layout, 1 = phone layout.</summary>
        public static int PhoneLayoutSetting
        {
            get => PlayerPrefs.GetInt(PhoneLayoutPref, -1);
            set
            {
                PlayerPrefs.SetInt(PhoneLayoutPref, Mathf.Clamp(value, -1, 1));
                PlayerPrefs.Save();
            }
        }

        /// <summary>True when the interface should use the compact touch layout.</summary>
        public static bool PhoneLayout
        {
            get
            {
                int s = PhoneLayoutSetting;
                return s < 0 ? Application.isMobilePlatform : s == 1;
            }
        }

        public static string PhoneLayoutName
        {
            get
            {
                int s = PhoneLayoutSetting;
                if (s < 0) return "Auto (" + (Application.isMobilePlatform ? "phone" : "desktop") + ")";
                return s == 1 ? "Phone" : "Desktop";
            }
        }

        /// <summary>Cycles Auto → Phone → Desktop → Auto.</summary>
        public static void CyclePhoneLayout()
        {
            int s = PhoneLayoutSetting;
            PhoneLayoutSetting = s < 0 ? 1 : (s == 1 ? 0 : -1);
        }

        /// <summary>
        /// Interface scale multiplier, persisted in PlayerPrefs and shared with the play HUD and menu.
        /// The desktop layout allows 0.7 - 2; the phone layout keeps its own value (default 2, range 1.4 - 3)
        /// because touch targets need to be physically larger.
        /// </summary>
        public static float UIScale
        {
            get => PhoneLayout ? Mathf.Clamp(PlayerPrefs.GetFloat(PhoneUIScalePref, 2f), 1.4f, 3f) : Mathf.Clamp(PlayerPrefs.GetFloat(UIScalePref, 1f), 0.7f, 2f);
            set
            {
                if (PhoneLayout) PlayerPrefs.SetFloat(PhoneUIScalePref, Mathf.Clamp(value, 1.4f, 3f));
                else PlayerPrefs.SetFloat(UIScalePref, Mathf.Clamp(value, 0.7f, 2f));
            }
        }

        public static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution / UIScale;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // phones: match height so the layout keeps its vertical budget on any aspect ratio and wide screens just gain room
            scaler.matchWidthOrHeight = PhoneLayout ? 1f : 0.5f;
        }

        /// <summary>Anchors a full-screen rect to the display's safe area (notches, rounded corners). No-op on desktop screens.</summary>
        public static void ApplySafeArea(RectTransform rt)
        {
            var sa = Screen.safeArea;
            float w = Screen.width, h = Screen.height;
            if (w <= 0f || h <= 0f || sa.width <= 0f || sa.height <= 0f) return;
            rt.anchorMin = new Vector2(Mathf.Clamp01(sa.xMin / w), Mathf.Clamp01(sa.yMin / h));
            rt.anchorMax = new Vector2(Mathf.Clamp01(sa.xMax / w), Mathf.Clamp01(sa.yMax / h));
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
        public TimelineStrip timeline;
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
        // status strip, hint bar, stacked notifications and tooltips
        RectTransform statusBar;
        Text statusText, hintText;
        float statusTimer;
        float stripHeight;
        class Note { public RectTransform rt; public float life; public CanvasGroup group; }
        readonly List<Note> notes = new List<Note>();
        RectTransform noteStack;
        RectTransform tooltipRt;
        Text tooltipText;
        Tooltip tooltipOwner;
        readonly Dictionary<GameObject, Action> modalPrimary = new Dictionary<GameObject, Action>();
        public const string HintsPref = "geodashy.hintBar";
        public static bool HintsEnabled
        {
            get => PlayerPrefs.GetInt(HintsPref, 1) == 1;
            set => PlayerPrefs.SetInt(HintsPref, value ? 1 : 0);
        }
        bool phone;
        bool viewDrawerOpen, propsDrawerOpen, dockCollapsed;
        float topHeight, dockHeight, leftWidth, rightWidth;

        /// <summary>True when this interface was built with the phone layout.</summary>
        public bool IsPhone => phone;
        public bool ViewDrawerOpen => viewDrawerOpen;
        public bool PropsDrawerOpen => propsDrawerOpen;
        public bool DockCollapsed => dockCollapsed;
        /// <summary>Height of the bottom dock in canvas units (0 while collapsed).</summary>
        public float DockHeight => dockCollapsed ? 0f : dockHeight;

        public bool ModalOpen => modals.Count > 0;

        /// <summary>
        /// True when the active pointer (the touch if one is down or just ended, else the mouse) is over interface.
        /// Uses a position raycast rather than IsPointerOverGameObject, which is unreliable for touches and for the
        /// phantom mouse device Android reports.
        /// </summary>
        public bool PointerOverUI => IsScreenPointOverUI(ActivePointerPosition(), false);

        static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        /// <summary>Screen position of whichever pointer the player is actually using.</summary>
        public static Vector2 ActivePointerPosition()
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && (TouchActive(touch) || UnityEngine.InputSystem.Mouse.current == null || Application.isMobilePlatform)) return touch.primaryTouch.position.ReadValue();
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : new Vector2(-1000, -1000);
        }

        /// <summary>A finger is down, went down or lifted this frame.</summary>
        public static bool TouchActive(UnityEngine.InputSystem.Touchscreen touch)
        {
            if (touch == null) return false;
            var press = touch.primaryTouch.press;
            return press.isPressed || press.wasPressedThisFrame || press.wasReleasedThisFrame;
        }

        /// <summary>
        /// Raycasts the UI at a screen position. With interactiveOnly, only buttons, toggles, sliders, inputs and
        /// scroll views count, so plain HUD text never swallows a tap.
        /// </summary>
        public static bool IsScreenPointOverUI(Vector2 screenPos, bool interactiveOnly)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var ped = new PointerEventData(es) { position = screenPos };
            raycastResults.Clear();
            es.RaycastAll(ped, raycastResults);
            if (!interactiveOnly) return raycastResults.Count > 0;
            for (int i = 0; i < raycastResults.Count; i++)
            {
                var go = raycastResults[i].gameObject;
                if (go == null) continue;
                if (go.GetComponentInParent<Selectable>() != null || go.GetComponentInParent<ScrollRect>() != null) return true;
            }
            return false;
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

            phone = PhoneLayout;
            topHeight = phone ? PhoneTopBarHeight : TopBarHeight;
            dockHeight = phone ? PhoneDockHeight : BottomDockHeight;
            leftWidth = phone ? PhoneLeftDockWidth : LeftDockWidth;
            rightWidth = phone ? PhoneRightDockWidth : RightDockWidth;

            root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);
            if (phone) ApplySafeArea(root);

            // docks -----------------------------------------------------------
            stripHeight = phone ? 20f : 24f;
            var top = UIFactory.Frame(root, "TopBar");
            UIFactory.Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -topHeight), new Vector2(0, 0));
            topBar = TopBar.Create(this, top);

            // timeline strip directly under the top bar; the side docks start below it
            float stripH = phone ? TimelineStrip.PhoneHeight : TimelineStrip.Height;
            var strip = UIFactory.Panel(root, "Timeline", UIFactory.PanelBg2);
            UIFactory.Anchor(strip, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -topHeight - stripH), new Vector2(0, -topHeight));
            timeline = TimelineStrip.Create(this, strip);
            topHeight += stripH;

            bottomDock = UIFactory.Frame(root, "BottomDock");
            UIFactory.Anchor(bottomDock, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, dockHeight));
            BuildStatusBar();

            leftDock = UIFactory.Frame(root, "LeftDock");
            UIFactory.Anchor(leftDock, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, dockHeight + stripHeight), new Vector2(leftWidth, -topHeight));

            rightDock = UIFactory.Frame(root, "RightDock");
            UIFactory.Anchor(rightDock, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-rightWidth, dockHeight + stripHeight), new Vector2(0, -topHeight));

            palettePanel = PalettePanel.Create(this, bottomDock);
            editPanel = EditPanel.Create(this, bottomDock);
            deletePanel = DeletePanel.Create(this, bottomDock);
            viewPanel = ViewPanel.Create(this, leftDock);
            propertiesPanel = PropertiesPanel.Create(this, rightDock);
            if (phone) leftDock.gameObject.SetActive(false);   // drawers start closed on a phone: the viewport comes first

            modalLayer = UIFactory.Rect(transform, "Modals");
            UIFactory.Stretch(modalLayer);
            popupLayer = UIFactory.Rect(transform, "Popups");
            UIFactory.Stretch(popupLayer);
            toastLayer = UIFactory.Rect(transform, "Toasts");
            UIFactory.Stretch(toastLayer);
            if (phone)
            {
                ApplySafeArea(modalLayer);
                ApplySafeArea(popupLayer);
                ApplySafeArea(toastLayer);
            }
            // notifications stack up from just above the dock; each row can carry an icon and an Undo button
            float tw = phone ? 230 : 280;
            noteStack = UIFactory.Rect(toastLayer, "Notes");
            UIFactory.Anchor(noteStack, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-tw, dockHeight + stripHeight + 10), new Vector2(tw, dockHeight + stripHeight + 10 + 4 * 44));
            var vl = UIFactory.VLayout(noteStack, 4, 0, true, true, TextAnchor.LowerCenter);
            vl.childForceExpandHeight = false;
            // legacy single-line toast kept for callers that update the same line rapidly
            var toastBg = UIFactory.Rect(toastLayer, "Toast");
            UIFactory.Anchor(toastBg, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-tw, dockHeight + stripHeight + 10), new Vector2(tw, dockHeight + stripHeight + 50));
            toastText = UIFactory.Label(toastBg, "", 16, TextAnchor.MiddleCenter, UIFactory.Accent);
            UIFactory.Stretch(toastText.rectTransform, 8, 4, 8, 4);
            toastBg.gameObject.SetActive(false);
            // tooltip bubble
            tooltipRt = UIFactory.Panel(toastLayer, "Tooltip", new Color(0.05f, 0.04f, 0.07f, 0.96f));
            UIFactory.Skin(tooltipRt.GetComponent<Image>(), UIFactory.SkinKind.Parchment);
            tooltipRt.GetComponent<Image>().raycastTarget = false;
            tooltipText = UIFactory.Label(tooltipRt, "", 13, TextAnchor.MiddleLeft, UIFactory.Themed ? UIFactory.Ink : UIFactory.TextColor);
            UIFactory.Stretch(tooltipText.rectTransform, 8, 4, 8, 4);
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipRt.gameObject.SetActive(false);

            editor.ModeChanged += OnModeChanged;
            editor.SelectionChanged += OnSelectionChanged;
            editor.ViewOptionsChanged += RefreshHint;
            editor.SelectionChanged += RefreshHint;
            editor.ModeChanged += RefreshHint;
            OnModeChanged();
            OnSelectionChanged();
            RefreshHint();
            EditorTour.StartIfFirstRun(this);
            Geodashy.Core.Achievements.Announce = msg =>
            {
                if (this != null) Toast(msg, 4f);
                Geodashy.Core.Sfx.Play("horn", 0.6f, 1.3f);
            };
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
            bool hasSelection = editor.selection.Count > 0;
            if (phone && !hasSelection) propsDrawerOpen = false;
            rightDock.gameObject.SetActive(hasSelection && !playMode && (!phone || propsDrawerOpen));
            topBar.RefreshDrawerButtons();
        }

        // ---- phone drawers ---------------------------------------------------

        public void ToggleViewDrawer()
        {
            viewDrawerOpen = !viewDrawerOpen;
            leftDock.gameObject.SetActive(viewDrawerOpen);
            if (viewDrawerOpen && phone) propsDrawerOpen = false;   // one drawer at a time keeps the viewport usable
            OnSelectionChanged();
        }

        public void TogglePropsDrawer()
        {
            if (editor.selection.Count == 0)
            {
                Toast("Select something first: tap an object in Edit mode");
                return;
            }
            propsDrawerOpen = !propsDrawerOpen;
            if (propsDrawerOpen && phone)
            {
                viewDrawerOpen = false;
                leftDock.gameObject.SetActive(false);
            }
            OnSelectionChanged();
        }

        /// <summary>Hides or shows the bottom dock; the side docks grow to fill the gap.</summary>
        public void ToggleDock()
        {
            dockCollapsed = !dockCollapsed;
            bottomDock.gameObject.SetActive(!dockCollapsed);
            float bottom = dockCollapsed ? 0f : dockHeight;
            leftDock.offsetMin = new Vector2(0, bottom + stripHeight);
            rightDock.offsetMin = new Vector2(-rightWidth, bottom + stripHeight);
            if (statusBar != null)
            {
                statusBar.offsetMin = new Vector2(0, bottom);
                statusBar.offsetMax = new Vector2(0, bottom + stripHeight);
            }
            var toast = toastText != null ? toastText.transform.parent as RectTransform : null;
            if (toast != null)
            {
                toast.offsetMin = new Vector2(toast.offsetMin.x, bottom + stripHeight + 10);
                toast.offsetMax = new Vector2(toast.offsetMax.x, bottom + stripHeight + 50);
            }
            if (noteStack != null)
            {
                noteStack.offsetMin = new Vector2(noteStack.offsetMin.x, bottom + stripHeight + 10);
                noteStack.offsetMax = new Vector2(noteStack.offsetMax.x, bottom + stripHeight + 10 + 4 * 44);
            }
            topBar.RefreshDrawerButtons();
        }

        void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastText.transform.parent.gameObject.SetActive(false);
            }
            UpdateNotes(Time.unscaledDeltaTime);
            statusTimer -= Time.unscaledDeltaTime;
            if (statusTimer <= 0f)
            {
                statusTimer = 0.1f;
                RefreshStatus();
            }
            // Enter confirms the top modal's primary action (Esc closes it from the editor's key handling)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && modals.Count > 0 && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                var top = modals[modals.Count - 1];
                if (modalPrimary.TryGetValue(top, out var act) && act != null) act();
            }
            topBar?.Tick();
            viewPanel?.Tick();
            timeline?.Tick();
        }

        // ---- public helpers ---------------------------------------------------

        public void Toast(string message, float seconds = 2.2f) => Notify(message, seconds, null, null, null);

        /// <summary>Stacked notification with an optional glyph and an action button ("Undo" after a delete).</summary>
        public void Notify(string message, float seconds = 2.2f, string icon = null, string actionLabel = null, Action action = null)
        {
            if (noteStack == null) return;
            // an identical message already showing just gets its timer refreshed
            foreach (var n in notes)
            {
                var t = n.rt.GetComponentInChildren<Text>();
                if (t != null && t.text == message && n.life > 0.3f)
                {
                    n.life = seconds;
                    return;
                }
            }
            while (notes.Count >= 4)
            {
                Destroy(notes[0].rt.gameObject);
                notes.RemoveAt(0);
            }
            var row = UIFactory.Panel(noteStack, "Note", new Color(0.05f, 0.04f, 0.07f, 0.9f));
            UIFactory.Skin(row.GetComponent<Image>(), UIFactory.SkinKind.Iron);
            row.GetComponent<Image>().raycastTarget = action != null;
            UIFactory.HLayout(row, 8, 8, false, TextAnchor.MiddleLeft);
            UIFactory.Layout(row.gameObject, -1, 40, 1);
            var g = row.gameObject.AddComponent<CanvasGroup>();
            if (!string.IsNullOrEmpty(icon))
            {
                var ic = UIFactory.Icon(row, Geodashy.Rendering.EditorIcons.Get(icon), 18, UIFactory.Accent);
                UIFactory.Layout(ic.gameObject, 18, 18);
            }
            var label = UIFactory.Label(row, message, 14, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 40);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (action != null)
            {
                var note = new Note { rt = row, life = seconds, group = g };
                UIFactory.Button(row, actionLabel ?? "Undo", () =>
                {
                    action();
                    note.life = 0f;
                }, 70, 28, UIFactory.ButtonActive, 13);
                notes.Add(note);
            }
            else notes.Add(new Note { rt = row, life = seconds, group = g });
        }

        void UpdateNotes(float dt)
        {
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                var n = notes[i];
                n.life -= dt;
                if (n.group != null) n.group.alpha = Mathf.Clamp01(n.life / 0.35f);
                if (n.life <= 0f)
                {
                    if (n.rt != null) Destroy(n.rt.gameObject);
                    notes.RemoveAt(i);
                }
            }
        }

        // ---- status strip and hint bar ------------------------------------------------

        void BuildStatusBar()
        {
            statusBar = UIFactory.Panel(root, "StatusBar", new Color(0.07f, 0.06f, 0.09f, 0.96f));
            UIFactory.Anchor(statusBar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, dockHeight), new Vector2(0, dockHeight + stripHeight));
            statusBar.GetComponent<Image>().raycastTarget = false;
            statusText = UIFactory.Label(statusBar, "", 12, TextAnchor.MiddleLeft, UIFactory.TextDim);
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Anchor(statusText.rectTransform, new Vector2(0, 0), new Vector2(phone ? 0f : 0.5f, 1), new Vector2(10, 0), new Vector2(phone ? 0f : -4, 0));
            hintText = UIFactory.Label(statusBar, "", 12, TextAnchor.MiddleRight, UIFactory.Accent);
            hintText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Anchor(hintText.rectTransform, new Vector2(phone ? 0f : 0.5f, 0), new Vector2(1, 1), new Vector2(phone ? 10f : 4, 0), new Vector2(-10, 0));
            if (phone)
            {
                statusText.gameObject.SetActive(false);
                hintText.alignment = TextAnchor.MiddleLeft;
            }
        }

        void RefreshStatus()
        {
            if (statusText == null || !statusText.gameObject.activeSelf || editor == null || editor.level == null) return;
            var sb = new System.Text.StringBuilder();
            var c = editor.CursorSnapped;
            sb.Append("x ").Append(c.x.ToString("0.##")).Append("  y ").Append(c.y.ToString("0.##"));
            int sel = editor.selection.Count;
            if (sel > 0)
            {
                var b = editor.SelectionBounds();
                sb.Append("   ·   ").Append(sel).Append(sel == 1 ? " selected" : " selected  ").Append(sel == 1 ? "" : b.width.ToString("0.#") + "×" + b.height.ToString("0.#"));
            }
            sb.Append("   ·   zoom ").Append(Mathf.RoundToInt(editor.editorCamera.zoom * 100f)).Append('%');
            sb.Append("   ·   layer ").Append(editor.currentEditorLayer).Append(editor.showAllLayers ? " (all)" : "");
            sb.Append("   ·   grid ").Append(editor.gridSize.ToString("0.##")).Append(editor.snapToGrid ? "" : " (no snap)");
            int n = editor.level.objects.Count;
            sb.Append("   ·   ").Append(n).Append(" objects").Append(n >= LevelEditor.BudgetHigh ? " (heavy)" : "");
            sb.Append("   ·   ").Append(string.IsNullOrEmpty(editor.currentFilePath) ? "unsaved" : (editor.Dirty ? "unsaved changes" : "saved"));
            statusText.text = sb.ToString();
        }

        /// <summary>One line that explains the current tool state; changes with mode, brush and selection.</summary>
        public void RefreshHint()
        {
            if (hintText == null) return;
            if (!HintsEnabled)
            {
                hintText.text = "";
                return;
            }
            string h;
            bool touch = phone;
            switch (editor.Mode)
            {
                case EditorMode.Build:
                    if (editor.StampBrush != null) h = touch ? "Stamp: tap to place the group" : "Stamp: click to place the group · Esc clears the brush";
                    else if (editor.pathTool) h = touch ? "Path: tap points, then Lay" : "Path: click points, then Lay (grid) or Lay (beat) · Curve bends it";
                    else if (editor.BuildDef != null) h = touch ? "Build: tap to place " + editor.BuildDef.name + " · two fingers pan and zoom" : "Build: click to place " + editor.BuildDef.name + " · drag paints · Ctrl+click selects · Q/E rotate · F/V flip · Esc clears";
                    else h = touch ? "Build: pick an object from the shelf below" : "Build: pick an object from the shelf · Ctrl+K searches everything";
                    break;
                case EditorMode.Edit:
                    if (editor.selection.Count > 0) h = touch ? editor.selection.Count + " selected: drag to move · long-press for the menu · Props edits" : editor.selection.Count + " selected: drag to move · arrows nudge · X gizmo · Ctrl+D duplicate · Delete removes · right-click for the menu";
                    else h = touch ? "Edit: tap an object to select · drag empty space to box-select" : "Edit: click to select · Shift+click adds · drag empty space to box-select · T selects same type";
                    break;
                default:
                    h = touch ? "Delete: tap objects to remove them · drag to sweep" : "Delete: click objects to remove them · drag to sweep · Ctrl+Z brings them back";
                    break;
            }
            hintText.text = h;
        }

        // ---- tooltips ---------------------------------------------------------------------

        public void ShowTooltip(Tooltip owner, string text, RectTransform anchor)
        {
            if (tooltipRt == null || anchor == null) return;
            tooltipOwner = owner;
            tooltipText.text = text;
            float w = Mathf.Clamp(text.Length * 7.2f + 20f, 60f, 320f);
            float lines = Mathf.Ceil(text.Length * 7.2f / (w - 16f));
            float h = 22f + (lines - 1) * 16f;
            tooltipRt.gameObject.SetActive(true);
            var canvasRt = canvas.GetComponent<RectTransform>();
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, RectTransformUtility.WorldToScreenPoint(null, corners[1]), null, out var tl);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out var bl);
            float canvasH = canvasRt.rect.height, canvasW = canvasRt.rect.width;
            float x = Mathf.Clamp(tl.x, -canvasW / 2f + 4f, canvasW / 2f - w - 4f);
            // above the widget when there is room, below it otherwise
            float y = tl.y + 6f + h;
            if (y > canvasH / 2f) y = bl.y - 6f;
            tooltipRt.anchorMin = tooltipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRt.pivot = new Vector2(0, 1);
            tooltipRt.anchoredPosition = new Vector2(x, y);
            tooltipRt.sizeDelta = new Vector2(w, h);
            tooltipRt.SetAsLastSibling();
        }

        public void HideTooltip(Tooltip owner)
        {
            if (tooltipRt == null) return;
            if (owner != null && tooltipOwner != null && owner != tooltipOwner) return;
            tooltipOwner = null;
            tooltipRt.gameObject.SetActive(false);
        }

        /// <summary>Registers the action Enter triggers while this modal is on top.</summary>
        public void SetModalPrimary(GameObject modal, Action action)
        {
            if (modal == null) return;
            modalPrimary[modal] = action;
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
        public void OpenHistory() => UndoHistoryDialog.Open(this, editor);

        /// <summary>Shows the properties of the selection: opens the Props drawer on phones (always visible on desktop).</summary>
        public void OpenPropertiesForSelection()
        {
            if (editor.selection.Count == 0)
            {
                Toast("Nothing selected");
                return;
            }
            if (phone && !propsDrawerOpen) TogglePropsDrawer();
        }

        /// <summary>Asks for a name and stores the selection as a stamp for the Stamps shelf.</summary>
        public void PromptSaveStamp()
        {
            if (editor.selection.Count == 0)
            {
                Toast("Select the objects to stamp first");
                return;
            }
            Prompt("Save as stamp", "Name for this stamp (" + editor.selection.Count + " objects):", "My stamp", name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                if (editor.SaveSelectionAsStamp(name.Trim())) Toast("Stamp saved: " + name.Trim() + " — find it on the Stamps shelf");
            });
        }

        /// <summary>A popup list at a screen position (context menus).</summary>
        public void ShowMenuAt(Vector2 screenPos, string[] options, Action<int> onPick)
        {
            float rowH = phone ? 40f : 28f;
            float width = phone ? 220f : 180f;
            float height = options.Length * (rowH + 2f) + 8f;
            ClosePopup();
            var backdrop = UIFactory.Panel(popupLayer, "PopupBackdrop", Color.clear);
            var bb = backdrop.gameObject.AddComponent<Button>();
            bb.transition = Selectable.Transition.None;
            bb.onClick.AddListener(ClosePopup);
            popup = backdrop.gameObject;
            var frame = UIFactory.Panel(backdrop, "Popup", UIFactory.PanelBg3);
            var canvasRt = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, null, out var local);
            float canvasH = canvasRt.rect.height, canvasW = canvasRt.rect.width;
            float x = Mathf.Clamp(local.x + 6f, -canvasW / 2f, canvasW / 2f - width);
            float top = local.y - 6f;
            if (top - height < -canvasH / 2f) top = Mathf.Min(canvasH / 2f, local.y + height + 6f);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0, 1);
            frame.anchoredPosition = new Vector2(x, top);
            frame.sizeDelta = new Vector2(width, height);
            var col = UIFactory.Rect(frame, "Col");
            UIFactory.Stretch(col, 2, 2, 2, 2);
            UIFactory.VLayout(col, 2, 2);
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var b = UIFactory.Button(col, options[i], () =>
                {
                    ClosePopup();
                    onPick?.Invoke(idx);
                }, -1, rowH, UIFactory.ButtonBg, 13);
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            }
        }

        /// <summary>Leaves the editor for the main menu, asking to save first when there are changes.</summary>
        public void ReturnToMenu()
        {
            if (AppController.Instance == null)
            {
                Toast("No main menu in this scene");
                return;
            }
            if (editor.Dirty)
            {
                Confirm("Unsaved changes", "Save the quest before leaving? Unsaved work is kept in the autosave either way.", () =>
                {
                    if (editor.Save()) AppController.Instance.ShowMenu();
                }, "Save & leave");
                return;
            }
            AppController.Instance.ShowMenu();
        }
        public void OpenFileDialog() => FileDialog.Open(this, editor);
        public void OpenSettings() => LevelSettingsDialog.Open(this, editor);

        /// <summary>Saves; asks for a name first if the level has never been saved.</summary>
        public void SaveWithPrompt()
        {
            if (!string.IsNullOrEmpty(editor.currentFilePath))
            {
                editor.Save();
                return;
            }
            Prompt("Save quest", "Give your quest a name:", editor.level.name, name =>
            {
                if (!string.IsNullOrWhiteSpace(name)) editor.level.name = name.Trim();
                editor.Save();
            });
        }

        public void PromptSaveAs()
        {
            Prompt("Save As", "Name for the copy:", editor.level.name, name => editor.SaveAs(name));
        }

        // ---- modals --------------------------------------------------------------

        /// <summary>Creates a modal window and returns its content column.</summary>
        public RectTransform OpenModal(string title, float width, float height, bool scroll = false)
        {
            // never larger than the screen (phones have far fewer canvas units than a desktop window)
            var avail = modalLayer.rect;
            if (avail.width > 100f && avail.height > 100f)
            {
                width = Mathf.Min(width, avail.width - 16f);
                height = Mathf.Min(height, avail.height - 16f);
            }
            var backdrop = UIFactory.Panel(modalLayer, "Modal " + title, new Color(0, 0, 0, 0.55f));
            var window = UIFactory.Frame(backdrop, "Window", UIFactory.PanelBg2);
            UIFactory.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-width / 2f, -height / 2f), new Vector2(width / 2f, height / 2f));
            // parchment title strip pinned across the iron frame
            var header = UIFactory.Themed ? UIFactory.Card(window, "Header") : UIFactory.Panel(window, "Header", UIFactory.PanelBg3);
            UIFactory.Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(6, -40), new Vector2(-6, -4));
            var t = UIFactory.Label(header, title, 18, TextAnchor.MiddleLeft, UIFactory.Themed ? UIFactory.Ink : UIFactory.Accent, -1, -1, true);
            UIFactory.Stretch(t.rectTransform, 14, 0, 50, 0);
            var close = UIFactory.IconButton(header, "close", null, () => CloseModal(backdrop.gameObject), 36, 30, UIFactory.Danger, 16, "Close (Esc)");
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
            modalPrimary.Remove(modal);
            Destroy(modal);
        }

        public void CloseTopModal()
        {
            if (modals.Count == 0) return;
            var m = modals[modals.Count - 1];
            modals.RemoveAt(modals.Count - 1);
            modalPrimary.Remove(m);
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
            Action yes = () =>
            {
                CloseModal(modal);
                onYes?.Invoke();
            };
            UIFactory.Button(row, yesLabel, () => yes(), 110, 34, UIFactory.ButtonActive);
            SetModalPrimary(modal, yes);
        }

        public void Prompt(string title, string message, string initial, Action<string> onOk)
        {
            var content = OpenModal(title, 460, 200);
            var modal = TopModal;
            UIFactory.Label(content, message, 15, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 30);
            var input = UIFactory.Input(content, "", initial, null, -1, 32);
            var row = UIFactory.Row(content, 36, 8, TextAnchor.MiddleRight);
            UIFactory.Button(row, "Cancel", () => CloseModal(modal), 110, 34);
            Action ok = () =>
            {
                var v = input.text;
                CloseModal(modal);
                onOk?.Invoke(v);
            };
            UIFactory.Button(row, "OK", () => ok(), 110, 34, UIFactory.ButtonActive);
            SetModalPrimary(modal, ok);
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
            float rowH = phone ? 40f : 28f;
            float maxH = phone ? Mathf.Max(200f, popupLayer.rect.height - 40f) : 340f;
            float height = Mathf.Min(maxH, options.Length * (rowH + 2f) + 8f);
            float width = Mathf.Max(anchor.rect.width, phone ? 220f : 160f);
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
