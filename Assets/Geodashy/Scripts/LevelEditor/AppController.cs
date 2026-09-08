using System;
using Geodashy.Core;
using Geodashy.Editing.UI;
using Geodashy.Gameplay;
using UnityEngine;

namespace Geodashy.Editing
{
    /// <summary>Top-level state: main menu, playing a level, or editing one. Owns the camera hand-offs between them.</summary>
    public class AppController : MonoBehaviour
    {
        public static AppController Instance { get; private set; }

        public Camera cam;
        MainMenu menu;
        LevelEditor editor;
        GameSession session;

        void Awake()
        {
            Instance = this;
        }

        public void Initialize(Camera camera, bool startInEditor)
        {
            cam = camera;
            if (startInEditor) OpenEditor(null, null);
            else ShowMenu();
        }

        void CloseAll()
        {
            if (editor != null)
            {
                Destroy(editor.gameObject);
                editor = null;
            }
            if (session != null)
            {
                session.Shutdown();
                Destroy(session.gameObject);
                session = null;
            }
            if (menu != null)
            {
                Destroy(menu.gameObject);
                menu = null;
            }
            cam.ResetProjectionMatrix();
            cam.transform.rotation = Quaternion.identity;
        }

        public void ShowMenu()
        {
            CloseAll();
            var go = new GameObject("Main Menu");
            go.transform.SetParent(transform, false);
            menu = go.AddComponent<MainMenu>();
            menu.Initialize(this, cam);
        }

        /// <summary>Opens the editor on a level (null = last autosave or a starter level).</summary>
        public void OpenEditor(LevelData level, string path)
        {
            CloseAll();
            var go = new GameObject("Level Editor");
            go.transform.SetParent(transform, false);
            editor = go.AddComponent<LevelEditor>();
            editor.Initialize(cam, level, path);
        }

        public void PlayLevel(LevelData level, bool practice)
        {
            CloseAll();
            var go = new GameObject("Game Session");
            go.transform.SetParent(transform, false);
            session = go.AddComponent<GameSession>();
            session.Begin(level, cam, practice, ShowMenu);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
