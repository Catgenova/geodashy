using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Geodashy.EditorTools
{
    /// <summary>
    /// Player builds. Use the Geodashy menu inside Unity, or from the command line:
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod Geodashy.EditorTools.BuildScript.BuildAndroid
    /// Optional environment variables: BUILD_NUMBER (Android versionCode), BUILD_OUTPUT (apk path),
    /// DEVELOPMENT_BUILD=1 for a development build with the profiler and script debugging enabled.
    /// </summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Geodashy/Scenes/LevelEditor.unity";
        const string DefaultApk = "Builds/Android/LyreFlyer.apk";

        [MenuItem("Geodashy/Build Android APK")]
        public static void BuildAndroidMenu()
        {
            var path = EditorUtility.SaveFilePanel("Build LyreFlyer APK", Path.GetDirectoryName(Path.GetFullPath(DefaultApk)), "LyreFlyer", "apk");
            if (string.IsNullOrEmpty(path)) return;
            var report = BuildAndroidTo(path, false);
            if (report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Geodashy/Build Android APK (Development)")]
        public static void BuildAndroidDevMenu()
        {
            var path = EditorUtility.SaveFilePanel("Build LyreFlyer development APK", Path.GetDirectoryName(Path.GetFullPath(DefaultApk)), "LyreFlyer-dev", "apk");
            if (string.IsNullOrEmpty(path)) return;
            BuildAndroidTo(path, true);
        }

        /// <summary>Batch-mode entry point (see the class comment). Exits with code 1 on failure.</summary>
        public static void BuildAndroid()
        {
            var output = Environment.GetEnvironmentVariable("BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = DefaultApk;
            bool dev = Environment.GetEnvironmentVariable("DEVELOPMENT_BUILD") == "1";
            var report = BuildAndroidTo(output, dev);
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        public static BuildReport BuildAndroidTo(string apkPath, bool development)
        {
            ApplyAndroidSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(apkPath)));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log("LyreFlyer: built " + apkPath + " (" + (s.totalSize / (1024 * 1024)) + " MB) in " + s.totalTime.TotalSeconds.ToString("0") + " s");
            else
                Debug.LogError("LyreFlyer: Android build " + s.result + " with " + s.totalErrors + " error(s)");
            return report;
        }

        /// <summary>Settings every APK build needs regardless of what the editor UI currently says.</summary>
        public static void ApplyAndroidSettings()
        {
            EditorUserBuildSettings.buildAppBundle = false;   // .apk, not .aab
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)) || PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android).Contains("DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.catgenova.lyreflyer");
            var number = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            if (int.TryParse(number, out int code) && code > 0) PlayerSettings.Android.bundleVersionCode = code;
            // no custom keystore configured: Unity signs with its debug key, which installs fine for sideloading
            PlayerSettings.Android.useCustomKeystore = !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName);
        }
    }
}
