using Geodashy.Core;
using Geodashy.Editing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Geodashy.EditorTools
{
    /// <summary>Unity Editor menu helpers for the Geodashy level editor.</summary>
    public static class GeodashyMenu
    {
        const string ScenePath = "Assets/Geodashy/Scenes/LevelEditor.unity";

        [MenuItem("Geodashy/Open Level Editor Scene")]
        public static void OpenScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                RegenerateScene();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Geodashy/Play Level Editor _F5")]
        public static void PlayEditor()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            OpenScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Geodashy/Regenerate Level Editor Scene")]
        public static void RegenerateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.2f, 0.36f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
            var boot = new GameObject("Geodashy Bootstrap");
            var b = boot.AddComponent<GeodashyBootstrap>();
            b.targetCamera = cam;
            System.IO.Directory.CreateDirectory("Assets/Geodashy/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            Debug.Log("Geodashy: level editor scene regenerated at " + ScenePath);
        }

        [MenuItem("Geodashy/Add Editor Scene To Build Settings")]
        public static void AddSceneToBuild()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes) if (s.path == ScenePath) return;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Geodashy/Open Saved Levels Folder")]
        public static void OpenLevelsFolder()
        {
            EditorUtility.RevealInFinder(LevelStorage.LevelsDirectory);
        }

        [MenuItem("Geodashy/Log Object Catalog")]
        public static void LogCatalog()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Geodashy object catalog (" + ObjectCatalog.All.Count + " objects)");
            foreach (var cat in ObjectCatalog.Categories)
            {
                sb.AppendLine("== " + cat);
                foreach (var d in ObjectCatalog.InCategory(cat)) sb.AppendLine("  " + d.id + "  (" + d.name + ", " + d.width + "x" + d.height + ")");
            }
            Debug.Log(sb.ToString());
        }
    }
}
