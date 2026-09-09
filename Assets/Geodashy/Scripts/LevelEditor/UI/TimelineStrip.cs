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
        Image waveform;
        Sprite waveformSprite;
        string waveKey = "";
        const int WaveColumns = 640;
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
            var wave = UIFactory.Rect(rt, "Waveform");
            UIFactory.Stretch(wave, 0, 1, 0, 1);
            waveform = wave.gameObject.AddComponent<Image>();
            waveform.raycastTarget = false;
            waveform.preserveAspect = false;
            waveform.color = new Color(EditorGrid.SongColor.r, EditorGrid.SongColor.g, EditorGrid.SongColor.b, 0.55f);
            waveform.enabled = false;
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
            editor.SongChanged += () => { waveKey = ""; version++; };
            Rebuild();
        }

        /// <summary>
        /// Paints the song's loudness along the strip: each column is a level x, turned into a song time by walking the
        /// speed portals, so the waveform stretches and squeezes exactly like the music will while riding.
        /// </summary>
        void RebuildWaveform(float length)
        {
            var data = editor.SongWaveform;
            if (data == null || data.Length == 0 || editor.SongLength <= 0f)
            {
                waveform.enabled = false;
                waveKey = "";
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append(editor.SongLength.ToString("0.00")).Append('|').Append(editor.level.settings.songOffset.ToString("0.000")).Append('|').Append(length.ToString("0.0")).Append('|').Append(editor.level.settings.startSpeed);
            foreach (var o in editor.level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d != null && d.kind == ObjectKind.Portal && d.portalType == PortalType.Speed) sb.Append(';').Append(o.x.ToString("0.0")).Append(':').Append((int)d.portalSpeed);
            }
            var key = sb.ToString();
            if (key == waveKey && waveformSprite != null)
            {
                waveform.enabled = true;
                return;
            }
            waveKey = key;
            int h = 24;
            var r = new Geodashy.Rendering.Raster(WaveColumns, h);
            var col = Color.white;
            float offset = editor.level.settings.songOffset;
            // one speed-portal walk for the whole strip: columns are in x order, so the travel time only grows
            var portals = new List<KeyValuePair<float, int>>();
            foreach (var o in editor.level.objects)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d != null && d.kind == ObjectKind.Portal && d.portalType == PortalType.Speed) portals.Add(new KeyValuePair<float, int>(o.x, (int)d.portalSpeed));
            }
            portals.Sort((a, b) => a.Key.CompareTo(b.Key));
            int pi = 0;
            float x = 0f, t = 0f;
            float speed = MountCatalog.Speed(editor.level.settings.startSpeed);
            for (int c = 0; c < WaveColumns; c++)
            {
                float target = (c + 0.5f) / WaveColumns * length;
                while (pi < portals.Count && portals[pi].Key <= target)
                {
                    if (portals[pi].Key > x)
                    {
                        t += (portals[pi].Key - x) / speed;
                        x = portals[pi].Key;
                    }
                    speed = MountCatalog.Speed(portals[pi].Value);
                    pi++;
                }
                float time = t + (target - x) / speed + offset;
                if (time < 0f || time > editor.SongLength) continue;
                int bin = Mathf.Clamp(Mathf.FloorToInt(time / editor.SongLength * data.Length), 0, data.Length - 1);
                float amp = Mathf.Clamp01(data[bin]);
                float half = Mathf.Max(0.5f, amp * h * 0.5f);
                r.FillRect(c, h * 0.5f - half, c + 1, h * 0.5f + half, col);
            }
            if (waveformSprite != null) UnityEngine.Object.Destroy(waveformSprite.texture);
            waveformSprite = r.ToSprite(1f);
            waveform.sprite = waveformSprite;
            waveform.enabled = true;
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
            RebuildWaveform(length);

            // bookmarks: cyan diamonds you can click to jump there
            int m = 0;
            if (editor.level.bookmarks != null)
            {
                for (int bi = 0; bi < editor.level.bookmarks.Count && m < 380; bi++)
                {
                    var bm = editor.level.bookmarks[bi];
                    var mk = Marker(m++);
                    mk.uid = -1;
                    mk.bookmark = bi;
                    mk.draggable = false;
                    mk.gameObject.SetActive(true);
                    mk.image.color = new Color(0.45f, 0.95f, 1f, 0.95f);
                    var br = mk.image.rectTransform;
                    br.anchorMin = new Vector2(0, 0.5f);
                    br.anchorMax = new Vector2(0, 0.5f);
                    br.pivot = new Vector2(0.5f, 0.5f);
                    br.sizeDelta = new Vector2(h - 10f, h - 10f);
                    br.localRotation = Quaternion.Euler(0, 0, 45f);
                    br.anchoredPosition = new Vector2(ToLocal(bm.x), 0);
                }
            }

            // trigger and portal markers
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
                mk.bookmark = -1;
                mk.draggable = trigger;
                mk.gameObject.SetActive(true);
                mk.image.rectTransform.localRotation = Quaternion.identity;
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
            if (m.bookmark >= 0)
            {
                editor.GoToBookmark(m.bookmark, false);
                return;
            }
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
        public int bookmark = -1;
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
