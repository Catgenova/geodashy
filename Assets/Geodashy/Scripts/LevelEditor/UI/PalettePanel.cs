using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Build-mode dock: object categories, searchable object grid and placement transform controls.</summary>
    public class PalettePanel : MonoBehaviour
    {
        EditorUI ui;
        LevelEditor editor;
        RectTransform gridContent;
        ScrollRect gridScroll;
        InputField search;
        string currentCategory = ObjectCatalog.CatBlocks;
        readonly Dictionary<string, Button> categoryButtons = new Dictionary<string, Button>();
        readonly Dictionary<string, Button> tiles = new Dictionary<string, Button>();
        Text selectedName, selectedDesc, transformLabel;
        Toggle swipeToggle;
        Button swipeButton, categoryButton;   // phone layout
        string lastSearch = "";
        public const string StampsCategory = "Stamps";
        Button deleteStampButton;
        readonly Dictionary<string, Stamp> stampTiles = new Dictionary<string, Stamp>();

        static string[] CategoriesWithStamps()
        {
            var list = new List<string>(ObjectCatalog.Categories);
            list.Add(StampsCategory);
            return list.ToArray();
        }

        public static PalettePanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "PalettePanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<PalettePanel>();
            p.ui = ui;
            p.editor = ui.editor;
            if (ui.IsPhone) p.BuildPhone(rt);
            else p.Build(rt);
            return p;
        }

        /// <summary>
        /// Phone dock (150 units tall): category dropdown + search on the left, a horizontally scrolling strip of
        /// object tiles in the middle and a 3×3 grid of finger-sized transform buttons on the right.
        /// </summary>
        void BuildPhone(RectTransform rt)
        {
            UIFactory.HLayout(rt, 6, 6, false, TextAnchor.UpperLeft);
            rt.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            var left = UIFactory.Column(rt, 150, 4);
            categoryButton = UIFactory.Button(left, currentCategory + "  ▾", () =>
            {
                var cats = CategoriesWithStamps();
                int cur = System.Array.IndexOf(cats, currentCategory);
                ui.ShowDropdown(categoryButton.GetComponent<RectTransform>(), cats, cur, i => ShowCategory(cats[i]));
            }, -1, 40, UIFactory.InputBg, 13);
            UIFactory.Button(left, "Search…", () =>
            {
                ui.Prompt("Search objects", "Name, tag or category:", lastSearch, q =>
                {
                    lastSearch = q ?? "";
                    if (string.IsNullOrWhiteSpace(lastSearch)) ShowCategory(currentCategory);
                    else
                    {
                        Populate(ObjectCatalog.Search(lastSearch));
                        UIFactory.SetButtonLabel(categoryButton, "\"" + UIFactory.Trunc(lastSearch, 10) + "\"  ▾");
                    }
                });
            }, -1, 40, null, 13);
            selectedName = UIFactory.Label(left, "", 13, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 20, true);
            transformLabel = UIFactory.Label(left, "", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);
            selectedDesc = UIFactory.Label(left, "", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 0);
            selectedDesc.gameObject.SetActive(false);   // no room for the description on a phone; the name is enough

            gridScroll = UIFactory.ScrollView(rt, "Objects", out gridContent, false, true);
            UIFactory.Layout(gridScroll.gameObject, -1, -1, 1, 1);
            var grid = UIFactory.Grid(gridContent, new Vector2(84, 84), new Vector2(5, 5), 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            UIFactory.Fitter(gridContent, false, true);

            var right = UIFactory.Column(rt, 204, 4);
            var r1 = UIFactory.Row(right, 40, 4);
            UIFactory.Button(r1, "↺ 90", () => editor.RotateSelection(90), -1, 40, null, 12);
            UIFactory.Button(r1, "↻ 90", () => editor.RotateSelection(-90), -1, 40, null, 12);
            UIFactory.Button(r1, "Flip H", () => editor.FlipSelection(true), -1, 40, null, 12);
            var r2 = UIFactory.Row(right, 40, 4);
            UIFactory.Button(r2, "↺ 45", () => editor.RotateSelection(45), -1, 40, null, 12);
            UIFactory.Button(r2, "Reset", () =>
            {
                editor.placeRotation = 0;
                editor.placeFlipX = editor.placeFlipY = false;
                editor.placeScale = 1;
                editor.NotifyViewOptionsChanged();
            }, -1, 40, null, 12);
            UIFactory.Button(r2, "Flip V", () => editor.FlipSelection(false), -1, 40, null, 12);
            var r3 = UIFactory.Row(right, 40, 4);
            UIFactory.Button(r3, "Scale −", () => editor.ScaleSelection(0.5f), -1, 40, null, 12);
            UIFactory.Button(r3, "Scale +", () => editor.ScaleSelection(2f), -1, 40, null, 12);
            swipeButton = UIFactory.Button(r3, "Swipe", () =>
            {
                editor.swipeBuild = !editor.swipeBuild;
                RefreshPlacement();
                ui.Toast(editor.swipeBuild ? "Swipe to paint: drag places a row of objects" : "Swipe off: one object per tap");
            }, -1, 40, null, 12);
            deleteStampButton = UIFactory.Button(left, "Delete stamp", DeleteCurrentStamp, -1, 0, UIFactory.Danger, 12);
            deleteStampButton.gameObject.SetActive(false);

            ShowCategory(currentCategory);
            editor.ViewOptionsChanged += RefreshPlacement;
            editor.StampsChanged += () => { if (currentCategory == StampsCategory) ShowCategory(StampsCategory); };
            RefreshPlacement();
        }

        void DeleteCurrentStamp()
        {
            var stamp = editor.StampBrush;
            if (stamp == null)
            {
                ui.Toast("Pick a stamp on the Stamps shelf first");
                return;
            }
            ui.Confirm("Delete stamp " + stamp.name + "?", "The stamp file is removed. Objects already placed with it stay.", () =>
            {
                StampStorage.Delete(stamp);
                editor.SetStampBrush(null);
                editor.NotifyStampsChanged();
                ui.Toast("Stamp deleted");
            }, "Delete");
        }

        void PopulateStamps()
        {
            foreach (Transform child in gridContent) Destroy(child.gameObject);
            tiles.Clear();
            stampTiles.Clear();
            var stamps = StampStorage.List();
            if (stamps.Count == 0)
            {
                var hint = UIFactory.Label(gridContent, "No stamps yet. Select some objects in Edit mode and choose Save as stamp (Edit dock, context menu or ⋯ menu).", 12, TextAnchor.UpperLeft, UIFactory.TextDim, 240, 76);
                hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            foreach (var st in stamps)
            {
                var stamp = st;
                var def = ObjectCatalog.Get(stamp.previewType);
                var sprite = def != null ? SpriteLibrary.ForObject(def) : null;
                var b = UIFactory.TileButton(gridContent, sprite, stamp.name + " (" + stamp.objects.Count + ")", () =>
                {
                    editor.SetStampBrush(stamp);
                    RefreshPlacement();
                });
                stampTiles[stamp.path] = b;
            }
            gridScroll.verticalNormalizedPosition = 1f;
            gridScroll.horizontalNormalizedPosition = 0f;
            RefreshPlacement();
        }

        void Build(RectTransform rt)
        {
            UIFactory.HLayout(rt, 8, 8, false, TextAnchor.UpperLeft);
            rt.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            // categories --------------------------------------------------------
            var catScroll = UIFactory.ScrollView(rt, "Categories", out var catContent, true, false);
            UIFactory.Layout(catScroll.gameObject, 150, -1, -1, 1);
            UIFactory.VLayout(catContent, 3, 4);
            UIFactory.Fitter(catContent, true, false);
            foreach (var cat in CategoriesWithStamps())
            {
                var c = cat;
                var b = UIFactory.Button(catContent, cat, () => ShowCategory(c), -1, 28, null, 13);
                categoryButtons[cat] = b;
            }

            // object grid ------------------------------------------------------------
            var center = UIFactory.Column(rt, -1, 6);
            var searchRow = UIFactory.Row(center, 30, 6);
            UIFactory.Label(searchRow, "Search", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, 52, 30);
            search = UIFactory.Input(searchRow, "name, tag or category…", "", _ => { }, -1, 28);
            search.onValueChanged.AddListener(OnSearchChanged);
            UIFactory.Button(searchRow, "✕", () =>
            {
                search.text = "";
                ShowCategory(currentCategory);
            }, 30, 28);
            gridScroll = UIFactory.ScrollView(center, "Objects", out gridContent, true, false);
            UIFactory.Layout(gridScroll.gameObject, -1, -1, 1, 1);
            UIFactory.Grid(gridContent, new Vector2(76, 76), new Vector2(5, 5), 6);
            UIFactory.Fitter(gridContent, true, false);

            // placement controls --------------------------------------------------------
            var right = UIFactory.Column(rt, 250, 4);
            UIFactory.SectionHeader(right, "Placement");
            selectedName = UIFactory.Label(right, "", 15, TextAnchor.MiddleLeft, UIFactory.Accent, -1, 22, true);
            selectedDesc = UIFactory.Label(right, "", 11, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 44);
            var r1 = UIFactory.Row(right, 30, 4);
            UIFactory.Button(r1, "↺ 90 (Q)", () => editor.RotateSelection(90), -1, 28, null, 12);
            UIFactory.Button(r1, "↻ 90 (E)", () => editor.RotateSelection(-90), -1, 28, null, 12);
            var r2 = UIFactory.Row(right, 30, 4);
            UIFactory.Button(r2, "↺ 45", () => editor.RotateSelection(45), -1, 28, null, 12);
            UIFactory.Button(r2, "↻ 45", () => editor.RotateSelection(-45), -1, 28, null, 12);
            var r3 = UIFactory.Row(right, 30, 4);
            UIFactory.Button(r3, "Flip H (F)", () => editor.FlipSelection(true), -1, 28, null, 12);
            UIFactory.Button(r3, "Flip V (V)", () => editor.FlipSelection(false), -1, 28, null, 12);
            var r4 = UIFactory.Row(right, 30, 4);
            UIFactory.Button(r4, "Scale −", () => editor.ScaleSelection(0.5f), -1, 28, null, 12);
            UIFactory.Button(r4, "Scale +", () => editor.ScaleSelection(2f), -1, 28, null, 12);
            UIFactory.Button(r4, "Reset (R)", () =>
            {
                editor.placeRotation = 0;
                editor.placeFlipX = editor.placeFlipY = false;
                editor.placeScale = 1;
                editor.NotifyViewOptionsChanged();
            }, -1, 28, null, 12);
            transformLabel = UIFactory.Label(right, "", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            swipeToggle = UIFactory.Toggle(right, "Swipe to paint while dragging", editor.swipeBuild, v => editor.swipeBuild = v, 24);
            UIFactory.Label(right, "Click to place · Ctrl+click to select · Esc clears the brush", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            deleteStampButton = UIFactory.Button(right, "Delete this stamp", DeleteCurrentStamp, -1, 24, UIFactory.Danger, 11);
            deleteStampButton.gameObject.SetActive(false);

            ShowCategory(currentCategory);
            editor.ViewOptionsChanged += RefreshPlacement;
            editor.StampsChanged += () => { if (currentCategory == StampsCategory) ShowCategory(StampsCategory); };
            RefreshPlacement();
        }

        void OnSearchChanged(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                ShowCategory(currentCategory);
                return;
            }
            Populate(ObjectCatalog.Search(q));
            foreach (var kv in categoryButtons) UIFactory.SetButtonActive(kv.Value, false);
        }

        public void ShowCategory(string cat)
        {
            currentCategory = cat;
            foreach (var kv in categoryButtons) UIFactory.SetButtonActive(kv.Value, kv.Key == cat);
            if (categoryButton != null) UIFactory.SetButtonLabel(categoryButton, cat + "  ▾");
            if (cat == StampsCategory)
            {
                PopulateStamps();
                return;
            }
            Populate(ObjectCatalog.InCategory(cat));
        }

        void Populate(List<ObjectDefinition> defs)
        {
            foreach (Transform child in gridContent) Destroy(child.gameObject);
            tiles.Clear();
            stampTiles.Clear();
            foreach (var def in defs)
            {
                var d = def;
                var b = UIFactory.TileButton(gridContent, SpriteLibrary.ForObject(def), def.name, () =>
                {
                    editor.SetBuildDef(d);
                    if (editor.Mode != EditorMode.Build) editor.SetMode(EditorMode.Build);
                });
                tiles[def.id] = b;
            }
            gridScroll.verticalNormalizedPosition = 1f;
            gridScroll.horizontalNormalizedPosition = 0f;
            RefreshPlacement();
        }

        void RefreshPlacement()
        {
            if (selectedName == null) return;   // panel destroyed
            var def = editor.BuildDef;
            var stamp = editor.StampBrush;
            foreach (var kv in tiles) UIFactory.SetButtonActive(kv.Value, def != null && kv.Key == def.id);
            foreach (var kv in stampTiles) UIFactory.SetButtonActive(kv.Value, stamp != null && kv.Key == stamp.path);
            if (deleteStampButton != null)
            {
                bool showDelete = stamp != null && currentCategory == StampsCategory;
                if (deleteStampButton.gameObject.activeSelf != showDelete) deleteStampButton.gameObject.SetActive(showDelete);
                if (ui.IsPhone)
                {
                    // the phone column has no spare height: the transform readout gives way to the delete button
                    if (showDelete) UIFactory.Layout(deleteStampButton.gameObject, -1, 34);
                    if (transformLabel.gameObject.activeSelf == showDelete) transformLabel.gameObject.SetActive(!showDelete);
                }
            }
            if (stamp != null)
            {
                selectedName.text = "Stamp: " + stamp.name;
                selectedDesc.text = stamp.objects.Count + " objects · " + stamp.width.ToString("0.#") + "×" + stamp.height.ToString("0.#") + " · tap to place";
            }
            else
            {
                selectedName.text = def != null ? def.name : "No object selected";
                selectedDesc.text = def != null ? (string.IsNullOrEmpty(def.description) ? def.category + " · " + def.width + "×" + def.height : def.description) : "Pick an object from the grid.";
            }
            transformLabel.text = string.Format("rot {0:0}°  scale {1:0.##}x  {2}{3}", editor.placeRotation, editor.placeScale, editor.placeFlipX ? "flipH " : "", editor.placeFlipY ? "flipV" : "");
            if (swipeToggle != null) swipeToggle.SetIsOnWithoutNotify(editor.swipeBuild);
            if (swipeButton != null) UIFactory.SetButtonActive(swipeButton, editor.swipeBuild);
        }
    }
}
