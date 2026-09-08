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

        public void Begin(LevelData level, Camera cam, bool practice, Action exit)
        {
            onExit = exit;
            var data = level.DeepClone();
            background = ParallaxBackground.Create(transform, cam, data.settings);
            ground = GroundRenderer.Create(transform, cam, data.settings);
            cam.backgroundColor = data.settings.backgroundColor;
            var go = new GameObject("Game Runner");
            go.transform.SetParent(transform, false);
            runner = go.AddComponent<GameRunner>();
            runner.Begin(data, cam, background, ground, null, () => onExit?.Invoke(), practice, "menu");
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
