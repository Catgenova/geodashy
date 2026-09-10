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

        enum DragState { None, Pan, BoxSelect, MoveSelection, SwipeBuild, SwipeDelete, Gizmo }

        struct TransformState
        {
            public float x, y, rotation, scaleX, scaleY;
        }

        // ---- scene ---------------------------------------------------------
        public Camera cam;
        public EditorCamera editorCamera;
        public EditorGrid grid;
        public ParallaxBackground background;
        public GroundRenderer ground;
        public GroundProps groundProps;
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
        /// <summary>Properties and colour a favourite preset applies to every object placed with the current brush (null when none).</summary>
        public BrushPreset ActivePreset { get; private set; }
        public void ApplyPreset(BrushPreset p)
        {
            var def = p != null ? ObjectCatalog.Get(p.type) : null;
            if (def == null)
            {
                ui.Toast("That preset's object no longer exists");
                return;
            }
            SetBuildDef(def);
            ActivePreset = p;
            placeRotation = p.rotation;
            placeScale = Mathf.Clamp(p.scale, 0.125f, 16f);
            placeFlipX = p.flipX;
            placeFlipY = p.flipY;
            if (Mode != EditorMode.Build) SetMode(EditorMode.Build);
            ViewOptionsChanged?.Invoke();
        }
        /// <summary>The current brush and transform as a preset that can be saved to the Favourites shelf.</summary>
        public BrushPreset CurrentBrushAsPreset(string name)
        {
            if (BuildDef == null) return null;
            var p = new BrushPreset { name = name, type = BuildDef.id, rotation = placeRotation, scale = placeScale, flipX = placeFlipX, flipY = placeFlipY };
            if (ActivePreset != null && ActivePreset.type == BuildDef.id)
            {
                p.baseColor = ActivePreset.baseColor;
                foreach (var pr in ActivePreset.props) p.props.Add(new ObjectProp(pr.key, pr.value));
            }
            else
            {
                // a single selected object of the brush type lends its properties and colour
                var sel = SelectedObjects();
                if (sel.Count == 1 && sel[0].type == BuildDef.id)
                {
                    p.baseColor = sel[0].baseColor;
                    foreach (var pr in sel[0].props) p.props.Add(new ObjectProp(pr.key, pr.value));
                }
            }
            return p;
        }
        void ApplyPresetProps(LevelObject o)
        {
            if (ActivePreset == null || o.type != ActivePreset.type) return;
            foreach (var pr in ActivePreset.props) o.SetProp(pr.key, pr.value);
            if (ActivePreset.baseColor != 0) o.baseColor = ActivePreset.baseColor;
        }
        public bool snapToGrid = true;
        public float gridSize = 1f;
        public bool swipeBuild = true;
        public bool swipeDelete = true;
        public bool deleteOnlyBuildType;
        /// <summary>Keep safe/danger edges drawn on every placed object while editing.</summary>
        public bool showHitboxes;
        HitboxOverlay hitboxOverlay;
        /// <summary>Snap placed objects' x to the song's beat grid (see beatDivision).</summary>
        public bool beatSnap;
        public int beatDivision = 1;

        // ---- defeat heatmap -----------------------------------------------------
        public struct DeathRecord
        {
            public Vector2 position;
            public Vector2 contact;
            public string killer;
            public float time;
        }

        /// <summary>Every death from playtests started in this editing session.</summary>
        public readonly List<DeathRecord> sessionDeaths = new List<DeathRecord>();
        public bool showDeathHeatmap;

        // ---- groups hidden / locked while editing --------------------------------
        public readonly HashSet<int> hiddenGroups = new HashSet<int>();
        public readonly HashSet<int> lockedGroups = new HashSet<int>();
        public bool IsHidden(LevelObject o)
        {
            if (hiddenGroups.Count == 0) return false;
            foreach (var g in o.groups) if (hiddenGroups.Contains(g)) return true;
            return false;
        }
        public bool IsLocked(LevelObject o)
        {
            if (lockedGroups.Count == 0) return false;
            foreach (var g in o.groups) if (lockedGroups.Contains(g)) return true;
            return false;
        }
        /// <summary>Visible on the current layer, not in a hidden or locked group.</summary>
        public bool IsSelectable(LevelObject o) => IsLayerVisible(o) && !IsHidden(o) && !IsLocked(o);
        public void SetGroupHidden(int g, bool hidden)
        {
            if (hidden) hiddenGroups.Add(g); else hiddenGroups.Remove(g);
            if (hidden) foreach (var o in level.objects) if (selection.Contains(o.uid) && IsHidden(o)) selection.Remove(o.uid);
            RefreshLayerVisibility();
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
            ViewOptionsChanged?.Invoke();
        }
        public void SetGroupLocked(int g, bool locked)
        {
            if (locked) lockedGroups.Add(g); else lockedGroups.Remove(g);
            if (locked) foreach (var o in level.objects) if (selection.Contains(o.uid) && IsLocked(o)) selection.Remove(o.uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
            ViewOptionsChanged?.Invoke();
        }
        /// <summary>Every group id used by at least one object, ascending.</summary>
        public List<int> UsedGroups()
        {
            var set = new HashSet<int>();
            foreach (var o in level.objects) foreach (var g in o.groups) if (g > 0) set.Add(g);
            var list = new List<int>(set);
            list.Sort();
            return list;
        }

        // ---- snapping guides -----------------------------------------------------
        public const string SnapGuidesPref = "geodashy.snapGuides";
        public bool snapGuides = PlayerPrefs.GetInt(SnapGuidesPref, 1) == 1;
        readonly List<float> guideXs = new List<float>();
        readonly List<float> guideYs = new List<float>();

        // ---- stamps ------------------------------------------------------------------
        public Stamp StampBrush { get; private set; }
        public void SetStampBrush(Stamp stamp)
        {
            StampBrush = stamp;
            if (stamp != null)
            {
                BuildDef = null;
                ghost.enabled = false;
                if (Mode != EditorMode.Build) SetMode(EditorMode.Build);
            }
            ViewOptionsChanged?.Invoke();
        }
        public bool SaveSelectionAsStamp(string name)
        {
            var objs = SelectedObjects();
            if (objs.Count == 0) return false;
            var stamp = Stamp.FromObjects(name, objs);
            StampStorage.Save(stamp);
            Achievements.Unlock("stamp_saved");
            StampsChanged?.Invoke();
            return true;
        }
        public event Action StampsChanged;
        public void NotifyStampsChanged() => StampsChanged?.Invoke();

        // ---- object budget -------------------------------------------------------------
        public const int BudgetWarn = 1500;
        public const int BudgetHigh = 3000;
        /// <summary>Object count per palette shelf, largest first.</summary>
        public List<KeyValuePair<string, int>> CountByCategory()
        {
            var counts = new Dictionary<string, int>();
            foreach (var o in level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                var cat = d != null ? d.category : "?";
                counts[cat] = counts.TryGetValue(cat, out var c) ? c + 1 : 1;
            }
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            return list;
        }

        // ---- song end flag: where the soundtrack runs out at the level's speeds --------------
        public const string SongEndPref = "geodashy.songEndFlag";
        public bool showSongEnd = PlayerPrefs.GetInt(SongEndPref, 1) == 1;
        /// <summary>Level x where the song ends, or null when there is no song or its length is not known yet.</summary>
        public float? SongEndX { get; private set; }
        public float SongLength { get; private set; } = -1f;
        /// <summary>Loudness per slice of the whole song (0..1), for the timeline strip; null until the clip is known.</summary>
        public float[] SongWaveform { get; private set; }
        /// <summary>The loaded song clip, when it is available in memory (built-in songs and imported files after loading).</summary>
        public AudioClip SongClip { get; private set; }
        public event Action SongChanged;

        /// <summary>Seconds of travel from the spawn to level x, walking through every speed portal.</summary>
        public float TravelTimeToX(float targetX)
        {
            var portals = new List<KeyValuePair<float, int>>();
            foreach (var o in level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d != null && d.kind == ObjectKind.Portal && d.portalType == PortalType.Speed) portals.Add(new KeyValuePair<float, int>(o.x, (int)d.portalSpeed));
            }
            portals.Sort((a, b) => a.Key.CompareTo(b.Key));
            float x = 0f, t = 0f;
            float speed = MountCatalog.Speed(level.settings.startSpeed);
            foreach (var p in portals)
            {
                if (p.Key >= targetX) break;
                if (p.Key > x)
                {
                    t += (p.Key - x) / speed;
                    x = p.Key;
                }
                speed = MountCatalog.Speed(p.Value);
            }
            if (targetX > x) t += (targetX - x) / speed;
            return t;
        }
        string songKey = "";
        int songRequest;

        /// <summary>Loads the song's length when the song changed, then recomputes the flag.</summary>
        public void RefreshSongLength()
        {
            var s = level.settings;
            string key = !string.IsNullOrEmpty(s.songFile) ? "file:" + level.id + "/" + s.songFile : (!string.IsNullOrEmpty(s.songId) ? "id:" + s.songId : "");
            if (key == songKey)
            {
                RecomputeSongEnd();
                return;
            }
            songKey = key;
            SongLength = -1f;
            SongEndX = null;
            SongWaveform = null;
            SongClip = null;
            int request = ++songRequest;
            if (key.StartsWith("file:"))
            {
                var path = LevelStorage.AssetPath(level.id, s.songFile);
                AudioLoader.Load(this, path, clip =>
                {
                    if (request != songRequest || this == null) return;
                    SongClip = clip;
                    SongLength = clip != null ? clip.length : -1f;
                    SongWaveform = clip != null ? BeatDetector.Waveform(clip, 2048) : null;
                    RecomputeSongEnd();
                    ViewOptionsChanged?.Invoke();
                    SongChanged?.Invoke();
                });
            }
            else if (key.StartsWith("id:"))
            {
                var clip = Resources.Load<AudioClip>("Songs/" + s.songId);
                SongClip = clip;
                SongLength = clip != null ? clip.length : -1f;
                SongWaveform = clip != null ? BeatDetector.Waveform(clip, 2048) : null;
            }
            RecomputeSongEnd();
            ViewOptionsChanged?.Invoke();
            SongChanged?.Invoke();
        }

        /// <summary>Walks the speed portals from the start and finds the x reached when the song runs out.</summary>
        public void RecomputeSongEnd()
        {
            float remaining = SongLength - level.settings.songOffset;
            if (SongLength <= 0f || remaining <= 0f)
            {
                SongEndX = null;
                return;
            }
            var portals = new List<KeyValuePair<float, int>>();
            foreach (var o in level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d != null && d.kind == ObjectKind.Portal && d.portalType == PortalType.Speed) portals.Add(new KeyValuePair<float, int>(o.x, (int)d.portalSpeed));
            }
            portals.Sort((a, b) => a.Key.CompareTo(b.Key));
            float x = 0f;
            float speed = MountCatalog.Speed(level.settings.startSpeed);
            foreach (var p in portals)
            {
                if (p.Key <= x) { speed = MountCatalog.Speed(p.Value); continue; }
                float dt = (p.Key - x) / speed;
                if (dt >= remaining)
                {
                    SongEndX = x + remaining * speed;
                    return;
                }
                remaining -= dt;
                x = p.Key;
                speed = MountCatalog.Speed(p.Value);
            }
            SongEndX = x + remaining * speed;
        }

        /// <summary>Drops a Finish Banner object where the song ends.</summary>
        public void PlaceSongEndBanner()
        {
            if (!SongEndX.HasValue)
            {
                ui.Toast(SongLength <= 0f ? "No song set (or still loading): pick one in Level Settings" : "The song ends before the level starts; lower the song offset");
                return;
            }
            var def = ObjectCatalog.Get("finish_flag");
            float x = snapToGrid ? Mathf.Round(SongEndX.Value) : SongEndX.Value;
            var o = AddObject(def, new Vector2(x, level.settings.groundY + def.height / 2f));
            selection.Clear();
            selection.Add(o.uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
            editorCamera.Position = new Vector2(x, editorCamera.Position.y);
            ui.Toast("Finish banner placed at x " + x.ToString("0.#") + " where the song ends");
        }

        // ---- last playtest trace ---------------------------------------------------------
        public readonly List<Vector2> lastRunTrace = new List<Vector2>();
        public Vector2? lastRunDeath;
        /// <summary>Every jump of the last sync-check playtest: x, y and how far (in beats, -0.5..0.5) it was from the nearest beat.</summary>
        public readonly List<Vector3> lastRunJumps = new List<Vector3>();
        public bool lastRunWasSyncCheck;
        public const string RunTracePref = "geodashy.runTrace";
        public bool showRunTrace = PlayerPrefs.GetInt(RunTracePref, 1) == 1;

        // ---- path tool: click points, lay the brush along them ---------------------------
        public bool pathTool;
        /// <summary>When on, the path points are joined by a smooth Catmull-Rom curve instead of straight lines.</summary>
        public bool pathCurve = PlayerPrefs.GetInt("geodashy.pathCurve", 0) == 1;
        public readonly List<Vector2> pathPoints = new List<Vector2>();
        public void SetPathCurve(bool on)
        {
            pathCurve = on;
            PlayerPrefs.SetInt("geodashy.pathCurve", on ? 1 : 0);
            ViewOptionsChanged?.Invoke();
        }
        /// <summary>The path as a polyline: the clicked points, or a Catmull-Rom spline through them when the curve tool is on.</summary>
        public List<Vector2> PathPolyline()
        {
            if (!pathCurve || pathPoints.Count < 3) return new List<Vector2>(pathPoints);
            var pts = new List<Vector2>();
            int n = pathPoints.Count;
            for (int i = 0; i < n - 1; i++)
            {
                var p0 = pathPoints[Mathf.Max(0, i - 1)];
                var p1 = pathPoints[i];
                var p2 = pathPoints[i + 1];
                var p3 = pathPoints[Mathf.Min(n - 1, i + 2)];
                int segs = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(p1, p2) * 4f), 4, 64);
                for (int k = 0; k < segs; k++)
                {
                    float t = k / (float)segs, t2 = t * t, t3 = t2 * t;
                    var p = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                    pts.Add(p);
                }
            }
            pts.Add(pathPoints[n - 1]);
            return pts;
        }
        public void SetPathTool(bool on)
        {
            pathTool = on;
            if (!on) pathPoints.Clear();
            if (on && Mode != EditorMode.Build) SetMode(EditorMode.Build);
            ViewOptionsChanged?.Invoke();
        }
        public void ClearPath()
        {
            pathPoints.Clear();
            ViewOptionsChanged?.Invoke();
        }
        /// <summary>Places the brush along the clicked polyline at a spacing (grid size, or the beat when onBeat).</summary>
        public void LayPath(bool onBeat) => LayPath(onBeat, pathCurve);

        /// <summary>Lays the brush along the path; with followTangent each object is rotated to face along the curve.</summary>
        public void LayPath(bool onBeat, bool followTangent)
        {
            if (BuildDef == null)
            {
                ui.Toast("Pick a brush object first");
                return;
            }
            if (pathPoints.Count < 2)
            {
                ui.Toast("Click at least two points for the path");
                return;
            }
            float spacing = onBeat ? BeatLength / Mathf.Max(1, beatDivision) : Mathf.Max(0.25f, gridSize);
            var placed = new List<LevelObject>();
            float carry = 0f;
            var poly = PathPolyline();
            bool curve = pathCurve && pathPoints.Count >= 3;
            for (int i = 1; i < poly.Count; i++)
            {
                var a = poly[i - 1];
                var b = poly[i];
                float len = Vector2.Distance(a, b);
                if (len < 0.001f) continue;
                var dir = (b - a) / len;
                float t = carry;
                while (t <= len + 0.001f)
                {
                    var p = a + dir * t;
                    if (snapToGrid && !curve) p = GeoMath.SnapCenter(p, new Vector2(BuildDef.width * placeScale, BuildDef.height * placeScale), gridSize);
                    bool dup = false;
                    foreach (var q in placed) if ((new Vector2(q.x, q.y) - p).sqrMagnitude < 0.0001f) { dup = true; break; }
                    if (!dup)
                    {
                        float rot = followTangent ? GeoMath.NormalizeAngle(placeRotation + Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) : placeRotation;
                        var o = new LevelObject { type = BuildDef.id, x = p.x, y = p.y, rotation = rot, scaleX = placeScale * (placeFlipX ? -1f : 1f), scaleY = placeScale * (placeFlipY ? -1f : 1f), zLayer = BuildDef.defaultZLayer, zOrder = BuildDef.defaultZOrder, editorLayer = currentEditorLayer };
                        o.flipX = placeFlipX;
                        o.flipY = placeFlipY;
                        o.scaleX = placeScale;
                        o.scaleY = placeScale;
                        placed.Add(o);
                    }
                    t += spacing;
                }
                carry = t - len;
            }
            if (placed.Count == 0) return;
            foreach (var o in placed) ApplyPresetProps(o);
            RecordUndo((curve ? "Lay curve of " : "Lay path of ") + BuildDef.name);
            AddObjects(placed, false, true);
            pathPoints.Clear();
            ui.Toast("Placed " + placed.Count + " × " + BuildDef.name + (curve ? " along the curve" : " along the path"));
            Haptics.Place();
            ViewOptionsChanged?.Invoke();
        }

        // ---- auto-decorate ---------------------------------------------------------------
        /// <summary>Fills editor layer 9 with themed decorations around the level's blocks; a second run replaces the previous one.</summary>
        public void AutoDecorate(string theme, float density, int seed)
        {
            var fresh = AutoDecorator.Decorate(level, theme, density, seed);
            RecordUndo("Auto-decorate (" + theme + ")");
            var old = new List<int>();
            foreach (var o in level.objects) if (o.editorLayer == AutoDecorator.Layer) old.Add(o.uid);
            if (old.Count > 0) DeleteObjects(old, false);
            foreach (var o in fresh) o.editorLayer = AutoDecorator.Layer;
            AddObjects(fresh, false, false);
            ui.Toast("Placed " + fresh.Count + " " + theme.ToLowerInvariant() + " decorations on editor layer " + AutoDecorator.Layer + (old.Count > 0 ? " (replaced " + old.Count + ")" : ""), 4f);
        }

        public void ClearAutoDecor()
        {
            var old = new List<int>();
            foreach (var o in level.objects) if (o.editorLayer == AutoDecorator.Layer) old.Add(o.uid);
            if (old.Count == 0)
            {
                ui.Toast("Nothing on editor layer " + AutoDecorator.Layer);
                return;
            }
            RecordUndo("Clear auto-decorations");
            DeleteObjects(old, false);
            ui.Toast("Removed " + old.Count + " decorations");
        }

        // ---- replace ---------------------------------------------------------------------
        /// <summary>Swaps every selected object's type for the brush, keeping position, rotation and scale.</summary>
        public void ReplaceSelectionWithBrush()
        {
            if (BuildDef == null)
            {
                ui.Toast("Pick a brush object in Build mode first");
                return;
            }
            if (selection.Count == 0)
            {
                ui.Toast("Select the objects to replace");
                return;
            }
            var def = BuildDef;
            int n = 0;
            EditSelection(o =>
            {
                if (o.type == def.id) return;
                o.type = def.id;
                o.props.Clear();
                n++;
            }, true, "Replace with " + def.name);
            RebuildViews();
            SelectionChanged?.Invoke();
            ui.Toast("Replaced " + n + " objects with " + def.name);
        }

        public void ReplaceAllOfTypeWithBrush(string fromType)
        {
            if (BuildDef == null) return;
            var list = new List<int>();
            foreach (var o in level.objects) if (o.type == fromType) list.Add(o.uid);
            if (list.Count == 0) return;
            selection.Clear();
            selection.UnionWith(list);
            ReplaceSelectionWithBrush();
        }

        /// <summary>Selects everything painted with a colour channel (base or detail).</summary>
        public void SelectByColorChannel(int channel)
        {
            selection.Clear();
            foreach (var o in level.objects) if (IsSelectable(o) && (o.baseColor == channel || o.detailColor == channel)) selection.Add(o.uid);
            RefreshSelectionVisuals();
            SelectionChanged?.Invoke();
            ui.Toast(selection.Count + " objects use channel " + ColorChannelIds.Name(channel));
        }

        // ---- long press / context menu ------------------------------------------------
        float pressTime;
        Vector2 pressScreen;
        bool longPressFired;
        Vector2 panStartScreen;
        TapRipple ripple;
        HitboxOverlay heatmapOverlay;
        readonly List<SpriteRenderer> heatmapPool = new List<SpriteRenderer>();
        public event Action DeathsChanged;

        void RecordDeath(Vector2 position, Vector2 contact, string killer)
        {
            sessionDeaths.Add(new DeathRecord { position = position, contact = contact, killer = killer, time = Time.unscaledTime });
            DeathsChanged?.Invoke();
        }

        public void ClearSessionDeaths()
        {
            sessionDeaths.Clear();
            DeathsChanged?.Invoke();
        }

        void DrawDeathHeatmap()
        {
            int used = 0;
            if (showDeathHeatmap && !IsPlaying)
            {
                var view = GeoMath.Expand(editorCamera.ViewRect, 2f);
                heatmapOverlay.thickness = Mathf.Clamp(0.05f / editorCamera.zoom, 0.03f, 0.25f);
                heatmapOverlay.Begin();
                foreach (var d in sessionDeaths)
                {
                    if (!view.Contains(d.position)) continue;
                    // translucent heat blob: overlapping deaths stack into a brighter spot
                    SpriteRenderer sr;
                    if (used < heatmapPool.Count) sr = heatmapPool[used];
                    else
                    {
                        sr = SpriteLibrary.CreateRenderer("heat", heatmapOverlay.transform, PlaceholderSpriteFactory.Circle(), 955);
                        heatmapPool.Add(sr);
                    }
                    used++;
                    sr.enabled = true;
                    sr.transform.position = new Vector3(d.position.x, d.position.y, 0f);
                    sr.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                    sr.color = new Color(1f, 0.15f, 0.1f, 0.22f);
                    // contact point and a short line from the rider to it
                    heatmapOverlay.Segment(d.position, d.contact, new Color(1f, 0.4f, 0.3f, 0.6f));
                    heatmapOverlay.Dot(d.contact, 0.09f, new Color(1f, 0.9f, 0.9f, 0.95f));
                }
                heatmapOverlay.End();
            }
            else heatmapOverlay.Clear();
            for (int i = used; i < heatmapPool.Count; i++) heatmapPool[i].enabled = false;
        }

        // ---- transform gizmo -------------------------------------------------
        public const string GizmoPref = "geodashy.gizmo";
        TransformGizmo gizmo;
        public bool GizmoVisible
        {
            get => PlayerPrefs.GetInt(GizmoPref, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(GizmoPref, value ? 1 : 0);
                ViewOptionsChanged?.Invoke();
            }
        }
        TransformGizmo.Handle gizmoHandle;
        Vector2 gizmoCenter;
        Rect gizmoStartBounds;
        readonly Dictionary<int, TransformState> gizmoOriginals = new Dictionary<int, TransformState>();
        string gizmoInfo = "";
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

        public void Initialize(Camera camera, LevelData initial = null, string initialPath = null)
        {
            cam = camera;
            editorCamera = camera.gameObject.GetComponent<EditorCamera>();
            if (editorCamera == null) editorCamera = camera.gameObject.AddComponent<EditorCamera>();
            editorCamera.Init(camera);

            objectsRoot = new GameObject("Level Objects").transform;
            objectsRoot.SetParent(transform, false);

            level = initial ?? LevelStorage.LoadAutosave() ?? CreateStarterLevel();
            currentFilePath = initial != null ? initialPath : null;

            background = ParallaxBackground.Create(transform, cam, level.settings);
            ground = GroundRenderer.Create(transform, cam, level.settings);
            groundProps = GroundProps.Create(transform, cam, level.settings, x => GameSession.LevelObjectNear(level, x));
            grid = EditorGrid.Create(transform, editorCamera, this);
            ripple = TapRipple.Create(transform, 990);
            editorCamera.minY = level.settings.groundY - 8f;

            var ghostGo = new GameObject("Ghost");
            ghostGo.transform.SetParent(transform, false);
            ghost = ghostGo.AddComponent<SpriteRenderer>();
            SpriteLibrary.ApplyMaterial(ghost);
            ghost.sortingOrder = 940;
            ghost.enabled = false;
            hitboxOverlay = HitboxOverlay.Create(transform, "Hitbox Overlay", 960);
            heatmapOverlay = HitboxOverlay.Create(transform, "Defeat Heatmap", 956);
            gizmo = TransformGizmo.Create(transform);

            RebuildViews();
            editorCamera.Position = new Vector2(Mathf.Max(level.editorCameraX, 6f), Mathf.Max(level.editorCameraY, level.settings.groundY + 4f));
            editorCamera.SetZoom(level.editorZoom);

            LevelChanged += RecomputeSongEnd;
            ui = EditorUI.Create(this);
            SetMode(EditorMode.Build);
            SetBuildDef(ObjectCatalog.Get("castle_stone"));
            ApplySettings();
            Dirty = false;
            ui.Toast(ui.IsPhone ? "Welcome. ▶ tests the quest, ⋯ saves it, View opens the tools drawer." : "Welcome. P tests the quest, Ctrl+S saves it, F1 lists every shortcut.", 6f);
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

        /// <summary>Editor-only tint for the first of the object's groups that has one, or clear.</summary>
        public Color GroupTint(LevelObject o)
        {
            if (level.groupInfos == null || level.groupInfos.Count == 0 || o.groups == null) return Color.clear;
            foreach (var g in o.groups)
            {
                var info = level.GetGroupInfo(g, false);
                if (info != null && !string.IsNullOrEmpty(info.colorHex) && ColorUtility.TryParseHtmlString("#" + info.colorHex.TrimStart('#'), out var c)) return c;
            }
            return Color.clear;
        }

        public string GroupLabel(int g)
        {
            var info = level.GetGroupInfo(g, false);
            return info != null && !string.IsNullOrEmpty(info.name) ? "Group " + g + " · " + info.name : "Group " + g;
        }

        /// <summary>Object type counts inside a group, most common first.</summary>
        public List<KeyValuePair<string, int>> GroupContents(int g)
        {
            var counts = new Dictionary<string, int>();
            foreach (var o in level.objects)
            {
                if (!o.InGroup(g)) continue;
                var d = ObjectCatalog.Get(o.type);
                var n = d != null ? d.name : o.type;
                counts[n] = counts.TryGetValue(n, out var c) ? c + 1 : 1;
            }
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            return list;
        }

        public void SetGroupName(int g, string name)
        {
            var info = level.GetGroupInfo(g, true);
            info.name = (name ?? "").Trim();
            MarkDirty();
            LevelChanged?.Invoke();
        }

        public void SetGroupTint(int g, Color? tint)
        {
            var info = level.GetGroupInfo(g, true);
            info.colorHex = tint.HasValue ? ColorUtility.ToHtmlStringRGB(tint.Value) : "";
            MarkDirty();
            RefreshAllViews();
            LevelChanged?.Invoke();
        }

        void RefreshLayerVisibility()
        {
            foreach (var v in viewList)
            {
                bool dim = !showAllLayers && v.data.editorLayer != currentEditorLayer;
                v.SetDimmed(dim);
                var tint = GroupTint(v.data);
                if (tint.a > 0f)
                {
                    var c = v.renderer2D.color;
                    v.renderer2D.color = new Color(Mathf.Lerp(c.r, tint.r, 0.45f), Mathf.Lerp(c.g, tint.g, 0.45f), Mathf.Lerp(c.b, tint.b, 0.45f), c.a);
                }
                bool hidden = IsHidden(v.data);
                if (v.gameObject.activeSelf == hidden) v.gameObject.SetActive(!hidden);
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
            if (def != null) StampBrush = null;
            if (ActivePreset != null && (def == null || def.id != ActivePreset.type)) ActivePreset = null;
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

        /// <summary>World distance travelled per beat at the level's starting speed.</summary>
        public float BeatLength => MountCatalog.Speed(level.settings.startSpeed) * 60f / Mathf.Max(20f, level.settings.bpm);
        /// <summary>Seconds per beat from the level BPM.</summary>
        public float BeatSeconds => 60f / Mathf.Max(20f, level.settings.bpm);

        // =====================================================================
        // undo
        // =====================================================================

        string Snapshot() => LevelSerializer.ToJson(level, false);

        public void RecordUndo() => RecordUndo("Edit");

        public void RecordUndo(string label)
        {
            undo.Push(Snapshot(), label);
        }

        /// <summary>Moves through the history: negative steps undo, positive redo.</summary>
        public void JumpHistory(int steps)
        {
            if (steps < 0) for (int i = 0; i < -steps && undo.CanUndo; i++) RestoreSnapshot(undo.PopUndo(Snapshot()));
            else for (int i = 0; i < steps && undo.CanRedo; i++) RestoreSnapshot(undo.PopRedo(Snapshot()));
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
            if (record) RecordUndo("Place " + def.name);
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
            ApplyPresetProps(o);
            level.objects.Add(o);
            CreateView(o);
            MarkChanged();
            return o;
        }

        public void AddObjects(List<LevelObject> objs, bool record = true, bool select = true)
        {
            if (record) RecordUndo("Add " + objs.Count + (objs.Count == 1 ? " object" : " objects"));
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
            if (record) RecordUndo("Delete " + list.Count + (list.Count == 1 ? " object" : " objects"));
            var set = new HashSet<int>(list);
            level.objects.RemoveAll(o => set.Contains(o.uid));
            foreach (var uid in list)
            {
                RemoveView(uid);
                selection.Remove(uid);
            }
            if (hoverUid >= 0 && set.Contains(hoverUid)) hoverUid = -1;
            MarkChanged();
            if (record && ui != null) ui.Notify("Deleted " + list.Count + (list.Count == 1 ? " object" : " objects"), 4f, "trash", "Undo", Undo);
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
        public void EditSelection(Action<LevelObject> edit, bool refreshPanel = true) => EditSelection(edit, refreshPanel, "Edit selection");

        public void EditSelection(Action<LevelObject> edit, bool refreshPanel, string label)
        {
            if (selection.Count == 0) return;
            RecordUndo(label);
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
            RecordUndo("Edit property");
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
            }, true, "Nudge");
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
            }, true, "Rotate");
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
            }, true, "Flip");
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
            }, true, "Scale");
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
            }, true, "Align " + how);
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
            }, true, "Snap to grid");
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
            foreach (var o in level.objects) if (IsSelectable(o)) selection.Add(o.uid);
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
                if (!IsSelectable(v.data)) continue;
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
                if (!IsSelectable(v.data)) continue;
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
            foreach (var o in level.objects) foreach (var gid in o.groups) used.Add(gid);
            int candidate = 1;
            while (used.Contains(candidate)) candidate++;
            return candidate;
        }

        // =====================================================================
        // settings / files
        // =====================================================================

        public void ApplySettings()
        {
            background.settings = level.settings;
            ground.settings = level.settings;
            groundProps.settings = level.settings;
            background.ApplySettings();
            ground.ApplySettings();
            ground.showCeiling = false;
            cam.backgroundColor = level.settings.backgroundColor;
            editorCamera.minY = level.settings.groundY - 8f;
            RefreshAllViews();
            RefreshSongLength();
        }

        /// <summary>Flags the level as modified and notifies listeners (used after settings edits).</summary>
        public void MarkDirty() => MarkChanged();

        public void NewLevel(string name)
        {
            RecordUndo("New level");
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
                Achievements.Unlock("first_save");
                if (level.objects.Count >= 500) Achievements.Unlock("builder_500");
                foreach (var o in level.objects) if (o.type == "trig_volley") { Achievements.Unlock("volley_used"); break; }
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
            var oldId = level.id;
            level.id = Guid.NewGuid().ToString("N");
            try
            {
                LevelStorage.CopyAssets(oldId, level.id);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not copy level assets: " + e.Message);
            }
            if (!string.IsNullOrWhiteSpace(newName)) level.name = newName;
            currentFilePath = null;
            Save();
        }

        // ---- song preview (editor only) --------------------------------------

        AudioSource previewSource;

        public bool IsPreviewingSong => previewSource != null && previewSource.isPlaying;

        /// <summary>Plays the level's song from the given second so the author can find offsets and BPM.</summary>
        public void PreviewSong(float fromSeconds)
        {
            StopSongPreview();
            var s = level.settings;
            if (!string.IsNullOrEmpty(s.songFile))
            {
                var path = LevelStorage.AssetPath(level.id, s.songFile);
                AudioLoader.Load(this, path, clip =>
                {
                    if (clip == null)
                    {
                        ui.Toast("Could not load " + s.songFile);
                        return;
                    }
                    PlayPreview(clip, fromSeconds);
                });
                return;
            }
            if (!string.IsNullOrEmpty(s.songId))
            {
                var clip = Resources.Load<AudioClip>("Songs/" + s.songId);
                if (clip == null)
                {
                    ui.Toast("No song called " + s.songId + " in Resources/Songs");
                    return;
                }
                PlayPreview(clip, fromSeconds);
                return;
            }
            ui.Toast("No song set. Import one in Level Settings.");
        }

        void PlayPreview(AudioClip clip, float fromSeconds)
        {
            if (previewSource == null)
            {
                previewSource = gameObject.AddComponent<AudioSource>();
                previewSource.playOnAwake = false;
            }
            previewSource.clip = clip;
            previewSource.volume = Sfx.MusicVolume;
            previewSource.time = Mathf.Clamp(fromSeconds, 0f, Mathf.Max(0f, clip.length - 0.1f));
            previewSource.Play();
            ui.Toast(string.Format("Previewing {0} ({1:0}:{2:00})", clip.name, Mathf.Floor(clip.length / 60f), clip.length % 60f));
        }

        public void StopSongPreview()
        {
            if (previewSource != null) previewSource.Stop();
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
            RecordUndo("Import JSON");
            LoadLevel(data, null);
            Dirty = true;
            return true;
        }

        // =====================================================================
        // playtest
        // =====================================================================

        /// <summary>Centres the camera on the spawn point.</summary>
        public void GoToSpawn()
        {
            var sp = grid.SpawnPosition;
            editorCamera.Position = new Vector2(sp.x + editorCamera.HalfWidth * 0.5f, Mathf.Max(sp.y, level.settings.groundY + 4f));
        }

        public void AddBookmark(string name, Vector2 pos)
        {
            if (level.bookmarks == null) level.bookmarks = new List<Bookmark>();
            level.bookmarks.Add(new Bookmark { name = string.IsNullOrWhiteSpace(name) ? "Bookmark " + (level.bookmarks.Count + 1) : name.Trim(), x = Mathf.Max(0f, pos.x), y = pos.y });
            MarkDirty();
            LevelChanged?.Invoke();
            ui.Toast("Bookmark added at x " + pos.x.ToString("0.#"));
        }

        public void RemoveBookmark(int index)
        {
            if (level.bookmarks == null || index < 0 || index >= level.bookmarks.Count) return;
            level.bookmarks.RemoveAt(index);
            MarkDirty();
            LevelChanged?.Invoke();
        }

        /// <summary>Jumps the camera to a bookmark and makes it the playtest marker so ▶ Marker starts there.</summary>
        public void GoToBookmark(int index, bool play)
        {
            if (level.bookmarks == null || index < 0 || index >= level.bookmarks.Count) return;
            var b = level.bookmarks[index];
            editorCamera.Position = new Vector2(b.x + editorCamera.HalfWidth * 0.5f, Mathf.Max(b.y, level.settings.groundY + 4f));
            level.playtestX = b.x;
            level.playtestY = b.y;
            Dirty = true;
            LevelChanged?.Invoke();
            if (play) StartPlaytest(true);
        }

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

        public const string TestDifficultyPref = "geodashy.testDifficulty";
        public Difficulty TestDifficulty
        {
            get => (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(TestDifficultyPref, (int)Difficulty.Checkpoints), 0, 2);
            set => PlayerPrefs.SetInt(TestDifficultyPref, (int)value);
        }

        public void StartPlaytest(bool fromMarker) => StartPlaytest(fromMarker, TestDifficulty);

        public void StartPlaytest(bool fromMarker, bool training) => StartPlaytest(fromMarker, training ? Difficulty.Training : TestDifficulty);

        public void StartPlaytest(bool fromMarker, Difficulty difficulty) => StartPlaytest(fromMarker, difficulty, false);

        /// <summary>Sync-check playtest: the screen flashes on every beat and each jump is scored against the beat grid afterwards.</summary>
        public void StartSyncCheck() => StartPlaytest(false, Difficulty.Training, true);

        public void StartPlaytest(bool fromMarker, Difficulty difficulty, bool syncCheck)
        {
            if (IsPlaying) return;
            StopSongPreview();
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
            runner.DeathRecorded += RecordDeath;
            runner.syncCheck = syncCheck;
            if (syncCheck) ui.Toast("Sync check: the screen pulses on every beat. Jump to the music; the dots afterwards show how far off each jump was.", 5f);
            runner.Begin(level.DeepClone(), cam, background, ground, start, StopPlaytest, difficulty);
        }

        public void StopPlaytest()
        {
            if (runner == null) return;
            var r = runner;
            runner = null;
            // keep the run's path so it can be drawn over the level
            lastRunTrace.Clear();
            lastRunTrace.AddRange(r.runTrace);
            lastRunDeath = r.player != null && r.player.dead ? r.player.deathPoint : (Vector2?)null;
            lastRunJumps.Clear();
            lastRunJumps.AddRange(r.jumpBeats);
            lastRunWasSyncCheck = r.syncCheck;
            if (r.syncCheck && lastRunJumps.Count > 0)
            {
                int onBeat = 0;
                float sum = 0f;
                foreach (var j in lastRunJumps)
                {
                    float off = Mathf.Abs(j.z);
                    sum += off;
                    if (off < 0.12f) onBeat++;
                }
                float avgMs = sum / lastRunJumps.Count * BeatSeconds * 1000f;
                ui.Toast(onBeat + " of " + lastRunJumps.Count + " jumps on the beat · average " + avgMs.ToString("0") + " ms off. Green dots landed, amber were close, red missed.", 6f);
            }
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
            var touch = Touchscreen.current;
            // Android reports a mouse device even with no mouse attached, so the touchscreen wins whenever a finger
            // is active, whenever there is no mouse, and always on handhelds. The mouse is then passed as null so
            // its stale buttons and wheel are ignored.
            bool useTouch = touch != null && (EditorUI.TouchActive(touch) || mouse == null || Application.isMobilePlatform);
            Pointer pointer = useTouch ? (Pointer)touch : mouse;
            if (useTouch) mouse = null;
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
            if (!modal && pointer != null) HandleMouse(pointer, mouse, touch, kb);
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
                    else ui.SaveWithPrompt();
                }
                if (kb[Key.O].wasPressedThisFrame) ui.OpenFileDialog();
                if (kb[Key.G].wasPressedThisFrame)
                {
                    grid.showGrid = !grid.showGrid;
                    ViewOptionsChanged?.Invoke();
                }
                if (kb[Key.Digit0].wasPressedThisFrame || kb[Key.Numpad0].wasPressedThisFrame) editorCamera.SetZoom(1f);
                if (kb[Key.Equals].wasPressedThisFrame || kb[Key.NumpadPlus].wasPressedThisFrame) ScaleSelection(1.25f);
                if (kb[Key.Minus].wasPressedThisFrame || kb[Key.NumpadMinus].wasPressedThisFrame) ScaleSelection(0.8f);
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
            if (kb[Key.Home].wasPressedThisFrame) GoToSpawn();
            if (kb[Key.End].wasPressedThisFrame) editorCamera.Position = new Vector2(level.GetFinishX(), level.settings.groundY + 4f);
            if (kb[Key.P].wasPressedThisFrame) StartPlaytest(shift, alt);
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
            if (kb[Key.X].wasPressedThisFrame)
            {
                GizmoVisible = !GizmoVisible;
                ui.Toast("Transform gizmo " + (GizmoVisible ? "on: drag corners to scale, edges for one axis, the ring to rotate, the centre to move" : "off"), 3f);
            }
            if (kb[Key.B].wasPressedThisFrame)
            {
                showHitboxes = !showHitboxes;
                ui.Toast("Hitbox edges " + (showHitboxes ? "shown" : "hidden"));
                ViewOptionsChanged?.Invoke();
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

        float pinchDistance = -1f;

        /// <summary>
        /// Pointer handling shared by mouse and touch. With a mouse: left = select/place/drag, middle/right (or
        /// Space+left) = pan, wheel = zoom. With touch: one finger = left button, two fingers = pan + pinch zoom.
        /// </summary>
        void HandleMouse(Pointer pointer, Mouse mouse, Touchscreen touch, Keyboard kb)
        {
            var screen = pointer.position.ReadValue();
            CursorWorld = editorCamera.ScreenToWorld(screen);
            bool overUI = EditorUI.IsScreenPointOverUI(screen, false);
            bool shift = kb != null && Shift(kb);
            bool ctrl = kb != null && Ctrl(kb);
            bool space = kb != null && kb[Key.Space].isPressed;
            var left = pointer.press;

            // two-finger touch: pan with the midpoint, zoom with the pinch distance ------------
            bool twoFingers = mouse == null && touch != null && touch.touches.Count > 1 && touch.touches[0].press.isPressed && touch.touches[1].press.isPressed;
            if (twoFingers)
            {
                var a = touch.touches[0].position.ReadValue();
                var b = touch.touches[1].position.ReadValue();
                var mid = (a + b) * 0.5f;
                float dist = Mathf.Max(1f, Vector2.Distance(a, b));
                if (drag != DragState.Pan)
                {
                    if (drag != DragState.None) CancelDrag();
                    drag = DragState.Pan;
                    editorCamera.BeginDragPan(mid);
                    pinchDistance = dist;
                }
                else
                {
                    if (pinchDistance > 0f && Mathf.Abs(dist - pinchDistance) > 0.5f)
                    {
                        editorCamera.ZoomAt(editorCamera.zoom * (dist / pinchDistance), mid);
                        editorCamera.BeginDragPan(mid);   // re-anchor so the zoom doesn't fight the pan
                        ViewOptionsChanged?.Invoke();
                    }
                    pinchDistance = dist;
                    editorCamera.DragPan(mid);
                }
                ghost.enabled = false;
                return;
            }
            if (pinchDistance > 0f)
            {
                // second finger lifted: end the pan and swallow the remaining single touch until it is released
                pinchDistance = -1f;
                editorCamera.EndDragPan();
                drag = DragState.None;
                ghost.enabled = false;
                return;
            }

            // snapped cursor for placement
            var size = BuildDef != null ? new Vector2(BuildDef.width * placeScale, BuildDef.height * placeScale) : Vector2.one;
            if (Mathf.Abs(Mathf.DeltaAngle(placeRotation, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(placeRotation, 270f)) < 1f) size = new Vector2(size.y, size.x);
            CursorSnapped = snapToGrid ? GeoMath.SnapCenter(CursorWorld, size, gridSize) : CursorWorld;
            if (beatSnap)
            {
                float beat = BeatLength / Mathf.Max(1, beatDivision);
                CursorSnapped = new Vector2(Mathf.Round(CursorWorld.x / beat) * beat, CursorSnapped.y);
            }

            // zoom -------------------------------------------------------------
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (!overUI && Mathf.Abs(scroll) > 0.01f)
            {
                float factor = scroll > 0 ? 1.15f : 1f / 1.15f;
                editorCamera.ZoomAt(editorCamera.zoom * factor, screen);
                ViewOptionsChanged?.Invoke();
            }

            // pan with middle / right mouse or space+left -------------------------
            bool sideButton = mouse != null && (mouse.middleButton.isPressed || mouse.rightButton.isPressed);
            bool sidePressed = mouse != null && (mouse.middleButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
            bool panButton = sideButton || (space && left.isPressed);
            if (drag == DragState.None && panButton && !overUI && (sidePressed || left.wasPressedThisFrame))
            {
                drag = DragState.Pan;
                panStartScreen = screen;
                editorCamera.BeginDragPan(screen);
            }
            if (drag == DragState.Pan)
            {
                editorCamera.DragPan(screen);
                if (!panButton)
                {
                    editorCamera.EndDragPan();
                    drag = DragState.None;
                    // a right-click that did not pan opens the context menu on the object under the cursor
                    if (mouse != null && mouse.rightButton.wasReleasedThisFrame && (screen - panStartScreen).sqrMagnitude < 36f) OpenContextMenu(screen);
                }
                ghost.enabled = false;
                return;
            }

            // long press (touch): context menu instead of a drag
            if (mouse == null && left.isPressed && !longPressFired && drag != DragState.None && drag != DragState.SwipeBuild && drag != DragState.SwipeDelete)
            {
                if ((screen - pressScreen).sqrMagnitude < 144f && Time.unscaledTime - pressTime > 0.55f)
                {
                    longPressFired = true;
                    CancelDrag();
                    OpenContextMenu(screen);
                    return;
                }
            }
            if (longPressFired)
            {
                if (left.wasReleasedThisFrame || !left.isPressed) longPressFired = false;
                ghost.enabled = false;
                return;
            }

            // hover --------------------------------------------------------------
            if (drag == DragState.None)
            {
                bool overHandle = GizmoActive && gizmo.HitTest(CursorWorld) != TransformGizmo.Handle.None;
                var hv = overUI || overHandle ? null : PickObject(CursorWorld);
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
            if (left.wasPressedThisFrame && !overUI && drag == DragState.None)
            {
                dragStartScreen = screen;
                dragStartWorld = CursorWorld;
                dragMoved = false;
                pressTime = Time.unscaledTime;
                pressScreen = screen;
                if (mouse == null && ripple != null) ripple.Spawn(CursorWorld, Mathf.Clamp(0.9f / editorCamera.zoom, 0.4f, 3f));
                var handle = GizmoActive ? gizmo.HitTest(CursorWorld) : TransformGizmo.Handle.None;
                if (handle != TransformGizmo.Handle.None) BeginGizmoDrag(handle);
                else BeginLeftPress(shift, ctrl);
            }
            else if (left.isPressed && drag != DragState.None)
            {
                if ((screen - dragStartScreen).sqrMagnitude > 9f) dragMoved = true;
                ContinueLeftDrag(shift);
            }
            else if (left.wasReleasedThisFrame && drag != DragState.None)
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
                    if (pathTool && !ctrl)
                    {
                        pathPoints.Add(CursorSnapped);
                        ViewOptionsChanged?.Invoke();
                        return;
                    }
                    if (StampBrush != null && !ctrl)
                    {
                        RecordUndo("Stamp " + StampBrush.name);
                        var placed = StampBrush.Place(CursorSnapped);
                        foreach (var o in placed) o.editorLayer = currentEditorLayer;
                        AddObjects(placed, false, false);
                        Haptics.Place();
                        return;
                    }
                    if (BuildDef != null && !ctrl)
                    {
                        RecordUndo("Place " + BuildDef.name);
                        Haptics.Place();
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
                    RecordUndo("Delete");
                    drag = DragState.SwipeDelete;
                    dragMoved = false;
                    if (hit != null && DeleteAllowed(hit)) DeleteObjects(new[] { hit.data.uid }, false);
                    break;
            }
        }

        bool GizmoActive => GizmoVisible && selection.Count > 0 && Mode != EditorMode.Delete && !(Mode == EditorMode.Build && BuildDef != null);

        void BeginGizmoDrag(TransformGizmo.Handle handle)
        {
            gizmoHandle = handle;
            gizmoStartBounds = SelectionBounds();
            gizmoCenter = gizmoStartBounds.center;
            gizmoOriginals.Clear();
            foreach (var o in SelectedObjects())
                gizmoOriginals[o.uid] = new TransformState { x = o.x, y = o.y, rotation = o.rotation, scaleX = o.scaleX, scaleY = o.scaleY };
            RecordUndo("Transform");
            drag = DragState.Gizmo;
        }

        void ContinueGizmoDrag(bool shift)
        {
            bool multi = gizmoOriginals.Count > 1;
            switch (gizmoHandle)
            {
                case TransformGizmo.Handle.Move:
                {
                    var delta = CursorWorld - dragStartWorld;
                    if (snapToGrid && !shift) delta = new Vector2(GeoMath.SnapValue(delta.x, gridSize), GeoMath.SnapValue(delta.y, gridSize));
                    foreach (var kv in gizmoOriginals)
                    {
                        var o = level.FindByUid(kv.Key);
                        if (o == null) continue;
                        o.x = kv.Value.x + delta.x;
                        o.y = kv.Value.y + delta.y;
                        GetView(kv.Key)?.ApplyTransform();
                    }
                    gizmoInfo = string.Format("MOVE {0:+0.##;-0.##;0} {1:+0.##;-0.##;0}", delta.x, delta.y);
                    break;
                }
                case TransformGizmo.Handle.Rotate:
                {
                    float a0 = Mathf.Atan2(dragStartWorld.y - gizmoCenter.y, dragStartWorld.x - gizmoCenter.x) * Mathf.Rad2Deg;
                    float a1 = Mathf.Atan2(CursorWorld.y - gizmoCenter.y, CursorWorld.x - gizmoCenter.x) * Mathf.Rad2Deg;
                    float delta = Mathf.DeltaAngle(a0, a1);
                    float step = shift ? 1f : (snapToGrid ? 15f : 5f);
                    delta = Mathf.Round(delta / step) * step;
                    foreach (var kv in gizmoOriginals)
                    {
                        var o = level.FindByUid(kv.Key);
                        if (o == null) continue;
                        if (multi)
                        {
                            var p = GeoMath.Rotate(new Vector2(kv.Value.x, kv.Value.y) - gizmoCenter, delta) + gizmoCenter;
                            o.x = p.x;
                            o.y = p.y;
                        }
                        o.rotation = GeoMath.NormalizeAngle(kv.Value.rotation + delta);
                        GetView(kv.Key)?.ApplyTransform();
                    }
                    gizmoInfo = string.Format("ROTATE {0:+0;-0;0} DEG", delta);
                    break;
                }
                default:
                {
                    // scale: factor from the distance to the centre along the handle's axes
                    var h = gizmoHandle;
                    bool xAxis = h != TransformGizmo.Handle.ScaleT && h != TransformGizmo.Handle.ScaleB;
                    bool yAxis = h != TransformGizmo.Handle.ScaleL && h != TransformGizmo.Handle.ScaleR;
                    bool uniform = xAxis && yAxis;
                    var d0 = dragStartWorld - gizmoCenter;
                    var d1 = CursorWorld - gizmoCenter;
                    float fx = Mathf.Abs(d0.x) > 0.01f ? Mathf.Abs(d1.x) / Mathf.Abs(d0.x) : 1f;
                    float fy = Mathf.Abs(d0.y) > 0.01f ? Mathf.Abs(d1.y) / Mathf.Abs(d0.y) : 1f;
                    if (uniform)
                    {
                        float f = d0.magnitude > 0.01f ? d1.magnitude / d0.magnitude : 1f;
                        fx = fy = f;
                    }
                    if (!xAxis) fx = 1f;
                    if (!yAxis) fy = 1f;
                    float step = shift ? 0.01f : 0.25f;
                    foreach (var kv in gizmoOriginals)
                    {
                        var o = level.FindByUid(kv.Key);
                        if (o == null) continue;
                        float sx = Mathf.Clamp(Mathf.Round(kv.Value.scaleX * fx / step) * step, 0.125f, 16f);
                        float sy = Mathf.Clamp(Mathf.Round(kv.Value.scaleY * fy / step) * step, 0.125f, 16f);
                        float ax = kv.Value.scaleX > 0.0001f ? sx / kv.Value.scaleX : 1f;
                        float ay = kv.Value.scaleY > 0.0001f ? sy / kv.Value.scaleY : 1f;
                        if (multi)
                        {
                            o.x = gizmoCenter.x + (kv.Value.x - gizmoCenter.x) * ax;
                            o.y = gizmoCenter.y + (kv.Value.y - gizmoCenter.y) * ay;
                        }
                        o.scaleX = sx;
                        o.scaleY = sy;
                        GetView(kv.Key)?.ApplyTransform();
                    }
                    gizmoInfo = string.Format("SCALE {0:0.##} X {1:0.##}", fx, fy);
                    break;
                }
            }
        }

        void EndGizmoDrag()
        {
            bool changed = false;
            foreach (var kv in gizmoOriginals)
            {
                var o = level.FindByUid(kv.Key);
                if (o == null) continue;
                if (Mathf.Abs(o.x - kv.Value.x) > 0.0001f || Mathf.Abs(o.y - kv.Value.y) > 0.0001f || Mathf.Abs(o.rotation - kv.Value.rotation) > 0.0001f ||
                    Mathf.Abs(o.scaleX - kv.Value.scaleX) > 0.0001f || Mathf.Abs(o.scaleY - kv.Value.scaleY) > 0.0001f) changed = true;
            }
            gizmoInfo = "";
            if (changed)
            {
                foreach (var kv in gizmoOriginals) RefreshView(kv.Key);
                RefreshSelectionVisuals();
                MarkChanged();
                SelectionChanged?.Invoke();
            }
            else undo.DiscardLast();
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
            RecordUndo("Move");
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
                case DragState.Gizmo:
                    if (dragMoved) ContinueGizmoDrag(shift);
                    break;
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
                    ApplyMoveDelta(delta);
                    if (snapGuides)
                    {
                        var adjust = GuideAdjustment();
                        if (adjust != Vector2.zero) ApplyMoveDelta(delta + adjust);
                    }
                    break;
                }
            }
        }

        void ApplyMoveDelta(Vector2 delta)
        {
            foreach (var kv in dragOriginalPositions)
            {
                var o = level.FindByUid(kv.Key);
                if (o == null) continue;
                o.x = kv.Value.x + delta.x;
                o.y = kv.Value.y + delta.y;
                var v = GetView(kv.Key);
                if (v != null) v.ApplyTransform();
            }
        }

        /// <summary>
        /// Snapping guides: the smallest shift that aligns an edge or centre of the dragged selection with an edge
        /// or centre of a nearby unselected object. Also records the matched lines so LateUpdate can draw them.
        /// </summary>
        Vector2 GuideAdjustment()
        {
            guideXs.Clear();
            guideYs.Clear();
            var sel = SelectionBounds();
            if (sel.width <= 0f && sel.height <= 0f) return Vector2.zero;
            float tol = Mathf.Clamp(0.3f / editorCamera.zoom, 0.08f, 0.6f);
            var view = GeoMath.Expand(editorCamera.ViewRect, 4f);
            float bestDx = float.MaxValue, bestDy = float.MaxValue;
            float lineX = 0f, lineY = 0f;
            float[] sx = { sel.xMin, sel.center.x, sel.xMax };
            float[] sy = { sel.yMin, sel.center.y, sel.yMax };
            foreach (var v in viewList)
            {
                if (selection.Contains(v.data.uid) || !IsLayerVisible(v.data) || IsHidden(v.data)) continue;
                var b = v.Bounds;
                if (!GeoMath.RectsOverlap(view, b)) continue;
                float[] ox = { b.xMin, b.center.x, b.xMax };
                float[] oy = { b.yMin, b.center.y, b.yMax };
                for (int i = 0; i < 3; i++)
                for (int k = 0; k < 3; k++)
                {
                    float dx = ox[k] - sx[i];
                    if (Mathf.Abs(dx) < tol && Mathf.Abs(dx) < Mathf.Abs(bestDx)) { bestDx = dx; lineX = ox[k]; }
                    float dy = oy[k] - sy[i];
                    if (Mathf.Abs(dy) < tol && Mathf.Abs(dy) < Mathf.Abs(bestDy)) { bestDy = dy; lineY = oy[k]; }
                }
            }
            var adjust = Vector2.zero;
            if (bestDx != float.MaxValue) { adjust.x = bestDx; guideXs.Add(lineX); }
            if (bestDy != float.MaxValue) { adjust.y = bestDy; guideYs.Add(lineY); }
            return adjust;
        }

        /// <summary>Context menu for the object under a screen point (long press on touch, right-click with a mouse).</summary>
        void OpenContextMenu(Vector2 screen)
        {
            if (Mode == EditorMode.Delete) return;
            var world = editorCamera.ScreenToWorld(screen);
            var hit = PickObject(world);
            if (hit == null)
            {
                if (selection.Count == 0) return;
            }
            else if (!selection.Contains(hit.data.uid)) Select(hit.data.uid, false);
            if (Mode == EditorMode.Build) SetMode(EditorMode.Edit);
            var options = new[] { "Copy", "Duplicate", "Delete", "Properties", "Select same type", "Save as stamp…", "Rotate 90°", "Flip H" };
            ui.ShowMenuAt(screen, options, i =>
            {
                switch (i)
                {
                    case 0: CopySelection(); break;
                    case 1: DuplicateSelection(); break;
                    case 2: DeleteSelection(); break;
                    case 3: ui.OpenPropertiesForSelection(); break;
                    case 4:
                    {
                        var objs = SelectedObjects();
                        if (objs.Count > 0) SelectByType(objs[0].type);
                        break;
                    }
                    case 5: ui.PromptSaveStamp(); break;
                    case 6: RotateSelection(90); break;
                    case 7: FlipSelection(true); break;
                }
            });
        }

        void EndLeftDrag(bool shift)
        {
            switch (drag)
            {
                case DragState.Gizmo:
                    EndGizmoDrag();
                    break;
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
                    guideXs.Clear();
                    guideYs.Clear();
                    break;
                }
                case DragState.SwipeDelete:
                    break;
            }
            drag = DragState.None;
        }

        void CancelDrag()
        {
            if (drag == DragState.Gizmo)
            {
                foreach (var kv in gizmoOriginals)
                {
                    var o = level.FindByUid(kv.Key);
                    if (o == null) continue;
                    o.x = kv.Value.x;
                    o.y = kv.Value.y;
                    o.rotation = kv.Value.rotation;
                    o.scaleX = kv.Value.scaleX;
                    o.scaleY = kv.Value.scaleY;
                    RefreshView(kv.Key);
                }
                undo.DiscardLast();
                gizmoInfo = "";
            }
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

        void LateUpdate()
        {
            if (hitboxOverlay == null) return;
            if (IsPlaying)
            {
                hitboxOverlay.Clear();
                return;
            }
            hitboxOverlay.thickness = Mathf.Clamp(0.05f / editorCamera.zoom, 0.03f, 0.25f) * Accessibility.OverlayThicknessMultiplier;
            hitboxOverlay.Begin();
            if (showHitboxes)
            {
                var view = GeoMath.Expand(editorCamera.ViewRect, 2f);
                foreach (var v in viewList)
                {
                    if (!IsLayerVisible(v.data)) continue;
                    if (!GeoMath.RectsOverlap(view, v.Bounds)) continue;
                    hitboxOverlay.DrawView(v, 0.85f);
                }
            }
            if (ghost != null && ghost.enabled && BuildDef != null)
            {
                var size = new Vector2(BuildDef.width * placeScale, BuildDef.height * placeScale);
                hitboxOverlay.DrawObject(BuildDef, CursorSnapped, placeRotation, size, placeFlipX, placeFlipY, 1f);
            }
            // stamp brush footprint at the cursor
            if (StampBrush != null && Mode == EditorMode.Build && drag == DragState.None && !ui.PointerOverUI)
            {
                var r = new Rect(CursorSnapped.x - StampBrush.width / 2f, CursorSnapped.y - StampBrush.height / 2f, StampBrush.width, StampBrush.height);
                hitboxOverlay.Rect(r, new Color(1f, 0.85f, 0.3f, 0.8f), hitboxOverlay.thickness);
                foreach (var o in StampBrush.objects)
                {
                    var d = ObjectCatalog.Get(o.type);
                    if (d == null) continue;
                    var sz = new Vector2(d.width * Mathf.Abs(o.scaleX), d.height * Mathf.Abs(o.scaleY));
                    hitboxOverlay.Rect(new Rect(CursorSnapped.x + o.x - sz.x / 2f, CursorSnapped.y + o.y - sz.y / 2f, sz.x, sz.y), new Color(1f, 0.85f, 0.3f, 0.35f), hitboxOverlay.thickness * 0.6f);
                }
            }
            // last playtest path
            if (showRunTrace && lastRunTrace.Count > 1)
            {
                var view = GeoMath.Expand(editorCamera.ViewRect, 2f);
                var tc = new Color(0.4f, 0.85f, 1f, 0.55f);
                int step = Mathf.Max(1, lastRunTrace.Count / 4000);
                for (int i = step; i < lastRunTrace.Count; i += step)
                {
                    var a = lastRunTrace[i - step];
                    var b = lastRunTrace[i];
                    if (b.x < view.xMin || a.x > view.xMax) continue;
                    hitboxOverlay.Segment(a, b, tc, hitboxOverlay.thickness * 0.8f);
                }
                if (lastRunDeath.HasValue) hitboxOverlay.Dot(lastRunDeath.Value, 0.16f, new Color(1f, 0.25f, 0.25f, 0.9f));
            }
            // sync-check jump dots: green on the beat, amber close, red off
            if (showRunTrace && lastRunWasSyncCheck && lastRunJumps.Count > 0)
            {
                var view = GeoMath.Expand(editorCamera.ViewRect, 1f);
                foreach (var j in lastRunJumps)
                {
                    if (j.x < view.xMin || j.x > view.xMax) continue;
                    float off = Mathf.Abs(j.z);
                    var c = off < 0.12f ? new Color(0.35f, 1f, 0.45f, 0.95f) : (off < 0.25f ? new Color(1f, 0.8f, 0.3f, 0.95f) : new Color(1f, 0.3f, 0.3f, 0.95f));
                    hitboxOverlay.Dot(new Vector2(j.x, j.y), 0.14f, c);
                    if (off >= 0.12f) hitboxOverlay.Segment(new Vector2(j.x, j.y), new Vector2(j.x - j.z * BeatLength, j.y), c, hitboxOverlay.thickness * 0.6f);
                }
            }
            // path tool polyline
            if (pathTool && pathPoints.Count > 0)
            {
                var pc = new Color(1f, 0.6f, 0.2f, 0.9f);
                var poly = PathPolyline();
                for (int i = 1; i < poly.Count; i++) hitboxOverlay.Segment(poly[i - 1], poly[i], pc, hitboxOverlay.thickness);
                foreach (var p in pathPoints) hitboxOverlay.Dot(p, 0.12f, pc);
                if (drag == DragState.None && !ui.PointerOverUI) hitboxOverlay.Segment(pathPoints[pathPoints.Count - 1], CursorSnapped, new Color(1f, 0.6f, 0.2f, 0.4f), hitboxOverlay.thickness * 0.6f);
            }
            // snapping guides while dragging
            if (drag == DragState.MoveSelection && snapGuides && (guideXs.Count > 0 || guideYs.Count > 0))
            {
                var view = editorCamera.ViewRect;
                var gc = new Color(1f, 0.4f, 0.9f, 0.9f);
                foreach (var x in guideXs) hitboxOverlay.Segment(new Vector2(x, view.yMin), new Vector2(x, view.yMax), gc, hitboxOverlay.thickness * 0.6f);
                foreach (var y in guideYs) hitboxOverlay.Segment(new Vector2(view.xMin, y), new Vector2(view.xMax, y), gc, hitboxOverlay.thickness * 0.6f);
            }
            hitboxOverlay.End();
            DrawDeathHeatmap();

            if (gizmo != null)
            {
                if (GizmoActive && !ui.ModalOpen)
                {
                    float ppu = Screen.height / (2f * editorCamera.HalfHeight);
                    string info = gizmoInfo;
                    if (string.IsNullOrEmpty(info))
                    {
                        var objs = SelectedObjects();
                        if (objs.Count == 1)
                        {
                            var o = objs[0];
                            info = string.Format("X {0:0.##}  Y {1:0.##}  ROT {2:0}  SCALE {3:0.##}X{4:0.##}", o.x, o.y, o.rotation, o.scaleX, o.scaleY);
                        }
                        else info = objs.Count + " OBJECTS";
                    }
                    gizmo.Refresh(SelectionBounds(), ppu, CursorWorld, drag == DragState.Gizmo, gizmoHandle, info);
                }
                else gizmo.Hide();
            }
        }

        void OnDestroy()
        {
            StopSongPreview();
            if (Instance == this) Instance = null;
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
