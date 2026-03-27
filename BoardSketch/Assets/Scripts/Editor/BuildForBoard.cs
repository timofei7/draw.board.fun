using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;

namespace BoardSketch.Editor
{
    public static class BuildForBoard
    {
        [MenuItem("BoardSketch/Configure for Board")]
        public static void ConfigureProject()
        {
            // Switch to Android
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Build] Switching to Android platform...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Player settings per Board SDK requirements
            PlayerSettings.companyName = "BoardSketch";
            PlayerSettings.productName = "BoardSketch";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.boardsketch.app");
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Android target: API 33, ARM64, IL2CPP
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Landscape Left only (Board is landscape)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Unity 6: Application entry point must be Activity (not GameActivity)
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;

            // Add scene to build settings
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/BoardSketchScene.unity/BoardSketchScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes;

            Debug.Log("[Build] Project configured for Board! Run BoardSketch/Build APK to build.");
        }

        [MenuItem("BoardSketch/Build APK")]
        public static void BuildAPK()
        {
            // Ensure configured
            ConfigureProject();

            string buildDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
            if (!Directory.Exists(buildDir))
                Directory.CreateDirectory(buildDir);

            string apkPath = Path.Combine(buildDir, "BoardSketch.apk");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/BoardSketchScene.unity/BoardSketchScene.unity" },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log("[Build] Building APK to " + apkPath + "...");
            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log("[Build] APK built successfully: " + apkPath + " (" + (report.summary.totalSize / 1024 / 1024) + " MB)");
            else
                Debug.LogError("[Build] Build failed: " + report.summary.result);
        }
    }
}
