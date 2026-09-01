#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class AndroidDeviceBuildTool
    {
        private const string ApkPath = "Builds/Android/TinyMonsterKeeper-debug.apk";

        [MenuItem("TinyMonsterKeeper/Automation/Build And Run Android Device")]
        public static void BuildAndRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before building Android.");
                return;
            }

            EditorSceneManager.SaveOpenScenes();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Could not switch the active build target to Android.");
                return;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Android build aborted because Build Settings contains no enabled scenes.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log("Android Build & Run started: " + Path.GetFullPath(ApkPath));
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Android Build & Run succeeded. APK: {Path.GetFullPath(ApkPath)} ({report.summary.totalSize} bytes)");
                return;
            }

            Debug.LogError($"Android Build & Run failed: {report.summary.result}, errors: {report.summary.totalErrors}");
        }
    }
}
#endif
