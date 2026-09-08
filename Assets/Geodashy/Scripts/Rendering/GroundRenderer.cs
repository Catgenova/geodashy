using Geodashy.Core;
using UnityEngine;

namespace Geodashy.Rendering
{
    /// <summary>Tiled floor (and optional ceiling for flying sections) that follows the camera.</summary>
    public class GroundRenderer : MonoBehaviour
    {
        public const int GroundSorting = -5000;
        public const int LineSorting = -4999;

        public Camera targetCamera;
        public LevelSettings settings;
        public bool showCeiling;
        public float ceilingY;

        SpriteRenderer ground;
        SpriteRenderer line;
        SpriteRenderer ceiling;
        SpriteRenderer ceilingLine;

        public static GroundRenderer Create(Transform parent, Camera cam, LevelSettings settings)
        {
            var go = new GameObject("Ground");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<GroundRenderer>();
            g.targetCamera = cam;
            g.settings = settings;
            g.Build();
            return g;
        }

        void Build()
        {
            ground = SpriteLibrary.CreateRenderer("Floor", transform, null, GroundSorting);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.tileMode = SpriteTileMode.Continuous;
            line = SpriteLibrary.CreateRenderer("FloorLine", transform, PlaceholderSpriteFactory.WhiteSquare(), LineSorting);
            ceiling = SpriteLibrary.CreateRenderer("Ceiling", transform, null, GroundSorting);
            ceiling.drawMode = SpriteDrawMode.Tiled;
            ceiling.tileMode = SpriteTileMode.Continuous;
            ceilingLine = SpriteLibrary.CreateRenderer("CeilingLine", transform, PlaceholderSpriteFactory.WhiteSquare(), LineSorting);
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (settings == null) return;
            var theme = ThemeCatalog.GetGround(settings.groundTheme);
            var sprite = SpriteLibrary.ForGround(theme);
            ground.sprite = sprite;
            ceiling.sprite = sprite;
            SetGroundColor(settings.groundColor);
            SetLineColor(settings.lineColor);
            ceilingY = settings.groundY + settings.ceilingHeight;
            LateUpdate();
        }

        public void SetTheme(string id)
        {
            settings.groundTheme = id;
            ApplySettings();
        }

        public void SetGroundColor(Color c)
        {
            ground.color = c;
            ceiling.color = c;
        }

        public void SetLineColor(Color c)
        {
            line.color = c;
            ceilingLine.color = c;
        }

        void LateUpdate()
        {
            if (targetCamera == null || ground.sprite == null) return;
            float camX = targetCamera.transform.position.x;
            float halfW = targetCamera.orthographicSize * targetCamera.aspect;
            float groundY = settings != null ? settings.groundY : 0f;
            float tileW = ground.sprite.bounds.size.x;
            float width = halfW * 2f + tileW * 2f;
            float depth = 12f;

            ground.size = new Vector2(width, depth);
            float phase = Mathf.Repeat(-(camX - width / 2f), tileW);
            float left = camX - width / 2f + phase;
            // ground sprite pivot is top-centre
            ground.transform.position = new Vector3(left + width / 2f, groundY, 0f);
            line.transform.position = new Vector3(camX, groundY, 0f);
            line.transform.localScale = new Vector3(halfW * 2f + 2f, 0.05f, 1f);

            ceiling.enabled = showCeiling;
            ceilingLine.enabled = showCeiling;
            if (showCeiling)
            {
                ceiling.size = new Vector2(width, depth);
                // flipped sprite: pivot stays at top-centre of the unflipped sprite, i.e. bottom of the flipped one
                ceiling.transform.position = new Vector3(left + width / 2f, ceilingY, 0f);
                ceiling.transform.localScale = new Vector3(1f, -1f, 1f);
                ceilingLine.transform.position = new Vector3(camX, ceilingY, 0f);
                ceilingLine.transform.localScale = new Vector3(halfW * 2f + 2f, 0.05f, 1f);
            }
        }
    }
}
