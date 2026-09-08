using Geodashy.Core;
using Geodashy.Gameplay;
using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Editing
{
    /// <summary>Grid lines, camera guide, BPM guides and the playtest marker drawn in the editor.</summary>
    public class EditorGrid : MonoBehaviour
    {
        public const int GridSorting = -3000;
        public const int GuideSorting = 950;

        public EditorCamera editorCamera;
        public LevelEditor editor;

        public bool showGrid = true;
        public bool showCameraGuide = true;
        public bool showBpmGuide;
        public float gridSize = 1f;

        SpriteRenderer grid;
        SpriteRenderer cameraGuide;
        SpriteRenderer bpmGuide;
        SpriteRenderer playtestMarker;
        SpriteRenderer selectionBox;
        SpriteRenderer ceilingLine;
        SpriteRenderer finishLine;
        SpriteRenderer spawnGhost, spawnLabel, spawnLine, spawnArrow;
        SpriteRenderer markerGhost, markerLabel, markerLine;
        float lastGridSize = -1f;
        float lastBeatWidth = -1f;
        bool spawnDirty = true;
        GameRunner.StartState spawnState;
        GameRunner.StartState markerState;
        bool hasMarker;

        public static readonly Color SpawnColor = new Color(0.35f, 1f, 0.5f, 1f);
        public static readonly Color MarkerColor = new Color(0.4f, 0.85f, 1f, 1f);

        public static EditorGrid Create(Transform parent, EditorCamera cam, LevelEditor editor)
        {
            var go = new GameObject("Editor Grid");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<EditorGrid>();
            g.editorCamera = cam;
            g.editor = editor;
            g.Build();
            editor.LevelChanged += () => g.spawnDirty = true;
            return g;
        }

        void Build()
        {
            grid = SpriteLibrary.CreateRenderer("Grid", transform, null, GridSorting);
            grid.drawMode = SpriteDrawMode.Tiled;
            grid.tileMode = SpriteTileMode.Continuous;

            cameraGuide = SpriteLibrary.CreateRenderer("CameraGuide", transform, PlaceholderSpriteFactory.Outline(), GuideSorting);
            cameraGuide.drawMode = SpriteDrawMode.Sliced;
            cameraGuide.color = new Color(1f, 1f, 1f, 0.35f);

            bpmGuide = SpriteLibrary.CreateRenderer("BpmGuide", transform, null, GridSorting + 1);
            bpmGuide.drawMode = SpriteDrawMode.Tiled;
            bpmGuide.tileMode = SpriteTileMode.Continuous;
            bpmGuide.color = new Color(1f, 0.8f, 0.3f, 0.5f);

            playtestMarker = SpriteLibrary.CreateRenderer("PlaytestMarker", transform, PlaceholderSpriteFactory.Outline(), GuideSorting);
            playtestMarker.drawMode = SpriteDrawMode.Sliced;
            playtestMarker.size = new Vector2(1.2f, 1.2f);
            playtestMarker.color = new Color(0.3f, 1f, 0.4f, 0.9f);

            selectionBox = SpriteLibrary.CreateRenderer("SelectionBox", transform, PlaceholderSpriteFactory.Outline(), GuideSorting + 1);
            selectionBox.drawMode = SpriteDrawMode.Sliced;
            selectionBox.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            selectionBox.enabled = false;

            ceilingLine = SpriteLibrary.CreateRenderer("CeilingGuide", transform, PlaceholderSpriteFactory.WhiteSquare(), GridSorting + 2);
            ceilingLine.color = new Color(0.6f, 0.8f, 1f, 0.35f);

            finishLine = SpriteLibrary.CreateRenderer("FinishGuide", transform, PlaceholderSpriteFactory.WhiteSquare(), GridSorting + 2);
            finishLine.color = new Color(1f, 0.85f, 0.3f, 0.5f);

            // spawn preview: the real mount, ghosted, plus a label, a start line and an arrow
            spawnLine = SpriteLibrary.CreateRenderer("SpawnLine", transform, PlaceholderSpriteFactory.WhiteSquare(), GridSorting + 3);
            spawnLine.color = new Color(SpawnColor.r, SpawnColor.g, SpawnColor.b, 0.45f);
            spawnGhost = SpriteLibrary.CreateRenderer("SpawnGhost", transform, null, GuideSorting + 2);
            spawnLabel = SpriteLibrary.CreateRenderer("SpawnLabel", transform, PlaceholderSpriteFactory.ForText("SPAWN", Color.white), GuideSorting + 3);
            spawnLabel.color = SpawnColor;
            spawnArrow = SpriteLibrary.CreateRenderer("SpawnArrow", transform, PlaceholderSpriteFactory.ForText("V", Color.white), GuideSorting + 3);
            spawnArrow.color = SpawnColor;

            markerLine = SpriteLibrary.CreateRenderer("MarkerLine", transform, PlaceholderSpriteFactory.WhiteSquare(), GridSorting + 3);
            markerLine.color = new Color(MarkerColor.r, MarkerColor.g, MarkerColor.b, 0.45f);
            markerGhost = SpriteLibrary.CreateRenderer("MarkerGhost", transform, null, GuideSorting + 2);
            markerLabel = SpriteLibrary.CreateRenderer("MarkerLabel", transform, PlaceholderSpriteFactory.ForText("TEST FROM HERE", Color.white), GuideSorting + 3);
            markerLabel.color = MarkerColor;
        }

        void RefreshSpawn()
        {
            spawnDirty = false;
            var level = editor.level;
            spawnState = GameRunner.ResolveStart(level, null);
            hasMarker = level.playtestX >= 0f;
            if (hasMarker) markerState = GameRunner.ResolveStart(level, new Vector2(level.playtestX, level.playtestY));
            ApplyGhost(spawnGhost, spawnState, SpawnColor);
            markerGhost.enabled = markerLabel.enabled = markerLine.enabled = hasMarker;
            if (hasMarker) ApplyGhost(markerGhost, markerState, MarkerColor);
        }

        static void ApplyGhost(SpriteRenderer sr, GameRunner.StartState st, Color tint)
        {
            var mount = MountCatalog.Get(st.mount);
            sr.sprite = SpriteLibrary.ForMount(mount);
            float s = st.mini ? 0.6f : 1f;
            float baseScale = SpriteLibrary.MountIsAnimated(mount) ? 1f : (sr.sprite != null ? mount.width / Mathf.Max(0.01f, sr.sprite.bounds.size.x) : 1f);
            sr.transform.localScale = new Vector3(baseScale * s * SpriteLibrary.MountFacing(mount), baseScale * s * (st.flipped ? -1f : 1f), 1f);
            sr.transform.position = new Vector3(st.position.x, st.position.y, 0f);
            sr.color = new Color(tint.r, tint.g, tint.b, 0.75f);
            sr.enabled = true;
        }

        /// <summary>Where the rider will appear when testing from the start.</summary>
        public Vector2 SpawnPosition
        {
            get
            {
                if (spawnDirty) RefreshSpawn();
                return spawnState.position;
            }
        }

        public void SetSelectionBox(Rect? r)
        {
            if (r == null)
            {
                selectionBox.enabled = false;
                return;
            }
            selectionBox.enabled = true;
            selectionBox.transform.position = new Vector3(r.Value.center.x, r.Value.center.y, 0f);
            selectionBox.size = new Vector2(Mathf.Max(0.05f, r.Value.width), Mathf.Max(0.05f, r.Value.height));
        }

        void LateUpdate()
        {
            if (editorCamera == null || editorCamera.cam == null) return;
            var view = editorCamera.ViewRect;
            var level = editor != null ? editor.level : null;
            float groundY = level != null ? level.settings.groundY : 0f;

            // grid ---------------------------------------------------------
            grid.enabled = showGrid;
            if (showGrid)
            {
                if (!Mathf.Approximately(lastGridSize, gridSize))
                {
                    int major = gridSize >= 1f ? 4 : Mathf.Max(1, Mathf.RoundToInt(1f / gridSize));
                    const int px = 32;
                    grid.sprite = PlaceholderSpriteFactory.GridCell(px, px / gridSize, new Color(1, 1, 1, 0.10f), new Color(1, 1, 1, 0.22f), major);
                    lastGridSize = gridSize;
                }
                float tile = grid.sprite.bounds.size.x; // world size of one tile (major cell)
                float w = Mathf.Ceil(view.width / tile) * tile + tile * 2f;
                float h = Mathf.Ceil(view.height / tile) * tile + tile * 2f;
                float left = Mathf.Floor(view.xMin / tile) * tile - tile;
                float bottom = Mathf.Floor(view.yMin / tile) * tile - tile;
                grid.size = new Vector2(w, h);
                grid.transform.position = new Vector3(left, bottom, 0f); // sprite pivot is bottom-left
                var c = grid.color;
                c.a = Mathf.Clamp01(editorCamera.zoom * 1.2f);
                grid.color = c;
            }

            // camera guide (what the player will see at this camera x) --------
            cameraGuide.enabled = showCameraGuide;
            if (showCameraGuide)
            {
                float halfH = EditorCamera.BaseHalfHeight;
                float halfW = halfH * editorCamera.cam.aspect;
                var center = editorCamera.Position;
                float y = Mathf.Max(groundY + halfH - 1.5f, center.y);
                if (editorCamera.zoom >= 0.98f) y = center.y;
                cameraGuide.transform.position = new Vector3(center.x, y, 0f);
                cameraGuide.size = new Vector2(halfW * 2f, halfH * 2f);
                cameraGuide.enabled = editorCamera.zoom < 0.98f;
            }

            // bpm guide ----------------------------------------------------
            bpmGuide.enabled = showBpmGuide && level != null;
            if (bpmGuide.enabled)
            {
                float beat = editor.BeatLength / Mathf.Max(1, editor.beatSnap ? editor.beatDivision : 1);
                if (!Mathf.Approximately(beat, lastBeatWidth))
                {
                    var r = new Raster(64, 4);
                    r.Clear(new Color(0, 0, 0, 0));
                    r.FillRect(0, 0, 2, 4, Color.white);
                    bpmGuide.sprite = r.ToSprite(64f / beat, new Vector2(0f, 0.5f), null, FilterMode.Point);
                    lastBeatWidth = beat;
                }
                float w = Mathf.Ceil(view.width / beat) * beat + beat * 2f;
                float left = Mathf.Floor(view.xMin / beat) * beat - beat;
                bpmGuide.size = new Vector2(w, view.height + 4f);
                bpmGuide.transform.position = new Vector3(left, view.center.y, 0f);
            }

            // spawn + playtest marker ------------------------------------
            if (level != null)
            {
                if (spawnDirty) RefreshSpawn();
                float bob = Mathf.Sin(Time.unscaledTime * 3f) * 0.12f;
                float ceilY = groundY + level.settings.ceilingHeight;
                var sp = spawnState.position;
                spawnLine.transform.position = new Vector3(sp.x, (groundY + ceilY) / 2f, 0f);
                spawnLine.transform.localScale = new Vector3(0.06f, ceilY - groundY + 2f, 1f);
                spawnLabel.transform.position = new Vector3(sp.x, sp.y + 2.1f + bob, 0f);
                spawnLabel.transform.localScale = Vector3.one * 0.6f;
                spawnArrow.transform.position = new Vector3(sp.x, sp.y + 1.35f + bob, 0f);
                spawnArrow.transform.localScale = Vector3.one * 0.6f;
                spawnLabel.enabled = spawnArrow.enabled = spawnLine.enabled = spawnGhost.enabled = true;

                playtestMarker.enabled = hasMarker;
                if (hasMarker)
                {
                    var mp = markerState.position;
                    playtestMarker.transform.position = new Vector3(mp.x, mp.y, 0f);
                    markerLine.transform.position = new Vector3(mp.x, (groundY + ceilY) / 2f, 0f);
                    markerLine.transform.localScale = new Vector3(0.06f, ceilY - groundY + 2f, 1f);
                    markerLabel.transform.position = new Vector3(mp.x, mp.y + 1.6f - bob, 0f);
                    markerLabel.transform.localScale = Vector3.one * 0.5f;
                }
            }

            // ceiling + finish guides -------------------------------------
            if (level != null)
            {
                float ceilY = groundY + level.settings.ceilingHeight;
                ceilingLine.transform.position = new Vector3(view.center.x, ceilY, 0f);
                ceilingLine.transform.localScale = new Vector3(view.width + 2f, 0.04f, 1f);
                float finishX = level.GetFinishX();
                finishLine.transform.position = new Vector3(finishX, view.center.y, 0f);
                finishLine.transform.localScale = new Vector3(0.06f, view.height + 2f, 1f);
                finishLine.enabled = level.objects.Count > 0;
            }
        }
    }
}
