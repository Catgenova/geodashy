using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Short vibrations on handhelds (Android Vibrator). No-op elsewhere. Toggle in Options.</summary>
    public static class Haptics
    {
        const string Pref = "geodashy.haptics";
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static bool resolved;
#endif

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Pref, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Pref, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool Supported => Application.platform == RuntimePlatform.Android;

        /// <summary>A short pulse; milliseconds are clamped to 5..80 so it never feels like a phone call.</summary>
        public static void Tap(int milliseconds = 12)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!resolved)
                {
                    resolved = true;
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (vibrator == null) return;
                long ms = Mathf.Clamp(milliseconds, 5, 80);
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, -1))
                    vibrator.Call("vibrate", effect);
            }
            catch (System.Exception)
            {
                vibrator = null;
            }
#endif
        }

        public static void Jump() => Tap(10);
        public static void Death() => Tap(45);
        public static void Waystone() => Tap(20);
        public static void Place() => Tap(8);
    }
}
