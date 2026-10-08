using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FarmBoxMergeAndroidBuild
{
    [MenuItem("Tools/FarmBoxMerge/Build Android APK and AAB")]
    public static void BuildApkAndBundle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before building.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Switch the active build target to Android first. For batch builds, use -buildTarget Android.");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length != 2 || scenes[0] != "Assets/FarmBoxMerge/Scenes/FarmBoxMergeMainMenu.unity" ||
            scenes[1] != "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity" || scenes.Any(scene => !File.Exists(scene)))
            throw new InvalidOperationException("Build scenes must be FarmBoxMergeMainMenu followed by FarmBoxMerge.");

        string folder = GetOutputDirectory();
        Directory.CreateDirectory(folder);
        string version = string.Concat(PlayerSettings.bundleVersion.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '-'));
        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        bool previousDevelopment = EditorUserBuildSettings.development;
        var summary = new StringBuilder();
        summary.AppendLine("FarmBoxMerge Android build");
        summary.AppendLine("Application ID: " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        summary.AppendLine("Version: " + PlayerSettings.bundleVersion + " (" + PlayerSettings.Android.bundleVersionCode + ")");
        summary.AppendLine("Architectures: " + PlayerSettings.Android.targetArchitectures);
        summary.AppendLine("Signing: " + (PlayerSettings.Android.useCustomKeystore ? "Configured custom keystore" : "Unity debug/test keystore; NOT a Google Play upload release"));
        var ads = AssetDatabase.LoadAssetAtPath<FarmBoxMergeAdsSettings>("Assets/FarmBoxMerge/Config/FarmBoxMergeAdsSettings.asset");
        summary.AppendLine("Android ad configuration: " + (ads != null && ads.HasAndroidConfiguration ? "Present" : "Missing; device rewarded/interstitial ads unavailable"));
        string reportPath = Path.Combine(folder, "build-summary.txt");
        try
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            foreach (bool bundle in new[] { false, true })
            {
                EditorUserBuildSettings.buildAppBundle = bundle;
                string path = Path.Combine(folder, "FarmBoxMerge-" + version + (bundle ? ".aab" : ".apk"));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = BuildTarget.Android,
                    locationPathName = path,
                    options = BuildOptions.None
                });
                long artifactSize = File.Exists(path) ? new FileInfo(path).Length : 0;
                summary.AppendLine($"{Path.GetFileName(path)}: {report.summary.result}; artifact {artifactSize} bytes; " +
                    $"build-report size {report.summary.totalSize} bytes; " +
                    $"{report.summary.totalErrors} errors; {report.summary.totalWarnings} warnings; {report.summary.totalTime}");
                File.WriteAllText(reportPath, summary.ToString());
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(path))
                    throw new InvalidOperationException("Android build failed. See " + reportPath + " and the Unity build log.");
                Debug.Log("[FarmBoxMerge Build] Created " + path);
            }
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport;
            EditorUserBuildSettings.development = previousDevelopment;
            File.WriteAllText(reportPath, summary.ToString());
        }
    }

    public static string GetOutputDirectory()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
            throw new InvalidOperationException("Desktop folder could not be resolved. No build was started.");
        return Path.GetFullPath(Path.Combine(desktop, "FARMJAMBUILD", "Android", "LocalizationUpdate-" +
            DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6)));
    }
}
