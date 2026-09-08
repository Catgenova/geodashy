using Geodashy.Core;
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
        float lastGridSize = -1f;
        float lastBeatWidth = -1f;

        public static EditorGrid Create(Transform parent, EditorCamera cam, LevelEditor editor)
        {
            var go = new GameObject("Editor Grid");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<EditorGrid>();
            g.editorCamera = cam;
            g.editor = editor;
            g.Build();
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
                float speed = MountCatalog.Speed(level.settings.startSpeed);
                float beat = speed * 60f / Mathf.Max(20f, level.settings.bpm);
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

            // playtest marker ---------------------------------------------
            if (level != null && level.playtestX >= 0f)
            {
                playtestMarker.enabled = true;
                playtestMarker.transform.position = new Vector3(level.playtestX, level.playtestY, 0f);
            }
            else playtestMarker.enabled = false;

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
