using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        Debug.Log("[BuildScript] Starting Android APK build for 'Starbound : Maze Runner'...");

        PlayerSettings.productName = "Starbound : Maze Runner";

        string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string apkPath = Path.Combine(buildDir, "Starbound - Maze Runner.apk");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/PuzzleSolver.unity" },
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] BUILD_SUCCESS: Android APK generated ({summary.totalSize} bytes) at: {apkPath}");
            try
            {
                string compatPath = Path.Combine(buildDir, "MazeRunner.apk");
                File.Copy(apkPath, compatPath, true);
                Debug.Log($"[BuildScript] Copied compatibility binary to: {compatPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildScript] Could not copy compat binary: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"[BuildScript] BUILD_FAILED: Result={summary.result}, Errors={summary.totalErrors}, Warnings={summary.totalWarnings}");
        }
    }
}

