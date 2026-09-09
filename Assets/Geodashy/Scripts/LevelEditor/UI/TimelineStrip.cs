using System.Collections.Generic;
using Geodashy.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>
    /// A thin strip under the top bar showing the whole level: bar lines from the BPM, trigger and portal
    /// markers, and the camera position. Click to jump the camera; drag a trigger marker to retime it.
    /// </summary>
    public class TimelineStrip : MonoBehaviour, IPointerClickHandler
    {
        public const float Height = 22f;
        public const float PhoneHeight = 18f;

        EditorUI ui;
        LevelEditor editor;
        RectTransform rt;
        RectTransform cameraLine, viewBox, songLine;
        readonly List<Image> barPool = new List<Image>();
        readonly List<TimelineMarker> markerPool = new List<TimelineMarker>();
        Text lengthLabel;
        float lastLength = -1f;
        int lastVersion = -1;
        int version;

        public static TimelineStrip Create(EditorUI ui, RectTransform host)
        {
            var strip = host.gameObject.AddComponent<TimelineStrip>();
            strip.ui = ui;
            strip.editor = ui.editor;
            strip.rt = host;
            strip.Build();
            return strip;
        }

        float LevelLength => Mathf.Max(10f, editor.level.GetFinishX() + 6f, editor.SongEndX.HasValue ? editor.SongEndX.Value + 6f : 0f);

        void Build()
        {
            viewBox = UIFactory.Panel(rt, "View", new Color(1f, 1f, 1f, 0.08f));
            viewBox.GetComponent<Image>().raycastTarget = false;
            cameraLine = UIFactory.Panel(rt, "Camera", new Color(0.4f, 0.85f, 1f, 0.9f));
            cameraLine.GetComponent<Image>().raycastTarget = false;
            songLine = UIFactory.Panel(rt, "SongEnd", new Color(EditorGrid.SongColor.r, EditorGrid.SongColor.g, EditorGrid.SongColor.b, 0.9f));
            songLine.GetComponent<Image>().raycastTarget = false;
            lengthLabel = UIFactory.Label(rt, "", 10, TextAnchor.MiddleRight, UIFactory.TextDim);
            lengthLabel.raycastTarget = false;
            UIFactory.Anchor(lengthLabel.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-90, 0), new Vector2(-4, 0));
            editor.LevelChanged += () => version++;
            editor.ViewOptionsChanged += () => version++;
            Rebuild();
        }

        Image Bar(int i)
        {
            while (barPool.Count <= i)
            {
                var r = UIFactory.Rect(rt, "Bar");
                var img = r.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                barPool.Add(img);
            }
            return barPool[i];
        }

        TimelineMarker Marker(int i)
        {
            while (markerPool.Count <= i)
            {
                var r = UIFactory.Rect(rt, "Marker");
                var img = r.gameObject.AddComponent<Image>();
                var m = r.gameObject.AddComponent<TimelineMarker>();
                m.strip = this;
                m.image = img;
                markerPool.Add(m);
            }
            return markerPool[i];
        }

        /// <summary>Canvas x of a level x.</summary>
        float ToLocal(float x) => x / LevelLength * rt.rect.width;
        public float ToLevel(float localX) => Mathf.Clamp(localX / Mathf.Max(1f, rt.rect.width) * LevelLength, 0f, LevelLength);

        void Rebuild()
        {
            float width = rt.rect.width;
            float h = rt.rect.height;
            float length = LevelLength;
            // bar lines (every 4 beats), thinned out so the strip never draws more than ~160 of them
            float bar = editor.BeatLength * 4f;
            int bars = bar > 0.01f ? Mathf.FloorToInt(length / bar) : 0;
            int step = Mathf.Max(1, Mathf.CeilToInt(bars / 160f));
            int used = 0;
            for (int b = 1; b <= bars; b += step)
            {
                var img = Bar(used++);
                img.enabled = true;
                img.color = (b / step) % 4 == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.16f);
                var r = img.rectTransform;
                r.anchorMin = new Vector2(0, 0);
                r.anchorMax = new Vector2(0, 1);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(ToLocal(b * bar), 0);
                r.sizeDelta = new Vector2(1f, 0);
                r.offsetMin = new Vector2(r.offsetMin.x, 2);
                r.offsetMax = new Vector2(r.offsetMax.x, -2);
            }
            for (int i = used; i < barPool.Count; i++) barPool[i].enabled = false;

            // trigger and portal markers
            int m = 0;
            foreach (var o in editor.level.objects)
            {
                var def = ObjectCatalog.Get(o.type);
                if (def == null) continue;
                bool trigger = def.kind == ObjectKind.Trigger;
                bool portal = def.kind == ObjectKind.Portal;
                bool special = def.kind == ObjectKind.Finish || o.type == "checkpoint" || o.type == "start_pos";
                if (!trigger && !portal && !special) continue;
                if (m >= 400) break;
                var mk = Marker(m++);
                mk.uid = o.uid;
                mk.draggable = trigger;
                mk.gameObject.SetActive(true);
                mk.image.color = trigger ? def.primaryColor : (portal ? new Color(def.primaryColor.r, def.primaryColor.g, def.primaryColor.b, 0.9f) : new Color(0.4f, 1f, 0.5f, 0.9f));
                var r = mk.image.rectTransform;
                r.anchorMin = new Vector2(0, 0.5f);
                r.anchorMax = new Vector2(0, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                float size = trigger ? h - 8f : h - 12f;
                r.sizeDelta = new Vector2(trigger ? size : 4f, size);
                r.anchoredPosition = new Vector2(ToLocal(o.x), 0);
            }
            for (int i = m; i < markerPool.Count; i++) markerPool[i].gameObject.SetActive(false);
            lengthLabel.text = Mathf.RoundToInt(length) + " blocks · " + Mathf.RoundToInt(length / MountCatalog.Speed(editor.level.settings.startSpeed)) + "s";
            lastLength = length;
            lastVersion = version;
        }

        public void Tick()
        {
            if (rt == null || editor.level == null) return;
            if (lastVersion != version || Mathf.Abs(LevelLength - lastLength) > 0.01f || Mathf.Abs(rt.rect.width - lastWidth) > 0.5f) Rebuild();
            lastWidth = rt.rect.width;
            float cx = ToLocal(editor.editorCamera.Position.x);
            cameraLine.anchorMin = new Vector2(0, 0);
            cameraLine.anchorMax = new Vector2(0, 1);
            cameraLine.pivot = new Vector2(0.5f, 0.5f);
            cameraLine.anchoredPosition = new Vector2(cx, 0);
            cameraLine.sizeDelta = new Vector2(2f, 0);
            bool song = editor.showSongEnd && editor.SongEndX.HasValue;
            if (songLine.gameObject.activeSelf != song) songLine.gameObject.SetActive(song);
            if (song)
            {
                songLine.anchorMin = new Vector2(0, 0);
                songLine.anchorMax = new Vector2(0, 1);
                songLine.pivot = new Vector2(0.5f, 0.5f);
                songLine.anchoredPosition = new Vector2(ToLocal(Mathf.Min(editor.SongEndX.Value, LevelLength)), 0);
                songLine.sizeDelta = new Vector2(3f, 0);
            }
            var view = editor.editorCamera.ViewRect;
            viewBox.anchorMin = new Vector2(0, 0);
            viewBox.anchorMax = new Vector2(0, 1);
            viewBox.pivot = new Vector2(0, 0.5f);
            viewBox.anchoredPosition = new Vector2(ToLocal(Mathf.Max(0f, view.xMin)), 0);
            viewBox.sizeDelta = new Vector2(Mathf.Max(2f, ToLocal(view.xMax) - ToLocal(Mathf.Max(0f, view.xMin))), 0);
        }
        float lastWidth;

        float LocalX(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local);
            return local.x + rt.rect.width * rt.pivot.x;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            float x = ToLevel(LocalX(e));
            editor.editorCamera.Position = new Vector2(x, editor.editorCamera.Position.y);
        }

        // ---- marker drags ------------------------------------------------------------------------

        public void BeginMarkerDrag(TimelineMarker m)
        {
            editor.RecordUndo("Retime trigger");
        }

        public void DragMarker(TimelineMarker m, PointerEventData e)
        {
            var o = editor.level.FindByUid(m.uid);
            if (o == null) return;
            float x = ToLevel(LocalX(e));
            if (editor.beatSnap)
            {
                float beat = editor.BeatLength / Mathf.Max(1, editor.beatDivision);
                x = Mathf.Round(x / beat) * beat;
            }
            else if (editor.snapToGrid) x = GeoMath.SnapValue(x, editor.gridSize);
            o.x = x;
            editor.RefreshView(o.uid);
            m.image.rectTransform.anchoredPosition = new Vector2(ToLocal(o.x), 0);
        }

        public void EndMarkerDrag(TimelineMarker m)
        {
            editor.MarkDirty();
            var o = editor.level.FindByUid(m.uid);
            if (o != null)
            {
                editor.Select(m.uid, false);
                ui.Toast("Trigger moved to x " + o.x.ToString("0.##"));
            }
        }

        public void ClickMarker(TimelineMarker m)
        {
            var o = editor.level.FindByUid(m.uid);
            if (o == null) return;
            editor.editorCamera.Position = new Vector2(o.x, editor.editorCamera.Position.y);
            editor.Select(m.uid, false);
            if (editor.Mode != EditorMode.Edit) editor.SetMode(EditorMode.Edit);
        }
    }

    /// <summary>One dot on the timeline. Triggers can be dragged to a new x; anything can be clicked to jump there.</summary>
    public class TimelineMarker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public TimelineStrip strip;
        public Image image;
        public int uid;
        public bool draggable;
        bool dragging;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!draggable) return;
            dragging = true;
            strip.BeginMarkerDrag(this);
        }

        public void OnDrag(PointerEventData e)
        {
            if (dragging) strip.DragMarker(this, e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            strip.EndMarkerDrag(this);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging) strip.ClickMarker(this);
        }
    }
}
