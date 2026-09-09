using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Picks an audio file with the platform's document picker. On Android this uses the system picker
    /// (scoped storage safe: no storage permission needed) via a tiny Java fragment that copies the chosen
    /// file into the app cache and writes its path to a result file we poll. Elsewhere it reports unavailable
    /// and callers fall back to the in-app file browser.
    /// </summary>
    public static class SongPicker
    {
        public static bool Available => Application.platform == RuntimePlatform.Android;

        static string ResultFile => Path.Combine(Application.temporaryCachePath, "songpicker_result.txt");

        /// <summary>Opens the picker; the callback receives the copied file's path, or an empty string when cancelled.</summary>
        public static void Pick(MonoBehaviour host, Action<string> onPicked)
        {
            if (!Available)
            {
                onPicked?.Invoke("");
                return;
            }
            try
            {
                if (File.Exists(ResultFile)) File.Delete(ResultFile);
                var targetDir = Path.Combine(Application.temporaryCachePath, "songpicker");
                Directory.CreateDirectory(targetDir);
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var picker = new AndroidJavaClass("com.catgenova.lyreflyer.SongPickerFragment"))
                {
                    string dir = targetDir, result = ResultFile;
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() => picker.CallStatic("pick", activity, dir, result)));
                }
#endif
                host.StartCoroutine(WaitForResult(onPicked));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Song picker failed: " + e.Message);
                onPicked?.Invoke("");
            }
        }

        static IEnumerator WaitForResult(Action<string> onPicked)
        {
            float deadline = Time.realtimeSinceStartup + 600f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (File.Exists(ResultFile))
                {
                    string path = "";
                    try
                    {
                        path = File.ReadAllText(ResultFile).Trim();
                        File.Delete(ResultFile);
                    }
                    catch (Exception)
                    {
                    }
                    onPicked?.Invoke(File.Exists(path) ? path : "");
                    yield break;
                }
                yield return null;
            }
            onPicked?.Invoke("");
        }
    }
}
