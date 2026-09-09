using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Screenshots and five-second clips. While enabled it keeps the last ~5 s of downscaled frames in a ring;
    /// SaveClip writes them as a looping GIF, SaveScreenshot writes a PNG. Both are stamped with the player's
    /// crest and the quest name in the corner and land in the captures folder.
    /// </summary>
    public class CaptureRecorder : MonoBehaviour
    {
        public const int ClipWidth = 320;
        public const float ClipSeconds = 5f;
        public const float ClipInterval = 0.1f;

        public string questName = "";
        public bool recording = true;

        readonly List<Color32[]> ring = new List<Color32[]>();
        int ringStart;
        int clipHeight;
        RenderTexture small;
        Texture2D readback;
        float timer;
        bool busy;

        public static string CapturesFolder
        {
            get
            {
                string dir;
                if (Application.isMobilePlatform) dir = Path.Combine(Application.persistentDataPath, "geodashy", "captures");
                else
                {
                    var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    dir = string.IsNullOrEmpty(pictures) ? Path.Combine(Application.persistentDataPath, "geodashy", "captures") : Path.Combine(pictures, "LyreFlyer");
                }
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static CaptureRecorder Create(Transform parent, string questName)
        {
            var go = new GameObject("Capture Recorder");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<CaptureRecorder>();
            c.questName = questName;
            return c;
        }

        void OnDestroy()
        {
            if (small != null) small.Release();
        }

        void LateUpdate()
        {
            if (!recording || busy) return;
            timer += Time.unscaledDeltaTime;
            if (timer < ClipInterval) return;
            timer = 0f;
            StartCoroutine(GrabFrame());
        }

        IEnumerator GrabFrame()
        {
            busy = true;
            yield return new WaitForEndOfFrame();
            try
            {
                int sw = Screen.width, sh = Screen.height;
                if (sw <= 0 || sh <= 0) yield break;
                int h = Mathf.Max(8, Mathf.RoundToInt(ClipWidth * sh / (float)sw));
                if (small == null || clipHeight != h)
                {
                    if (small != null) small.Release();
                    small = new RenderTexture(ClipWidth, h, 0);
                    readback = new Texture2D(ClipWidth, h, TextureFormat.RGBA32, false);
                    clipHeight = h;
                    ring.Clear();
                    ringStart = 0;
                }
                var full = ScreenCapture.CaptureScreenshotAsTexture();
                Graphics.Blit(full, small);
                var prev = RenderTexture.active;
                RenderTexture.active = small;
                readback.ReadPixels(new Rect(0, 0, ClipWidth, h), 0, 0);
                readback.Apply();
                RenderTexture.active = prev;
                Destroy(full);
                var pixels = readback.GetPixels32();
                int capacity = Mathf.RoundToInt(ClipSeconds / ClipInterval);
                if (ring.Count < capacity) ring.Add(pixels);
                else
                {
                    ring[ringStart] = pixels;
                    ringStart = (ringStart + 1) % ring.Count;
                }
            }
            finally
            {
                busy = false;
            }
        }

        /// <summary>Writes the ring as a GIF and returns the path (empty when nothing was recorded).</summary>
        public string SaveClip()
        {
            if (ring.Count < 2) return "";
            var frames = new List<Color32[]>(ring.Count);
            for (int i = 0; i < ring.Count; i++) frames.Add(ring[(ringStart + i) % ring.Count]);
            foreach (var f in frames) Stamp(f, ClipWidth, clipHeight);
            var path = Path.Combine(CapturesFolder, "lyreflyer_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".gif");
            GifEncoder.Write(path, frames, ClipWidth, clipHeight, Mathf.RoundToInt(ClipInterval * 100f));
            return path;
        }

        /// <summary>Grabs the screen at full size, stamps it and writes a PNG. Runs over a frame; the callback gets the path.</summary>
        public void SaveScreenshot(Action<string> done)
        {
            StartCoroutine(ScreenshotRoutine(done));
        }

        IEnumerator ScreenshotRoutine(Action<string> done)
        {
            yield return new WaitForEndOfFrame();
            string path = "";
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                var pixels = tex.GetPixels32();
                Stamp(pixels, tex.width, tex.height);
                tex.SetPixels32(pixels);
                tex.Apply();
                path = Path.Combine(CapturesFolder, "lyreflyer_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Screenshot failed: " + e.Message);
            }
            done?.Invoke(path);
        }

        /// <summary>Draws the crest and the quest name into the bottom-left corner of a pixel buffer.</summary>
        void Stamp(Color32[] pixels, int w, int h)
        {
            int size = Mathf.Clamp(h / 8, 16, 96);
            var crest = PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary, size);
            var ctex = crest.texture;
            int margin = Mathf.Max(4, h / 40);
            try
            {
                var cp = ctex.GetPixels32();
                int cw = ctex.width, ch = ctex.height;
                for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                {
                    var c = cp[y * cw + x];
                    if (c.a < 30) continue;
                    int px = margin + x, py = margin + y;
                    if (px < 0 || py < 0 || px >= w || py >= h) continue;
                    var dst = pixels[py * w + px];
                    float a = c.a / 255f;
                    pixels[py * w + px] = new Color32((byte)(c.r * a + dst.r * (1 - a)), (byte)(c.g * a + dst.g * (1 - a)), (byte)(c.b * a + dst.b * (1 - a)), 255);
                }
            }
            catch (Exception)
            {
                // crest texture not readable on this platform: skip the badge
            }
            // quest name in the bitmap font beside the crest
            var text = (string.IsNullOrEmpty(questName) ? GameInfo.Title : questName).ToUpperInvariant();
            if (text.Length > 24) text = text.Substring(0, 24);
            var m = BitmapFont.Measure(text);
            int scale = Mathf.Max(1, size / 24);
            int tw = m.x * scale, th = m.y * scale;
            if (tw <= 0 || th <= 0) return;
            var r = new Raster(m.x, m.y);
            r.Clear(new Color(0, 0, 0, 0));
            BitmapFont.DrawCentered(r, text, Color.white, 0f, 1);
            int ox = margin + size + margin, oy = margin + (size - th) / 2;
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                var c = r.pixels[(y / scale) * r.width + (x / scale)];
                if (c.a < 30) continue;
                int px = ox + x, py = oy + y;
                if (px < 0 || py < 0 || px >= w || py >= h) continue;
                pixels[py * w + px] = new Color32(255, 240, 200, 255);
                // one-pixel shadow so the text reads on bright frames
                int sx = px + 1, sy = py - 1;
                if (sx < w && sy >= 0 && pixels[sy * w + sx].r > 60) pixels[sy * w + sx] = new Color32(20, 12, 20, 255);
            }
        }
    }
}
