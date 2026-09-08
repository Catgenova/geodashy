using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Gameplay;
using Geodashy.Editing.UI;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Geodashy.Editing
{
    /// <summary>
    /// The in-game level editor. Owns the level data, the scene views, selection, tools,
    /// undo/redo, clipboard, file operations and playtesting.
    /// </summary>
    public class LevelEditor : MonoBehaviour
    {
        public static LevelEditor Instance { get; private set; }

        enum DragState { None, Pan, BoxSelect, MoveSelection, SwipeBuild, SwipeDelete }

        // ---- scene ---------------------------------------------------------
        public Camera cam;
        public EditorCamera editorCamera;
        public EditorGrid grid;
        public ParallaxBackground background;
        public GroundRenderer ground;
        public EditorUI ui;
        public Transform objectsRoot;

        // ---- data ----------------------------------------------------------
        public LevelData level;
        public string currentFilePath;
        public bool Dirty { get; private set; }

        readonly Dictionary<int, LevelObjectView> views = new Dictionary<int, LevelObjectView>();
        readonly List<LevelObjectView> viewList = new List<LevelObjectView>();
        public IReadOnlyList<LevelObjectView> Views => viewList;

        // ---- tools ---------------------------------------------------------
        public EditorMode Mode { get; private set; } = EditorMode.Build;
        public ObjectDefinition BuildDef { get; private set; }
        public float placeRotation;
        public bool placeFlipX, placeFlipY;
        public float placeScale = 1f;
        public bool snapToGrid = true;
        public float gridSize = 1f;
        public bool swipeBuild = true;
        public bool swipeDelete = true;
        public bool deleteOnlyBuildType;
        public int currentEditorLayer;
        public bool showAllLayers = true;
        public float nudgeStep = 1f;

        public readonly HashSet<int> selection = new HashSet<int>();
        public readonly UndoStack undo = new UndoStack();
        public readonly EditorClipboard clipboard = new EditorClipboard();

        public event Action SelectionChanged;
        public event Action LevelChanged;
        public event Action ModeChanged;
        public event Action ViewOptionsChanged;

        public bool IsPlaying => runner != null;
        GameRunner runner;
        Vector2 cameraBeforePlay;
        float zoomBeforePlay;

        // ---- interaction state --------------------------------------------
        DragState drag = DragState.None;
        Vector2 dragStartScreen;
        Vector2 dragStartWorld;
        Vector2 lastSwipeCell = new Vector2(float.NaN, float.NaN);
        readonly Dictionary<int, Vector2> dragOriginalPositions = new Dictionary<int, Vector2>();
        bool dragMoved;
        int hoverUid = -1;
        LevelObjectView ghostView;
        SpriteRenderer ghost;
        float autosaveTimer;
        public bool uiHidden;
        public Vector2 CursorWorld { get; private set; }
        public Vector2 CursorSnapped { get; private set; }

        public const float AutosaveInterval = 60f;

        // =====================================================================
        // lifecycle
        // =====================================================================

        void Awake()
        {
            Instance = this;
        }

        public void Initialize(Camera camera)
        {
            cam = camera;
            editorCamera = camera.gameObject.GetComponent<EditorCamera>();
            if (editorCamera == null) editorCamera = camera.gameObject.AddComponent<EditorCamera>();
            editorCamera.Init(camera);

            objectsRoot = new GameObject("Level Objects").transform;
            objectsRoot.SetParent(transform, false);

            level = LevelStorage.LoadAutosave() ?? CreateStarterLevel();
            currentFilePath = null;

            background = ParallaxBackground.Create(transform, cam, level.settings);
            ground = GroundRenderer.Create(transform, cam, level.settings);
            grid = EditorGrid.Create(transform, editorCamera, this);
            editorCamera.minY = level.settings.groundY - 8f;

            var ghostGo = new GameObject("Ghost");
            ghostGo.transform.SetParent(transform, false);
            ghost = ghostGo.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(ghost);
            ghost.sortingOrder = 940;
            ghost.enabled = false;

            RebuildViews();
            editorCamera.Position = new Vector2(Mathf.Max(level.editorCameraX, 6f), Mathf.Max(level.editorCameraY, level.settings.groundY + 4f));
            editorCamera.SetZoom(level.editorZoom);

            ui = EditorUI.Create(this);
            SetMode(EditorMode.Build);
            SetBuildDef(ObjectCatalog.Get("castle_stone"));
            ApplySettings();
            Dirty = false;
        }

        static LevelData CreateStarterLevel()
        {
            var d = LevelData.CreateNew("My First Quest");
            d.settings.backgroundTheme = "castle";
            d.settings.groundTheme = "stone";
            d.settings.backgroundColor = ThemeCatalog.GetBackground("castle").skyBottom;
            return d;
        }

        // =====================================================================
        // views
        // =====================================================================

        public void RebuildViews()
        {
            foreach (var v in viewList) if (v != null) Destroy(v.gameObject);
            views.Clear();
            viewList.Clear();
            foreach (var o in level.objects) CreateView(o);
            RefreshSelectionVisuals();
            RefreshLayerVisibility();
        }

        LevelObjectView CreateView(LevelObject o)
        {
            var v = LevelObjectView.Create(objectsRoot, o, level, false);
            views[o.uid] = v;
            viewList.Add(v);
            return v;
        }

        void RemoveView(int uid)
        {
            if (!views.TryGetValue(uid, out var v)) return;
            views.Remove(uid);
            viewList.Remove(v);
            if (v != null) Destroy(v.gameObject);
        }

        public LevelObjectView GetView(int uid) => views.TryGetValue(uid, out var v) ? v : null;

        public void RefreshView(int uid)
        {
            var v = GetView(uid);
            if (v != null) v.Refresh();
        }

        public void RefreshAllViews()
        {
            foreach (var v in viewList) v.Refresh();
            RefreshLayerVisibility();
        }

        void RefreshLayerVisibility()
        {
            foreach (var v in viewList)
            {
                bool dim = !showAllLayers && v.data.editorLayer != currentEditorLayer;
                v.SetDimmed(dim);
            }
        }

        public bool IsLayerVisible(LevelObject o) => showAllLayers || o.editorLayer == currentEditorLayer;

        // =====================================================================
        // modes / tools
        // =====================================================================

        public void SetMode(EditorMode m)
        {
            Mode = m;
            drag = DragState.None;
            ModeChanged?.Invoke();
        }

        public void SetBuildDef(ObjectDefinition def)
        {
            BuildDef = def;
            if (ghost != null)
            {
                ghost.sprite = def != null ? SpriteLibrary.ForObject(def) : null;
            }
            ViewOptionsChanged?.Invoke();
        }

        public void SetEditorLayer(int layer)
        {
            currentEditorLayer = Mathf.Clamp(layer, 0, 99);
            RefreshLayerVisibility();
            ViewOptionsChanged?.Invoke();
        }

        public void SetShowAllLayers(bool all)
        {
            showAllLayers = all;
            RefreshLayerVisibility();
            ViewOptionsChanged?.Invoke();
        }

        public void SetGridSize(float g)
        {
            gridSize = Mathf.Clamp(g, 0.125f, 4f);
            grid.gridSize = gridSize;
            ViewOptionsChanged?.Invoke();
        }

        public void NotifyViewOptionsChanged() => ViewOptionsChanged?.Invoke();

        // =====================================================================
        // undo
        // =====================================================================

        string Snapshot() => LevelSerializer.ToJson(level, false);

        public void RecordUndo()
        {
            undo.Push(Snapshot());
        }

        void MarkChanged()
        {
            Dirty = true;
            level.modifiedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LevelChanged?.Invoke();
        }

        void RestoreSnapshot(string snap)
        {
            var d = LevelSerializer.FromJson(snap);
            level.objects = d.objects;
            level.colors = d.colors;
            level.settings = d.settings;
            level.nextUid = d.nextUid;
            level.name = d.name;
            level.author = d.author;
            level.description = d.description;
            background.settings = level.settings;
            ground.settings = level.settings;
            RebuildViews();
            var valid = new HashSet<int>();
            foreach (var o in level.objects) if (selection.Contains(o.uid)) valid.Add(o.uid);
            selection.Clear();
            selection.UnionWith(valid);
            ApplySettings();
            RefreshSelectionVisuals();
            MarkChanged();
            SelectionChanged?.Invoke();
        }

        public void Undo()
        {
            var s = undo.PopUndo(Snapshot());
            if (s == null)
            {
                ui.Toast("Nothing to undo");
                return;
            }
            RestoreSnapshot(s);
        }

        public void Redo()
        {
            var s = undo.PopRedo(Snapshot());
            if (s == null)
            {
                ui.Toast("Nothing to redo");
                return;
            }
            RestoreSnapshot(s);
        }

        // =====================================================================
        // object mutation
        // =====================================================================

        public LevelObject AddObject(ObjectDefinition def, Vector2 pos, bool record = true)
        {
            if (record) RecordUndo();
            var o = new LevelObject
            {
                uid = level.AllocateUid(),
                type = def.id,
                x = pos.x,
                y = pos.y,
                rotation = placeRotation,
                scaleX = placeScale,
                scaleY = placeScale,
                flipX = placeFlipX,
                flipY = placeFlipY,
                zLayer = def.defaultZLayer,
                zOrder = def.defaultZOrder,
                editorLayer = currentEditorLayer
            };
            foreach (var p in def.props) o.SetProp(p.key, p.defaultValue);
            level.objects.Add(o);
            CreateView(o);
            MarkChanged();
            return o;
        }

        public void AddObjects(List<LevelObject> objs, bool record = true, bool select = true)
        {
            if (record) RecordUndo();
            if (select) selection.Clear();
            foreach (var o in objs)
            {
                o.uid = level.AllocateUid();
                level.objects.Add(o);
                CreateView(o);
                if (select) selection.Add(o.uid);
            }
            RefreshLayerVisibility();
            RefreshSelectionVisuals();
            MarkChanged();
            if (select) SelectionChanged?.Invoke();
        }

        public void DeleteObjects(IEnumerable<int> uids, bool record = true)
        {
            var list = new List<int>(uids);
            if (list.Count == 0) return;
            if (record) RecordUndo();
            var set = new HashSet<int>(list);
            level.objects.RemoveAll(o => set.Contains(o.uid));
            foreach (var uid in list)
            {
                RemoveView(uid);
                selection.Remove(uid);
            }
            if (hoverUid >= 0 && set.Contains(hoverUid)) hoverUid = -1;
            MarkChanged();
            SelectionChanged?.Invoke();
        }

        public void DeleteSelection() => DeleteObjects(new List<int>(selection));

        public void DeleteAllOfType(string type)
        {
            var list = new List<int>();
            foreach (var o in level.objects) if (o.type == type) list.Add(o.uid);
            if (list.Count == 0)
            {
                ui.Toast("No objects of that type");
                return;
            }
            DeleteObjects(list);
            ui.Toast("Deleted " + list.Count + " objects");
        }

        /// <summary>Applies an edit to every selected object with a single undo step.</summary>
        public void EditSelection(Action<LevelObject> edit, bool refreshPanel = true)
        {
            if (selection.Count == 0) return;
            RecordUndo();
            foreach (var uid in selection)
            {
                var o = level.FindByUid(uid);
                if (o == null) continue;
                edit(o);
                RefreshView(uid);
            }
            RefreshLayerVisibility();
            RefreshSelectionVisuals();
            MarkChanged();
            if (refreshPanel) SelectionChanged?.Invoke();
        }

        public void EditObject(LevelObject o, Action<LevelObject> edit)
        {
            RecordUndo();
            edit(o);
            RefreshView(o.uid);
            RefreshSelectionVisuals();
            MarkChanged();
        }

        public List<LevelObject> SelectedObjects()
        {
            var list = new List<LevelObject>();
            foreach (var uid in selection)
            {
                var o = level.FindByUid(uid);
                if (o != null) list.Add(o);
            }
            return list;
        }

        public Vector2 SelectionCenter()
        {
            var objs = SelectedObjects();
            if (objs.Count == 0) return Vector2.zero;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var o in objs)
            {
                var v = GetView(o.uid);
                var b = v != null ? v.Bounds : new Rect(o.x, o.y, 0, 0);
                minX = Mathf.Min(minX, b.xMin);
                minY = Mathf.Min(minY, b.yMin);
                maxX = Mathf.Max(maxX, b.xMax);
                maxY = Mathf.Max(maxY, b.yMax);
            }
            return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        }

        public Rect SelectionBounds()
        {
            var objs = SelectedObjects();
            if (objs.Count == 0) return new Rect();
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var o in objs)
            {
                var v = GetView(o.uid);
                var b = v != null ? v.Bounds : new Rect(o.x, o.y, 0, 0);
                minX = Mathf.Min(minX, b.xMin);
                minY = Mathf.Min(minY, b.yMin);
                maxX = Mathf.Max(maxX, b.xMax);
                maxY = Mathf.Max(maxY, b.yMax);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        public void MoveSelection(Vector2 delta)
        {
            EditSelection(o =>
            {
                o.x += delta.x;
                o.y += delta.y;
            });
        }

        /// <summary>Rotates the selection; multiple objects rotate around the selection centre.</summary>
        public void RotateSelection(float degrees, bool aroundCenter = true)
        {
            if (selection.Count == 0)
            {
                placeRotation = GeoMath.NormalizeAngle(placeRotation + degrees);
                ViewOptionsChanged?.Invoke();
                return;
            }
            var center = SelectionCenter();
            bool multi = selection.Count > 1 && aroundCenter;
            EditSelection(o =>
            {
                if (multi)
                {
                    var p = GeoMath.Rotate(new Vector2(o.x, o.y) - center, degrees) + center;
                    o.x = p.x;
                    o.y = p.y;
                }
                o.rotation = GeoMath.NormalizeAngle(o.rotation + degrees);
            });
        }

        public void FlipSelection(bool horizontal)
        {
            if (selection.Count == 0)
            {
                if (horizontal) placeFlipX = !placeFlipX;
                else placeFlipY = !placeFlipY;
                ViewOptionsChanged?.Invoke();
                return;
            }
            var center = SelectionCenter();
            bool multi = selection.Count > 1;
            EditSelection(o =>
            {
                if (multi)
                {
                    if (horizontal) o.x = center.x - (o.x - center.x);
                    else o.y = center.y - (o.y - center.y);
                }
                // mirror rotation so the shape flips in place
                if (horizontal)
                {
                    o.flipX = !o.flipX;
                    o.rotation = GeoMath.NormalizeAngle(-o.rotation);
                }
                else
                {
                    o.flipY = !o.flipY;
                    o.rotation = GeoMath.NormalizeAngle(-o.rotation);
                }
            });
        }

        public void ScaleSelection(float factor)
        {
            if (selection.Count == 0)
            {
                placeScale = Mathf.Clamp(placeScale * factor, 0.125f, 16f);
                ViewOptionsChanged?.Invoke();
                return;
            }
            var center = SelectionCenter();
            bool multi = selection.Count > 1;
            EditSelection(o =>
            {
                if (multi)
                {
                    o.x = center.x + (o.x - center.x) * factor;
                    o.y = center.y + (o.y - center.y) * factor;
                }
                o.scaleX = Mathf.Clamp(o.scaleX * factor, 0.125f, 16f);
                o.scaleY = Mathf.Clamp(o.scaleY * factor, 0.125f, 16f);
            });
        }

        public void AlignSelection(string how)
        {
            if (selection.Count < 2) return;
            var b = SelectionBounds();
            EditSelection(o =>
            {
                var v = GetView(o.uid);
                var ob = v != null ? v.Bounds : new Rect(o.x, o.y, 0, 0);
                switch (how)
                {
                    case "left": o.x += b.xMin - ob.xMin; break;
                    case "right": o.x += b.xMax - ob.xMax; break;
                    case "bottom": o.y += b.yMin - ob.yMin; break;
                    case "top": o.y += b.yMax - ob.yMax; break;
                    case "centerX": o.x = b.center.x; break;
                    case "centerY": o.y = b.center.y; break;
                }
            });
        }

        public void SnapSelectionToGrid()
        {
            EditSelection(o =>
            {
                var def = ObjectCatalog.Get(o.type);
                var size = GeoMath.ObjectSize(o, def);
                if (Mathf.Abs(Mathf.DeltaAngle(o.rotation, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(o.rotation, 270f)) < 1f) size = new Vector2(size.y, size.x);
                var p = GeoMath.SnapCenter(new Vector2(o.x, o.y), size, gridSize);
                o.x = p.x;
                o.y = p.y;
            });
        }

        // =====================================================================
        // selection
        // =====================================================================

        public void Select(int uid, bool add)
        {
            if (!add) selection.Clear();
            selection.Add(uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        public void ToggleSelect(int uid)
        {
            if (!selection.Remove(uid)) selection.Add(uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        public void Deselect()
        {
            if (selection.Count == 0) return;
            selection.Clear();
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        public void SelectAll()
        {
            selection.Clear();
            foreach (var o in level.objects) if (IsLayerVisible(o)) selection.Add(o.uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        public void SelectByType(string type, bool add = false)
        {
            if (!add) selection.Clear();
            foreach (var o in level.objects) if (o.type == type && IsLayerVisible(o)) selection.Add(o.uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        public void SelectByGroup(int group, bool add = false)
        {
            if (!add) selection.Clear();
            foreach (var o in level.objects) if (o.InGroup(group) && IsLayerVisible(o)) selection.Add(o.uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
            ui.Toast("Selected " + selection.Count + " in group " + group);
        }

        public void SelectInRect(Rect r, bool add)
        {
            if (!add) selection.Clear();
            foreach (var v in viewList)
            {
                if (!IsLayerVisible(v.data)) continue;
                if (GeoMath.RectsOverlap(r, v.Bounds)) selection.Add(v.data.uid);
            }
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        public void InvertSelection()
        {
            var next = new HashSet<int>();
            foreach (var o in level.objects) if (!selection.Contains(o.uid) && IsLayerVisible(o)) next.Add(o.uid);
            selection.Clear();
            selection.UnionWith(next);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
        }

        void RefreshSelectionVisuals()
        {
            foreach (var v in viewList)
            {
                bool sel = selection.Contains(v.data.uid);
                bool hov = v.data.uid == hoverUid;
                if (sel) v.SetOutline(new Color(0.4f, 0.85f, 1f, 1f), true);
                else if (hov) v.SetOutline(new Color(1f, 1f, 1f, 0.5f), true);
                else v.SetOutline(Color.clear, false);
            }
        }

        /// <summary>Topmost visible object under a world point.</summary>
        public LevelObjectView PickObject(Vector2 world)
        {
            LevelObjectView best = null;
            float bestArea = float.MaxValue;
            int bestOrder = int.MinValue;
            foreach (var v in viewList)
            {
                if (!IsLayerVisible(v.data)) continue;
                if (!v.Bounds.Contains(world)) continue;
                if (!v.ContainsPoint(world, 0.02f)) continue;
                int order = v.renderer2D.sortingOrder;
                var s = v.Size;
                float area = s.x * s.y;
                if (order > bestOrder || (order == bestOrder && area < bestArea))
                {
                    best = v;
                    bestOrder = order;
                    bestArea = area;
                }
            }
            return best;
        }

        // =====================================================================
        // clipboard
        // =====================================================================

        public void CopySelection()
        {
            var objs = SelectedObjects();
            if (objs.Count == 0)
            {
                ui.Toast("Nothing selected");
                return;
            }
            clipboard.Copy(objs);
            ui.Toast("Copied " + objs.Count + " object" + (objs.Count == 1 ? "" : "s"));
        }

        public void CutSelection()
        {
            if (selection.Count == 0) return;
            clipboard.Copy(SelectedObjects());
            DeleteSelection();
        }

        public void Paste(bool atCursor)
        {
            if (!clipboard.HasItems && !clipboard.TryImportFromSystem())
            {
                ui.Toast("Clipboard is empty");
                return;
            }
            Vector2 center = atCursor && !ui.PointerOverUI ? CursorWorld : editorCamera.Position;
            if (snapToGrid) center = GeoMath.SnapCenter(center, Vector2.one, gridSize);
            var objs = clipboard.Paste(center, gridSize);
            foreach (var o in objs) o.editorLayer = currentEditorLayer;
            AddObjects(objs);
            SetMode(EditorMode.Edit);
        }

        public void DuplicateSelection()
        {
            var objs = SelectedObjects();
            if (objs.Count == 0) return;
            var clones = new List<LevelObject>();
            float offset = Mathf.Max(gridSize, 1f);
            var b = SelectionBounds();
            foreach (var o in objs)
            {
                var c = o.Clone();
                c.x += b.width > 0 ? Mathf.Ceil(b.width / offset) * offset : offset;
                clones.Add(c);
            }
            AddObjects(clones);
        }

        // =====================================================================
        // groups / colours / z helpers used by the UI
        // =====================================================================

        public void AddGroupToSelection(int group)
        {
            if (group <= 0) return;
            EditSelection(o => o.AddGroup(group));
        }

        public void RemoveGroupFromSelection(int group)
        {
            EditSelection(o => o.groups.Remove(group));
        }

        public int NextFreeGroup()
        {
            var used = new HashSet<int>();
            foreach (var o in level.objects) foreach (var g in o.groups) used.Add(g);
            int g = 1;
            while (used.Contains(g)) g++;
            return g;
        }

        // =====================================================================
        // settings / files
        // =====================================================================

        public void ApplySettings()
        {
            background.settings = level.settings;
            ground.settings = level.settings;
            background.ApplySettings();
            ground.ApplySettings();
            ground.showCeiling = false;
            cam.backgroundColor = level.settings.backgroundColor;
            editorCamera.minY = level.settings.groundY - 8f;
            RefreshAllViews();
        }

        /// <summary>Flags the level as modified and notifies listeners (used after settings edits).</summary>
        public void MarkDirty() => MarkChanged();

        public void NewLevel(string name)
        {
            RecordUndo();
            level = LevelData.CreateNew(string.IsNullOrWhiteSpace(name) ? "Untitled Quest" : name);
            level.settings.backgroundColor = ThemeCatalog.GetBackground(level.settings.backgroundTheme).skyBottom;
            currentFilePath = null;
            selection.Clear();
            undo.Clear();
            RebuildViews();
            ApplySettings();
            editorCamera.Position = new Vector2(6f, level.settings.groundY + 4f);
            editorCamera.SetZoom(1f);
            Dirty = false;
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
            ui.Toast("New level: " + level.name);
        }

        public void LoadLevel(LevelData data, string path)
        {
            level = data;
            currentFilePath = path;
            selection.Clear();
            undo.Clear();
            RebuildViews();
            ApplySettings();
            editorCamera.Position = new Vector2(Mathf.Max(level.editorCameraX, 6f), Mathf.Max(level.editorCameraY, level.settings.groundY + 4f));
            editorCamera.SetZoom(level.editorZoom);
            Dirty = false;
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
            ui.Toast("Loaded " + level.name);
        }

        public bool Save()
        {
            try
            {
                StoreEditorCamera();
                if (!string.IsNullOrEmpty(currentFilePath) && currentFilePath.StartsWith("res:"))
                {
                    // built-in levels are read-only: save a personal copy
                    level.id = Guid.NewGuid().ToString("N");
                    currentFilePath = null;
                }
                LevelStorage.Save(level);
                currentFilePath = LevelStorage.PathFor(level);
                Dirty = false;
                ui.Toast("Saved " + level.name);
                LevelChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ui.Toast("Save failed: " + e.Message);
                return false;
            }
        }

        public void SaveAs(string newName)
        {
            level.id = Guid.NewGuid().ToString("N");
            if (!string.IsNullOrWhiteSpace(newName)) level.name = newName;
            currentFilePath = null;
            Save();
        }

        void StoreEditorCamera()
        {
            level.editorCameraX = editorCamera.Position.x;
            level.editorCameraY = editorCamera.Position.y;
            level.editorZoom = editorCamera.zoom;
        }

        public void ExportToClipboard()
        {
            StoreEditorCamera();
            GUIUtility.systemCopyBuffer = LevelSerializer.ToJson(level, false);
            ui.Toast("Level JSON copied to clipboard");
        }

        public bool ImportFromClipboard()
        {
            var json = GUIUtility.systemCopyBuffer;
            if (!LevelSerializer.TryFromJson(json, out var data, out var err))
            {
                ui.Toast("Import failed: " + err);
                return false;
            }
            RecordUndo();
            LoadLevel(data, null);
            Dirty = true;
            return true;
        }

        // =====================================================================
        // playtest
        // =====================================================================

        public void SetPlaytestMarker(Vector2? pos)
        {
            if (pos == null)
            {
                level.playtestX = -1f;
                level.playtestY = -1f;
                ui.Toast("Playtest marker cleared");
            }
            else
            {
                level.playtestX = Mathf.Max(0f, pos.Value.x);
                level.playtestY = pos.Value.y;
                ui.Toast("Playtest marker set");
            }
            Dirty = true;
            LevelChanged?.Invoke();
        }

        public void StartPlaytest(bool fromMarker)
        {
            if (IsPlaying) return;
            StoreEditorCamera();
            Deselect();
            cameraBeforePlay = editorCamera.Position;
            zoomBeforePlay = editorCamera.zoom;
            drag = DragState.None;
            ghost.enabled = false;
            objectsRoot.gameObject.SetActive(false);
            grid.gameObject.SetActive(false);
            ui.SetPlayMode(true);

            Vector2? start = null;
            if (fromMarker)
            {
                if (level.playtestX >= 0f) start = new Vector2(level.playtestX, level.playtestY);
                else start = new Vector2(Mathf.Max(0f, editorCamera.Position.x - editorCamera.HalfWidth * 0.5f), level.settings.groundY + 0.5f);
            }

            var go = new GameObject("Game Runner");
            go.transform.SetParent(transform, false);
            runner = go.AddComponent<GameRunner>();
            runner.Begin(level.DeepClone(), cam, background, ground, start, StopPlaytest);
        }

        public void StopPlaytest()
        {
            if (runner == null) return;
            var r = runner;
            runner = null;
            r.Shutdown();
            Destroy(r.gameObject);
            objectsRoot.gameObject.SetActive(true);
            grid.gameObject.SetActive(true);
            editorCamera.Init(cam);
            editorCamera.SetZoom(zoomBeforePlay);
            editorCamera.Position = cameraBeforePlay;
            cam.transform.rotation = Quaternion.identity;
            ui.SetPlayMode(false);
            ApplySettings();
        }

        // =====================================================================
        // per-frame input
        // =====================================================================

        void Update()
        {
            if (IsPlaying)
            {
                var kb0 = Keyboard.current;
                if (kb0 != null && kb0[Key.Escape].wasPressedThisFrame && runner != null) runner.TogglePause();
                return;
            }
            if (ui == null) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool typing = ui.IsTyping;
            bool modal = ui.ModalOpen;

            if (kb != null && kb[Key.Escape].wasPressedThisFrame)
            {
                if (modal) ui.CloseTopModal();
                else if (drag != DragState.None) CancelDrag();
                else if (selection.Count > 0) Deselect();
                else if (Mode == EditorMode.Build && BuildDef != null) SetBuildDef(null);
            }

            if (!typing && !modal && kb != null) HandleHotkeys(kb);
            if (!modal && mouse != null) HandleMouse(mouse, kb);
            else ghost.enabled = false;

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer > AutosaveInterval)
            {
                autosaveTimer = 0f;
                if (Dirty)
                {
                    try
                    {
                        StoreEditorCamera();
                        LevelStorage.SaveAutosave(level);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Autosave failed: " + e.Message);
                    }
                }
            }
        }

        bool Ctrl(Keyboard kb) => kb.ctrlKey.isPressed || kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed;
        bool Shift(Keyboard kb) => kb.shiftKey.isPressed;
        bool Alt(Keyboard kb) => kb.altKey.isPressed;

        void HandleHotkeys(Keyboard kb)
        {
            bool ctrl = Ctrl(kb), shift = Shift(kb), alt = Alt(kb);

            // camera pan with WASD / arrows (arrows nudge when something is selected)
            var pan = Vector2.zero;
            if (kb[Key.W].isPressed) pan.y += 1;
            if (kb[Key.S].isPressed && !ctrl) pan.y -= 1;
            if (kb[Key.A].isPressed && !ctrl) pan.x -= 1;
            if (kb[Key.D].isPressed && !ctrl) pan.x += 1;
            if (selection.Count == 0)
            {
                if (kb[Key.UpArrow].isPressed) pan.y += 1;
                if (kb[Key.DownArrow].isPressed) pan.y -= 1;
                if (kb[Key.LeftArrow].isPressed) pan.x -= 1;
                if (kb[Key.RightArrow].isPressed) pan.x += 1;
            }
            editorCamera.PanKeys(pan, Time.unscaledDeltaTime * (shift ? 3f : 1f));

            if (selection.Count > 0)
            {
                float step = shift ? nudgeStep * 5f : (alt ? nudgeStep / 8f : nudgeStep);
                var nudge = Vector2.zero;
                if (kb[Key.UpArrow].wasPressedThisFrame) nudge.y += step;
                if (kb[Key.DownArrow].wasPressedThisFrame) nudge.y -= step;
                if (kb[Key.LeftArrow].wasPressedThisFrame) nudge.x -= step;
                if (kb[Key.RightArrow].wasPressedThisFrame) nudge.x += step;
                if (nudge != Vector2.zero) MoveSelection(nudge);
            }

            if (ctrl)
            {
                if (kb[Key.Z].wasPressedThisFrame)
                {
                    if (shift) Redo();
                    else Undo();
                }
                if (kb[Key.Y].wasPressedThisFrame) Redo();
                if (kb[Key.C].wasPressedThisFrame) CopySelection();
                if (kb[Key.X].wasPressedThisFrame) CutSelection();
                if (kb[Key.V].wasPressedThisFrame) Paste(true);
                if (kb[Key.D].wasPressedThisFrame) DuplicateSelection();
                if (kb[Key.A].wasPressedThisFrame) SelectAll();
                if (kb[Key.I].wasPressedThisFrame) InvertSelection();
                if (kb[Key.S].wasPressedThisFrame)
                {
                    if (shift) ui.PromptSaveAs();
                    else Save();
                }
                if (kb[Key.O].wasPressedThisFrame) ui.OpenFileDialog();
                if (kb[Key.G].wasPressedThisFrame)
                {
                    grid.showGrid = !grid.showGrid;
                    ViewOptionsChanged?.Invoke();
                }
                if (kb[Key.Digit0].wasPressedThisFrame || kb[Key.Numpad0].wasPressedThisFrame) editorCamera.SetZoom(1f);
                if (kb[Key.Enter].wasPressedThisFrame) StartPlaytest(true);
                return;
            }

            if (kb[Key.Digit1].wasPressedThisFrame) SetMode(EditorMode.Build);
            if (kb[Key.Digit2].wasPressedThisFrame) SetMode(EditorMode.Edit);
            if (kb[Key.Digit3].wasPressedThisFrame) SetMode(EditorMode.Delete);
            if (kb[Key.Tab].wasPressedThisFrame) SetMode((EditorMode)(((int)Mode + 1) % 3));

            if (kb[Key.Delete].wasPressedThisFrame || kb[Key.Backspace].wasPressedThisFrame) DeleteSelection();

            float rot = shift ? 45f : (alt ? 5f : 90f);
            if (kb[Key.Q].wasPressedThisFrame) RotateSelection(rot);
            if (kb[Key.E].wasPressedThisFrame) RotateSelection(-rot);
            if (kb[Key.F].wasPressedThisFrame) FlipSelection(true);
            if (kb[Key.V].wasPressedThisFrame) FlipSelection(false);
            if (kb[Key.G].wasPressedThisFrame)
            {
                snapToGrid = !snapToGrid;
                ui.Toast("Grid snap " + (snapToGrid ? "on" : "off"));
                ViewOptionsChanged?.Invoke();
            }
            if (kb[Key.Equals].wasPressedThisFrame || kb[Key.NumpadPlus].wasPressedThisFrame) editorCamera.SetZoom(editorCamera.zoom * 1.25f);
            if (kb[Key.Minus].wasPressedThisFrame || kb[Key.NumpadMinus].wasPressedThisFrame) editorCamera.SetZoom(editorCamera.zoom / 1.25f);
            if (kb[Key.LeftBracket].wasPressedThisFrame) SetEditorLayer(currentEditorLayer - 1);
            if (kb[Key.RightBracket].wasPressedThisFrame) SetEditorLayer(currentEditorLayer + 1);
            if (kb[Key.Backslash].wasPressedThisFrame) SetShowAllLayers(!showAllLayers);
            if (kb[Key.Home].wasPressedThisFrame) editorCamera.Position = new Vector2(6f, level.settings.groundY + 4f);
            if (kb[Key.End].wasPressedThisFrame) editorCamera.Position = new Vector2(level.GetFinishX(), level.settings.groundY + 4f);
            if (kb[Key.P].wasPressedThisFrame) StartPlaytest(shift);
            if (kb[Key.Enter].wasPressedThisFrame) StartPlaytest(false);
            if (kb[Key.M].wasPressedThisFrame)
            {
                if (shift) SetPlaytestMarker(null);
                else SetPlaytestMarker(ui.PointerOverUI ? editorCamera.Position : CursorSnapped);
            }
            if (kb[Key.H].wasPressedThisFrame)
            {
                uiHidden = !uiHidden;
                ui.SetHidden(uiHidden);
            }
            if (kb[Key.F1].wasPressedThisFrame) ui.OpenHelp();
            if (kb[Key.T].wasPressedThisFrame && selection.Count == 1)
            {
                var o = level.FindByUid(new List<int>(selection)[0]);
                if (o != null) SelectByType(o.type, false);
            }
            if (kb[Key.R].wasPressedThisFrame)
            {
                placeRotation = 0f;
                placeFlipX = placeFlipY = false;
                placeScale = 1f;
                ViewOptionsChanged?.Invoke();
                ui.Toast("Placement transform reset");
            }
        }

        void HandleMouse(Mouse mouse, Keyboard kb)
        {
            var screen = mouse.position.ReadValue();
            CursorWorld = editorCamera.ScreenToWorld(screen);
            bool overUI = ui.PointerOverUI;
            bool shift = kb != null && Shift(kb);
            bool ctrl = kb != null && Ctrl(kb);
            bool space = kb != null && kb[Key.Space].isPressed;

            // snapped cursor for placement
            var size = BuildDef != null ? new Vector2(BuildDef.width * placeScale, BuildDef.height * placeScale) : Vector2.one;
            if (Mathf.Abs(Mathf.DeltaAngle(placeRotation, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(placeRotation, 270f)) < 1f) size = new Vector2(size.y, size.x);
            CursorSnapped = snapToGrid ? GeoMath.SnapCenter(CursorWorld, size, gridSize) : CursorWorld;

            // zoom -------------------------------------------------------------
            float scroll = mouse.scroll.ReadValue().y;
            if (!overUI && Mathf.Abs(scroll) > 0.01f)
            {
                float factor = scroll > 0 ? 1.15f : 1f / 1.15f;
                editorCamera.ZoomAt(editorCamera.zoom * factor, screen);
                ViewOptionsChanged?.Invoke();
            }

            // pan with middle / right mouse or space+left -------------------------
            bool panButton = mouse.middleButton.isPressed || mouse.rightButton.isPressed || (space && mouse.leftButton.isPressed);
            if (drag == DragState.None && panButton && !overUI && (mouse.middleButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame))
            {
                drag = DragState.Pan;
                editorCamera.BeginDragPan(screen);
            }
            if (drag == DragState.Pan)
            {
                editorCamera.DragPan(screen);
                if (!panButton)
                {
                    editorCamera.EndDragPan();
                    drag = DragState.None;
                }
                ghost.enabled = false;
                return;
            }

            // hover --------------------------------------------------------------
            if (drag == DragState.None)
            {
                var hv = overUI ? null : PickObject(CursorWorld);
                int newHover = hv != null ? hv.data.uid : -1;
                if (newHover != hoverUid)
                {
                    hoverUid = newHover;
                    RefreshSelectionVisuals();
                }
            }

            // ghost preview ------------------------------------------------------
            bool showGhost = Mode == EditorMode.Build && BuildDef != null && !overUI && drag == DragState.None;
            ghost.enabled = showGhost;
            if (showGhost)
            {
                ghost.transform.position = new Vector3(CursorSnapped.x, CursorSnapped.y, 0f);
                ghost.transform.rotation = Quaternion.Euler(0, 0, placeRotation);
                ghost.transform.localScale = new Vector3(placeScale * (placeFlipX ? -1 : 1), placeScale * (placeFlipY ? -1 : 1), 1f);
                ghost.color = new Color(1f, 1f, 1f, 0.5f);
            }

            // left button ----------------------------------------------------------
            if (mouse.leftButton.wasPressedThisFrame && !overUI && drag == DragState.None)
            {
                dragStartScreen = screen;
                dragStartWorld = CursorWorld;
                dragMoved = false;
                BeginLeftPress(shift, ctrl);
            }
            else if (mouse.leftButton.isPressed && drag != DragState.None)
            {
                if ((screen - dragStartScreen).sqrMagnitude > 9f) dragMoved = true;
                ContinueLeftDrag(shift);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && drag != DragState.None)
            {
                EndLeftDrag(shift);
            }
        }

        void BeginLeftPress(bool shift, bool ctrl)
        {
            var hit = PickObject(CursorWorld);
            switch (Mode)
            {
                case EditorMode.Build:
                    if (BuildDef != null && !ctrl)
                    {
                        RecordUndo();
                        PlaceAt(CursorSnapped);
                        lastSwipeCell = CursorSnapped;
                        drag = DragState.SwipeBuild;
                        return;
                    }
                    BeginSelectPress(hit, shift);
                    break;
                case EditorMode.Edit:
                    BeginSelectPress(hit, shift);
                    break;
                case EditorMode.Delete:
                    RecordUndo();
                    drag = DragState.SwipeDelete;
                    dragMoved = false;
                    if (hit != null && DeleteAllowed(hit)) DeleteObjects(new[] { hit.data.uid }, false);
                    break;
            }
        }

        bool DeleteAllowed(LevelObjectView v)
        {
            if (!deleteOnlyBuildType || BuildDef == null) return true;
            return v.data.type == BuildDef.id;
        }

        void BeginSelectPress(LevelObjectView hit, bool shift)
        {
            if (hit == null)
            {
                drag = DragState.BoxSelect;
                return;
            }
            int uid = hit.data.uid;
            if (shift)
            {
                ToggleSelect(uid);
                if (!selection.Contains(uid)) return;
            }
            else if (!selection.Contains(uid))
            {
                Select(uid, false);
            }
            // begin move
            drag = DragState.MoveSelection;
            dragOriginalPositions.Clear();
            foreach (var s in selection)
            {
                var o = level.FindByUid(s);
                if (o != null) dragOriginalPositions[s] = new Vector2(o.x, o.y);
            }
            RecordUndo();
        }

        void PlaceAt(Vector2 pos)
        {
            // avoid stacking identical objects on the same spot while swiping
            foreach (var v in viewList)
            {
                if (v.data.type == BuildDef.id && Mathf.Abs(v.data.x - pos.x) < 0.01f && Mathf.Abs(v.data.y - pos.y) < 0.01f &&
                    Mathf.Approximately(v.data.rotation, placeRotation)) return;
            }
            AddObject(BuildDef, pos, false);
        }

        void ContinueLeftDrag(bool shift)
        {
            switch (drag)
            {
                case DragState.SwipeBuild:
                    if (swipeBuild && (CursorSnapped - lastSwipeCell).sqrMagnitude > 0.0001f)
                    {
                        PlaceAt(CursorSnapped);
                        lastSwipeCell = CursorSnapped;
                    }
                    break;
                case DragState.SwipeDelete:
                {
                    if (!swipeDelete) break;
                    var hit = PickObject(CursorWorld);
                    if (hit != null && DeleteAllowed(hit)) DeleteObjects(new[] { hit.data.uid }, false);
                    break;
                }
                case DragState.BoxSelect:
                    grid.SetSelectionBox(GeoMath.RectFromPoints(dragStartWorld, CursorWorld));
                    break;
                case DragState.MoveSelection:
                {
                    if (!dragMoved) break;
                    var delta = CursorWorld - dragStartWorld;
                    if (snapToGrid) delta = new Vector2(GeoMath.SnapValue(delta.x, gridSize), GeoMath.SnapValue(delta.y, gridSize));
                    foreach (var kv in dragOriginalPositions)
                    {
                        var o = level.FindByUid(kv.Key);
                        if (o == null) continue;
                        o.x = kv.Value.x + delta.x;
                        o.y = kv.Value.y + delta.y;
                        var v = GetView(kv.Key);
                        if (v != null) v.ApplyTransform();
                    }
                    break;
                }
            }
        }

        void EndLeftDrag(bool shift)
        {
            switch (drag)
            {
                case DragState.BoxSelect:
                    grid.SetSelectionBox(null);
                    if (dragMoved) SelectInRect(GeoMath.RectFromPoints(dragStartWorld, CursorWorld), shift);
                    else if (!shift) Deselect();
                    break;
                case DragState.MoveSelection:
                {
                    bool changed = false;
                    foreach (var kv in dragOriginalPositions)
                    {
                        var o = level.FindByUid(kv.Key);
                        if (o != null && (Mathf.Abs(o.x - kv.Value.x) > 0.0001f || Mathf.Abs(o.y - kv.Value.y) > 0.0001f)) changed = true;
                    }
                    if (changed)
                    {
                        MarkChanged();
                        RefreshSelectionVisuals();
                        SelectionChanged?.Invoke();
                    }
                    else undo.DiscardLast();
                    break;
                }
                case DragState.SwipeDelete:
                    break;
            }
            drag = DragState.None;
        }

        void CancelDrag()
        {
            if (drag == DragState.MoveSelection)
            {
                foreach (var kv in dragOriginalPositions)
                {
                    var o = level.FindByUid(kv.Key);
                    if (o == null) continue;
                    o.x = kv.Value.x;
                    o.y = kv.Value.y;
                    RefreshView(kv.Key);
                }
                undo.DiscardLast();
            }
            grid.SetSelectionBox(null);
            drag = DragState.None;
        }

        void OnApplicationQuit()
        {
            if (level != null && Dirty)
            {
                try
                {
                    StoreEditorCamera();
                    LevelStorage.SaveAutosave(level);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
