using Geodashy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Delete-mode dock.</summary>
    public class DeletePanel : MonoBehaviour
    {
        EditorUI ui;
        LevelEditor editor;
        Text filterLabel;

        public static DeletePanel Create(EditorUI ui, RectTransform dock)
        {
            var rt = UIFactory.Rect(dock, "DeletePanel");
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<DeletePanel>();
            p.ui = ui;
            p.editor = ui.editor;
            if (ui.IsPhone) p.BuildPhone(rt);
            else p.Build(rt);
            return p;
        }

        void Build(RectTransform rt)
        {
            UIFactory.HLayout(rt, 10, 8, false, TextAnchor.UpperLeft);
            rt.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            var c1 = UIFactory.Column(rt, 280, 6, 4);
            UIFactory.SectionHeader(c1, "Delete");
            UIFactory.Label(c1, "Click an object to delete it. Drag to sweep.", 13, TextAnchor.UpperLeft, UIFactory.TextColor, -1, 24);
            UIFactory.Toggle(c1, "Sweep-delete while dragging", editor.swipeDelete, v => editor.swipeDelete = v, 26);
            UIFactory.Toggle(c1, "Only delete the current brush type", editor.deleteOnlyBuildType, v => editor.deleteOnlyBuildType = v, 26);
            filterLabel = UIFactory.Label(c1, "", 12, TextAnchor.UpperLeft, UIFactory.TextDim, -1, 22);

            var c2 = UIFactory.Column(rt, 260, 6, 4);
            UIFactory.SectionHeader(c2, "Bulk");
            UIFactory.Button(c2, "Delete selection", editor.DeleteSelection, -1, 30, UIFactory.Danger, 13);
            UIFactory.Button(c2, "Delete all of brush type", DeleteBrushType, -1, 30, UIFactory.Danger, 13);
            UIFactory.Button(c2, "Delete all triggers", DeleteTriggers, -1, 30, UIFactory.Danger, 13);
            UIFactory.Button(c2, "Delete all start positions", DeleteStartPositions, -1, 30, UIFactory.Danger, 13);
            UIFactory.Button(c2, "Delete all waystones", DeleteWaystones, -1, 30, UIFactory.Danger, 13);
            UIFactory.Button(c2, "Clear entire level", ClearLevel, -1, 30, UIFactory.Danger, 13);

            editor.ViewOptionsChanged += Refresh;
            Refresh();
        }

        /// <summary>Phone dock: two toggles on the left, the bulk actions as two rows of finger-sized buttons.</summary>
        void BuildPhone(RectTransform rt)
        {
            var scroll = UIFactory.ScrollView(rt, "Scroll", out var c, false, true, Color.clear);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>());
            UIFactory.HLayout(c, 10, 6, false, TextAnchor.UpperLeft);
            c.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            UIFactory.Fitter(c, false, true);
            const float h = 44f;

            var c1 = UIFactory.Column(c, 260, 4, 0);
            UIFactory.Label(c1, "Tap an object to delete it. Drag to sweep.", 12, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, 18);
            UIFactory.Toggle(c1, "Sweep while dragging", editor.swipeDelete, v => editor.swipeDelete = v, 36);
            UIFactory.Toggle(c1, "Only the brush type", editor.deleteOnlyBuildType, v => editor.deleteOnlyBuildType = v, 36);
            filterLabel = UIFactory.Label(c1, "", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 18);

            var c2 = UIFactory.Column(c, 470, 4, 0);
            UIFactory.Label(c2, "Bulk", 11, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 16);
            var r1 = UIFactory.Row(c2, h, 4);
            UIFactory.Button(r1, "Selection", editor.DeleteSelection, -1, h, UIFactory.Danger, 12);
            UIFactory.Button(r1, "All of brush", DeleteBrushType, -1, h, UIFactory.Danger, 12);
            UIFactory.Button(r1, "All triggers", DeleteTriggers, -1, h, UIFactory.Danger, 12);
            var r2 = UIFactory.Row(c2, h, 4);
            UIFactory.Button(r2, "All starts", DeleteStartPositions, -1, h, UIFactory.Danger, 12);
            UIFactory.Button(r2, "All waystones", DeleteWaystones, -1, h, UIFactory.Danger, 12);
            UIFactory.Button(r2, "Clear level", ClearLevel, -1, h, UIFactory.Danger, 12);

            editor.ViewOptionsChanged += Refresh;
            Refresh();
        }

        void DeleteBrushType()
        {
            if (editor.BuildDef == null)
            {
                ui.Toast("Pick a brush object in Build mode first");
                return;
            }
            var def = editor.BuildDef;
            ui.Confirm("Delete all " + def.name + "?", "Every " + def.name + " in the level will be removed. You can undo this.", () => editor.DeleteAllOfType(def.id), "Delete");
        }

        void DeleteTriggers()
        {
            ui.Confirm("Delete all triggers?", "Every trigger object will be removed. You can undo this.", () =>
            {
                var list = new System.Collections.Generic.List<int>();
                foreach (var o in editor.level.objects)
                {
                    var d = ObjectCatalog.Get(o.type);
                    if (d != null && d.kind == ObjectKind.Trigger) list.Add(o.uid);
                }
                editor.DeleteObjects(list);
            }, "Delete");
        }

        void DeleteStartPositions()
        {
            int n = 0;
            foreach (var o in editor.level.objects) if (o.type == "start_pos") n++;
            if (n == 0)
            {
                ui.Toast("No start positions in the level");
                return;
            }
            editor.DeleteAllOfType("start_pos");
        }

        void DeleteWaystones()
        {
            ui.Confirm("Delete all waystones?", "Every Waystone checkpoint will be removed. You can undo this.", () => editor.DeleteAllOfType("checkpoint"), "Delete");
        }

        void ClearLevel()
        {
            ui.Confirm("Clear the level?", "All objects will be removed. Settings are kept. You can undo this.", () =>
            {
                var list = new System.Collections.Generic.List<int>();
                foreach (var o in editor.level.objects) list.Add(o.uid);
                editor.DeleteObjects(list);
            }, "Clear");
        }

        void Refresh()
        {
            if (filterLabel == null) return;
            filterLabel.text = editor.BuildDef != null ? "Brush: " + editor.BuildDef.name : "Brush: none";
        }
    }
}
