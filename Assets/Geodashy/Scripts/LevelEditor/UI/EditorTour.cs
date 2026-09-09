using System;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>
    /// First-run walkthrough of the editor: dims everything except one part of the interface at a time and explains it
    /// in a card with Back / Next / Skip. Replayable from the View dock and the Help dialog.
    /// </summary>
    public class EditorTour : MonoBehaviour
    {
        public const string DonePref = "geodashy.tourDone";

        struct Step
        {
            public string title, body;
            public Func<RectTransform> target;
        }

        EditorUI ui;
        RectTransform layer, dimTop, dimBottom, dimLeft, dimRight, ring, card;
        Text titleText, bodyText, stepText;
        Button nextButton;
        Step[] steps;
        int step;
        static EditorTour current;

        public static void StartIfFirstRun(EditorUI ui)
        {
            if (PlayerPrefs.GetInt(DonePref, 0) == 1) return;
            Begin(ui);
        }

        public static void Begin(EditorUI ui)
        {
            if (ui == null || ui.root == null) return;
            Close();
            var go = new GameObject("EditorTour", typeof(RectTransform));
            go.transform.SetParent(ui.root, false);
            go.transform.SetAsLastSibling();
            var t = go.AddComponent<EditorTour>();
            t.ui = ui;
            t.Build();
            current = t;
        }

        public static void Close()
        {
            if (current != null) Destroy(current.gameObject);
            current = null;
        }

        void Build()
        {
            layer = GetComponent<RectTransform>();
            UIFactory.Stretch(layer);
            var dim = new Color(0f, 0f, 0f, 0.62f);
            dimTop = UIFactory.Panel(layer, "DimTop", dim);
            dimBottom = UIFactory.Panel(layer, "DimBottom", dim);
            dimLeft = UIFactory.Panel(layer, "DimLeft", dim);
            dimRight = UIFactory.Panel(layer, "DimRight", dim);
            ring = UIFactory.Rect(layer, "Ring");
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = Geodashy.Rendering.PlaceholderSpriteFactory.Outline();
            ringImg.type = Image.Type.Sliced;
            ringImg.color = UIFactory.Accent;
            ringImg.raycastTarget = false;

            bool phone = ui.IsPhone;
            float w = phone ? 360f : 460f, h = phone ? 190f : 230f;
            card = UIFactory.Panel(layer, "Card", UIFactory.PanelBg2);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(w, h);
            UIFactory.VLayout(card, 6, 14, true, true, TextAnchor.UpperLeft);
            var head = UIFactory.Row(card, 28, 8);
            titleText = UIFactory.Label(head, "", phone ? 16 : 18, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 28, true);
            stepText = UIFactory.Label(head, "", 12, TextAnchor.MiddleRight, UIFactory.TextDim, 60, 28);
            bodyText = UIFactory.Label(card, "", phone ? 12 : 14, TextAnchor.UpperLeft, UIFactory.TextColor, -1, -1);
            UIFactory.Layout(bodyText.gameObject, -1, -1, 1, 1);
            var row = UIFactory.Row(card, 34, 8, TextAnchor.MiddleRight);
            UIFactory.Button(row, "Skip tour", Finish, 100, 32, null, 13);
            UIFactory.Layout(UIFactory.Spacer(row, 32).gameObject, -1, 32, 1);
            UIFactory.Button(row, "◀ Back", () => Show(step - 1), 90, 32, null, 13);
            nextButton = UIFactory.Button(row, "Next ▶", () => Show(step + 1), 110, 32, UIFactory.ButtonActive, 13);

            steps = new[]
            {
                new Step { title = "Welcome to the forge", body = "This is where quests are built. The tour points at each part of the workshop in turn; nothing here is destructive, and you can replay it any time from the View dock or Help.", target = () => null },
                new Step { title = "The top bar", body = phone
                    ? "Build, Edit and Del are the three modes. ↶ ↷ undo and redo, ▶ Play tests the quest, View and Props open the side drawers, ▼ folds the dock away and ⋯ holds saving, files, settings and more."
                    : "Build, Edit and Delete are the three modes (keys 1, 2, 3). Undo and redo sit beside them, then ▶ Play, ▶ Marker and ▶ Train to test the quest. Save, Files, Settings, Help and History live on the right.", target = () => ui.topBar != null ? ui.topBar.transform as RectTransform : null },
                new Step { title = "The timeline", body = "The whole quest in one strip: bar lines from the BPM, the song's waveform behind them, triggers and portals as dots and your bookmarks as diamonds. Click anywhere to jump the camera there; drag a trigger to retime it.", target = () => ui.timeline != null ? ui.timeline.transform as RectTransform : null },
                new Step { title = "The palette", body = phone
                    ? "In Build mode the dock shows the object shelves: the ▾ button picks a shelf (★ Favourites keeps your starred objects), Search filters, the strip scrolls sideways. One finger places; the buttons on the right rotate, flip and scale."
                    : "In Build mode the dock lists the shelves on the left, the objects in the middle and the placement transform on the right. Click the level to place the brush; Ctrl+click selects instead. Star anything you use often for the ★ Favourites shelf.", target = () => ui.bottomDock },
                new Step { title = "The View dock", body = phone
                    ? "Open it with View in the top bar: quest name, testing, grid and beat snapping, guides, editor layers, colour channels, groups, bookmarks and zoom all live there."
                    : "Everything about how you look at the quest: testing buttons, grid and beat snapping, guides, editor layers, colour channels, groups with names and tints, bookmarks, zoom and navigation.", target = () => ui.leftDock != null && ui.leftDock.gameObject.activeInHierarchy ? ui.leftDock : null },
                new Step { title = "Edit and Properties", body = phone
                    ? "Switch to Edit, tap an object and the Props drawer shows its settings; drag to move it, long-press for the context menu. Select several to edit them together."
                    : "Switch to Edit, click an object and the Properties dock on the right shows its settings; drag to move, right-click for the context menu, X for the transform gizmo. Select several and blank fields mean they differ.", target = () => ui.rightDock != null && ui.rightDock.gameObject.activeInHierarchy ? ui.rightDock : null },
                new Step { title = "Ride it", body = "Press ▶ Play (P) whenever you like; Esc brings you back with the run drawn over the level. Level settings hold the song, tempo, backgrounds and difficulty rating. Files saves, shares and packs quests. Have fun building!", target = () => null },
            };
            Show(0);
        }

        void Show(int i)
        {
            if (steps == null) return;
            step = Mathf.Clamp(i, 0, steps.Length - 1);
            titleText.text = steps[step].title;
            bodyText.text = steps[step].body;
            stepText.text = (step + 1) + " / " + steps.Length;
            UIFactory.SetButtonLabel(nextButton, step == steps.Length - 1 ? "Finish" : "Next ▶");
            if (step == steps.Length - 1)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(Finish);
            }
            else
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => Show(step + 1));
            }
            Layout();
        }

        void Finish()
        {
            PlayerPrefs.SetInt(DonePref, 1);
            Close();
        }

        void LateUpdate()
        {
            if (ui == null || ui.editor == null)
            {
                Close();
                return;
            }
            Layout();
        }

        /// <summary>Places the four dim panels around the step's target and keeps the card clear of it.</summary>
        void Layout()
        {
            if (layer == null || steps == null) return;
            var L = layer.rect;
            if (L.width < 10f || L.height < 10f) return;
            RectTransform target = steps[step].target != null ? steps[step].target() : null;
            Rect r = new Rect(0, 0, 0, 0);
            bool has = false;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                var corners = new Vector3[4];
                target.GetWorldCorners(corners);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out var b);
                r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                has = r.width > 4f && r.height > 4f;
            }
            if (!has) r = new Rect(L.center.x, L.center.y, 0f, 0f);
            void Set(RectTransform rt, float left, float bottom, float right, float top)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(left, bottom);
                rt.offsetMax = new Vector2(-right, -top);
            }
            // distances from the layer edges to the highlight rect
            float dl = r.xMin - L.xMin, db = r.yMin - L.yMin, dr = L.xMax - r.xMax, dt = L.yMax - r.yMax;
            Set(dimTop, 0, L.height - dt, 0, 0);
            Set(dimBottom, 0, 0, 0, L.height - db);
            Set(dimLeft, 0, db, L.width - dl, dt);
            Set(dimRight, L.width - dr, db, 0, dt);
            ring.gameObject.SetActive(has);
            if (has) Set(ring, dl - 3f, db - 3f, dr - 3f, dt - 3f);

            // card: centred, but pushed to the side of the screen the highlight does not occupy
            var size = card.sizeDelta;
            float cx = 0f, cy = 0f;
            if (has)
            {
                bool wide = r.width > L.width * 0.6f;
                bool tall = r.height > L.height * 0.6f;
                if (wide && !tall) cy = r.center.y > L.center.y ? r.yMin - L.center.y - size.y / 2f - 16f : r.yMax - L.center.y + size.y / 2f + 16f;
                else if (!wide) cx = r.center.x > L.center.x ? r.xMin - L.center.x - size.x / 2f - 16f : r.xMax - L.center.x + size.x / 2f + 16f;
            }
            cx = Mathf.Clamp(cx, -L.width / 2f + size.x / 2f + 8f, L.width / 2f - size.x / 2f - 8f);
            cy = Mathf.Clamp(cy, -L.height / 2f + size.y / 2f + 8f, L.height / 2f - size.y / 2f - 8f);
            card.anchoredPosition = new Vector2(cx, cy);
        }
    }
}
