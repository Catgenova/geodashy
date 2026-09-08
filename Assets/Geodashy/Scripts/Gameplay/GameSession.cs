using System;
using Geodashy.Core;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geodashy.Gameplay
{
    /// <summary>Plays a level outside the editor: builds background + ground and hosts a GameRunner.</summary>
    public class GameSession : MonoBehaviour
    {
        public GameRunner runner;
        ParallaxBackground background;
        GroundRenderer ground;
        Action onExit;

        public void Begin(LevelData level, Camera cam, Difficulty difficulty, Action exit)
        {
            onExit = exit;
            var data = level.DeepClone();
            background = ParallaxBackground.Create(transform, cam, data.settings);
            ground = GroundRenderer.Create(transform, cam, data.settings);
            var props = GroundProps.Create(transform, cam, data.settings, x => LevelObjectNear(data, x));
            cam.backgroundColor = data.settings.backgroundColor;
            var go = new GameObject("Game Runner");
            go.transform.SetParent(transform, false);
            runner = go.AddComponent<GameRunner>();
            runner.Begin(data, cam, background, ground, null, () => onExit?.Invoke(), difficulty, "menu");
        }

        public static bool LevelObjectNear(LevelData data, float x)
        {
            float groundY = data.settings.groundY;
            foreach (var o in data.objects)
            {
                if (Mathf.Abs(o.x - x) < 2.2f && o.y < groundY + 3.5f) return true;
            }
            return false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[Key.Escape].wasPressedThisFrame && runner != null) runner.TogglePause();
        }

        public void Shutdown()
        {
            if (runner != null) runner.Shutdown();
        }
    }
}
