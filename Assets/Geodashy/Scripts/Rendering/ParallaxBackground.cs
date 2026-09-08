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
            baseCameraY = settings != null ? settings.groundY + 4.5f : 4.5f;
            ApplySettings();
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

            float groundY = settings != null ? settings.groundY : 0f;
            for (int i = 0; i < 3; i++)
            {
                if (visible[i]) PositionLayer(layers[i], i, camX, camY, halfW, halfH, groundY);
                if (fadeLayers[i].enabled) PositionLayer(fadeLayers[i], i, camX, camY, halfW, halfH, groundY);
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
