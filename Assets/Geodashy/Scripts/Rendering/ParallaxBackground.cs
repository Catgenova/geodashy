using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>
    /// Sky gradient plus three tiled silhouette layers (far, mid, near) that scroll at
    /// different fractions of the camera speed. Purely visual - nothing here collides.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        public const int SkySorting = -10000;
        public const int FarSorting = -9000;
        public const int MidSorting = -8000;
        public const int NearSorting = -7000;

        public Camera targetCamera;
        public LevelSettings settings;

        SpriteRenderer sky;
        // blurred, washed-out copy of the far layer drawn behind it: a hazier range further off
        SpriteRenderer haze;
        string hazeSource = "";
        readonly SpriteRenderer[] layers = new SpriteRenderer[3];
        readonly string[] layerIds = new string[3];
        readonly float[] factors = new float[3];
        readonly float[] yOffsets = new float[3];
        readonly float[] scales = { 1f, 1f, 1f };
        readonly Color[] tints = { Color.white, Color.white, Color.white };
        readonly bool[] visible = { true, true, true };

        // fading between themes (BG Switch trigger)
        readonly SpriteRenderer[] fadeLayers = new SpriteRenderer[3];
        float fadeT = -1f;
        float fadeDuration;

        Color skyTop, skyBottom;
        Color bgTint = Color.white;
        float pulse;
        float baseCameraY;

        public static ParallaxBackground Create(Transform parent, Camera cam, LevelSettings settings)
        {
            var go = new GameObject("Parallax Background");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParallaxBackground>();
            p.targetCamera = cam;
            p.settings = settings;
            p.Build();
            return p;
        }

        void Build()
        {
            sky = SpriteLibrary.CreateRenderer("Sky", transform, PlaceholderSpriteFactory.Gradient(Color.white, Color.white), SkySorting);
            for (int i = 0; i < 3; i++)
            {
                layers[i] = SpriteLibrary.CreateRenderer("Layer" + i, transform, null, FarSorting + i * 1000);
                layers[i].drawMode = SpriteDrawMode.Tiled;
                layers[i].tileMode = SpriteTileMode.Continuous;
                fadeLayers[i] = SpriteLibrary.CreateRenderer("FadeLayer" + i, transform, null, FarSorting + i * 1000 + 1);
                fadeLayers[i].drawMode = SpriteDrawMode.Tiled;
                fadeLayers[i].tileMode = SpriteTileMode.Continuous;
                fadeLayers[i].enabled = false;
            }
            haze = SpriteLibrary.CreateRenderer("Haze", transform, null, FarSorting - 1);
            haze.drawMode = SpriteDrawMode.Tiled;
            haze.tileMode = SpriteTileMode.Continuous;
            haze.enabled = false;
            baseCameraY = settings != null ? settings.groundY + 4.5f : 4.5f;
            ApplySettings();
        }

        /// <summary>Builds the haze sprite from the far layer: box-blurred, desaturated and lifted towards the sky.</summary>
        void RefreshHaze()
        {
            if (haze == null || layers[0].sprite == null)
            {
                if (haze != null) haze.enabled = false;
                return;
            }
            var src = layers[0].sprite;
            string key = layerIds[0] + ":" + src.GetInstanceID();
            if (key == hazeSource && haze.sprite != null)
            {
                haze.enabled = visible[0];
                return;
            }
            hazeSource = key;
            try
            {
                var tex = src.texture;
                var rect = src.textureRect;
                int w = (int)rect.width, h = (int)rect.height;
                if (w < 4 || h < 4) throw new System.Exception("tiny");
                var px = tex.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), w, h);
                // horizontal then vertical box blur, radius 3, alpha-weighted so edges bleed softly
                const int r = 3;
                var tmp = new Color[px.Length];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color acc = Color.clear;
                        int n = 0;
                        for (int k = -r; k <= r; k++)
                        {
                            int xx = Mathf.Clamp(x + k, 0, w - 1);
                            acc += px[y * w + xx];
                            n++;
                        }
                        tmp[y * w + x] = acc / n;
                    }
                var outPx = new Color[px.Length];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color acc = Color.clear;
                        int n = 0;
                        for (int k = -r; k <= r; k++)
                        {
                            int yy = Mathf.Clamp(y + k, 0, h - 1);
                            acc += tmp[yy * w + x];
                            n++;
                        }
                        var c = acc / n;
                        float lum = 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
                        var grey = new Color(lum, lum, lum, c.a);
                        var washed = Color.Lerp(grey, c, 0.35f);
                        washed = Color.Lerp(washed, new Color(0.85f, 0.88f, 0.95f, washed.a), 0.45f);
                        washed.a *= 0.75f;
                        outPx[y * w + x] = washed;
                    }
                var t2 = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
                t2.SetPixels(outPx);
                t2.Apply(false, false);
                if (haze.sprite != null && haze.sprite.texture != null && haze.sprite.texture != tex) Destroy(haze.sprite.texture);
                haze.sprite = Sprite.Create(t2, new Rect(0, 0, w, h), src.pivot / new Vector2(w, h), src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                haze.enabled = visible[0];
            }
            catch (System.Exception)
            {
                haze.sprite = null;
                haze.enabled = false;
            }
        }

        /// <summary>Re-reads theme, overrides, parallax factors and colours from the level settings.</summary>
        public void ApplySettings()
        {
            if (settings == null) return;
            LevelSerializer.MigrateParallax(settings);
            var theme = ThemeCatalog.GetBackground(settings.backgroundTheme);
            string[] themeLayers = { theme.far, theme.mid, theme.near };
            for (int i = 0; i < 3; i++)
            {
                var ls = settings.Layer(i);
                SetLayerSprite(i, string.IsNullOrEmpty(ls.layerId) ? themeLayers[i] : ls.layerId);
                factors[i] = Mathf.Clamp(ls.parallax, -1f, 2f);
                yOffsets[i] = ls.yOffset;
                scales[i] = Mathf.Clamp(ls.scale, 0.1f, 10f);
                tints[i] = ls.tint;
                visible[i] = ls.visible;
                layers[i].flipX = ls.flipX;
                fadeLayers[i].flipX = ls.flipX;
                layers[i].enabled = ls.visible;
            }
            skyTop = theme.skyTop;
            skyBottom = theme.skyBottom;
            RefreshHaze();
            SetBackgroundColor(settings.backgroundColor);
            LateUpdate();
        }

        /// <summary>Tints the sky and layers (driven by the BG colour channel).</summary>
        public void SetBackgroundColor(Color c)
        {
            bgTint = c;
            sky.sprite = PlaceholderSpriteFactory.Gradient(Color.Lerp(skyBottom, c, 0.6f), Color.Lerp(skyTop, c, 0.4f));
            for (int i = 0; i < 3; i++)
            {
                float blend = 0.55f - i * 0.2f;
                var col = Color.Lerp(Color.white, c, blend) * tints[i];
                col.a = tints[i].a;
                layers[i].color = col;
                var fc = col;
                fc.a = fadeLayers[i].color.a;
                fadeLayers[i].color = fc;
            }
        }

        void SetLayerSprite(int index, string layerId)
        {
            var def = ThemeCatalog.GetLayer(layerId);
            layerIds[index] = def.id;
            layers[index].sprite = SpriteLibrary.ForBackgroundLayer(def);
        }

        /// <summary>Beat pulse: briefly brightens the sky.</summary>
        public void Pulse(float strength = 1f)
        {
            pulse = Mathf.Max(pulse, strength);
        }

        /// <summary>Crossfades to another theme (BG Switch trigger).</summary>
        public void SwitchTheme(string themeId, float duration)
        {
            settings.backgroundTheme = themeId;
            settings.ResetLayersToTheme();
            if (duration <= 0.01f)
            {
                ApplySettings();
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                fadeLayers[i].sprite = layers[i].sprite;
                fadeLayers[i].enabled = true;
            }
            fadeT = 0f;
            fadeDuration = duration;
            ApplySettings();
        }

        void LateUpdate()
        {
            if (targetCamera == null) return;
            float camX = targetCamera.transform.position.x;
            float camY = targetCamera.transform.position.y;
            float halfH = targetCamera.orthographicSize;
            float halfW = halfH * targetCamera.aspect;

            sky.transform.position = new Vector3(camX, camY, 0f);
            sky.transform.localScale = new Vector3(halfW * 2f + 1f, halfH * 2f + 1f, 1f);
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
            sky.color = Color.Lerp(new Color(0.93f, 0.93f, 0.95f, 1f), Color.white, pulse);

            float groundY = settings != null ? settings.groundY : 0f;
            for (int i = 0; i < 3; i++)
            {
                if (visible[i]) PositionLayer(layers[i], i, camX, camY, halfW, halfH, groundY);
                if (fadeLayers[i].enabled) PositionLayer(fadeLayers[i], i, camX, camY, halfW, halfH, groundY);
            }
            if (haze != null && haze.enabled && haze.sprite != null)
            {
                // further away than the far layer: moves less, sits higher, bobs very slowly
                float scale = scales[0] * 1.18f;
                float tileW = Mathf.Max(0.5f, haze.sprite.bounds.size.x * scale);
                float factor = Mathf.Clamp01(factors[0] + (1f - factors[0]) * 0.45f);
                float width = halfW * 2f + tileW * 2f;
                haze.transform.localScale = new Vector3(scale, scale, 1f);
                haze.size = new Vector2(width / scale, haze.sprite.bounds.size.y);
                float phase = Mathf.Repeat(camX * (1f - factor) - (camX - width / 2f) + tileW * 0.37f, tileW);
                float left = camX - width / 2f + phase;
                float bob = Mathf.Sin(Time.time * 0.45f) * 0.12f;
                float bottom = groundY - 0.5f + yOffsets[0] + 1.4f + bob + (camY - baseCameraY) * (1f - factor);
                haze.transform.position = new Vector3(left + width / 2f, bottom, 0f);
                var c = layers[0].color;
                haze.color = new Color(c.r, c.g, c.b, c.a * 0.9f);
            }

            if (fadeT >= 0f)
            {
                fadeT += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(fadeT / Mathf.Max(0.01f, fadeDuration));
                for (int i = 0; i < 3; i++)
                {
                    var c = fadeLayers[i].color;
                    c.a = a;
                    fadeLayers[i].color = c;
                }
                if (a <= 0f)
                {
                    fadeT = -1f;
                    for (int i = 0; i < 3; i++) fadeLayers[i].enabled = false;
                }
            }
        }

        void PositionLayer(SpriteRenderer sr, int index, float camX, float camY, float halfW, float halfH, float groundY)
        {
            if (sr.sprite == null) return;
            float scale = scales[index];
            float tileW = Mathf.Max(0.5f, sr.sprite.bounds.size.x * scale);
            float layerH = sr.sprite.bounds.size.y; // local units, scaled by the transform
            float factor = factors[index];
            float width = halfW * 2f + tileW * 2f;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.size = new Vector2(width / scale, layerH);
            float phase = Mathf.Repeat(camX * (1f - factor) - (camX - width / 2f), tileW);
            float left = camX - width / 2f + phase;
            float bottom = groundY - 0.5f + yOffsets[index] + (camY - baseCameraY) * (1f - factor) - index * 0.5f;
            // the sprite pivot is bottom-centre, tiling grows symmetrically around the pivot
            sr.transform.position = new Vector3(left + width / 2f, bottom, 0f);
        }
    }
}
