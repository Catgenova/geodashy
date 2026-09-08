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

        public static PalettePanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "PalettePanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<PalettePanel>();
            p.ui = ui;
            p.editor = ui.editor;
            p.Build(rt);
            return p;
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
            foreach (var cat in ObjectCatalog.Categories)
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

            ShowCategory(currentCategory);
            editor.ViewOptionsChanged += RefreshPlacement;
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
            Populate(ObjectCatalog.InCategory(cat));
        }

        void Populate(List<ObjectDefinition> defs)
        {
            foreach (Transform child in gridContent) Destroy(child.gameObject);
            tiles.Clear();
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
            RefreshPlacement();
        }

        void RefreshPlacement()
        {
            var def = editor.BuildDef;
            foreach (var kv in tiles) UIFactory.SetButtonActive(kv.Value, def != null && kv.Key == def.id);
            selectedName.text = def != null ? def.name : "No object selected";
            selectedDesc.text = def != null ? (string.IsNullOrEmpty(def.description) ? def.category + " · " + def.width + "×" + def.height : def.description) : "Pick an object from the grid.";
            transformLabel.text = string.Format("rot {0:0}°  scale {1:0.##}x  {2}{3}", editor.placeRotation, editor.placeScale, editor.placeFlipX ? "flipH " : "", editor.placeFlipY ? "flipV" : "");
            if (swipeToggle != null) swipeToggle.SetIsOnWithoutNotify(editor.swipeBuild);
        }
    }
}
