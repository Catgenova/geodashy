using Geodashy.Rendering;
using UnityEngine;

namespace Geodashy.Editing
{
    /// <summary>Drop this on any GameObject in a scene with a camera to boot the level editor. Everything else is built from code.</summary>
    public class GeodashyBootstrap : MonoBehaviour
    {
        [Tooltip("Camera used by the editor and playtests. Uses Camera.main when empty.")]
        public Camera targetCamera;

        void Start()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            if (Mathf.Abs(cam.transform.position.z) < 1f) cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, -10f);

            Application.targetFrameRate = 144;
            var go = new GameObject("Level Editor");
            var editor = go.AddComponent<LevelEditor>();
            editor.Initialize(cam);
        }
    }
}
