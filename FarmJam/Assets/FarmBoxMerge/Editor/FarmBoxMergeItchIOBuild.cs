using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FarmBoxMergeItchIOBuild
{
    public static string ReleaseRoot
    {
        get
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop) || !Path.IsPathRooted(desktop))
                throw new InvalidOperationException("The operating system Desktop directory is unavailable.");
            return Path.Combine(desktop, "FARMJAMBUILD", "ItchIO-2026-10-15");
        }
    }

    [MenuItem("Tools/FarmBoxMerge/Itch.io/Build Web Release")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new InvalidOperationException("Switch to WebGL first, or use -buildTarget WebGL in batch mode.");
        FarmBoxMergeItchIOChecks.Run();
        FarmBoxMergeEditorRegressionChecks.Run();
        string root = ReleaseRoot;
        string player = Path.Combine(root, "WebGL");
        string zip = Path.Combine(root, "FarmJam-ItchIO-WebGL.zip");
        if (Directory.Exists(player) || File.Exists(zip)) throw new InvalidOperationException("Release already exists. Move the previous release before rebuilding; files will not be overwritten.");
        Directory.CreateDirectory(root);
        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        string template = PlayerSettings.WebGL.template;
        bool threads = PlayerSettings.WebGL.threadsSupport;
        int width = PlayerSettings.defaultWebScreenWidth, height = PlayerSettings.defaultWebScreenHeight;
        bool development = EditorUserBuildSettings.development;
        try
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "PROJECT:FarmJam";
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            EditorUserBuildSettings.development = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FarmBoxMerge/Scenes/FarmBoxMergeMainMenu.unity", "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity" },
                target = BuildTarget.WebGL,
                locationPathName = player,
                extraScriptingDefines = new[] { "FARMBOXMERGE_ITCHIO" },
                options = BuildOptions.None
            });
            File.WriteAllText(Path.Combine(root, "build-summary.txt"),
                $"WebGL: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nDuration: {report.summary.totalTime}\n" +
                "Edition: ad-free, analytics disabled, one Add Card and Trash use per attempt; free Retry/Refresh.\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("WebGL build failed; see build-summary.txt and Unity log.");
            ValidateWebFiles(player);
            ZipFile.CreateFromDirectory(player, zip, System.IO.Compression.CompressionLevel.Optimal, false);
            foreach (string file in Directory.GetFiles("Docs/ItchIO")) File.Copy(file, Path.Combine(root, Path.GetFileName(file)), true);
            FarmBoxMergeItchIOArtwork.Run();
            Debug.Log("[FarmJam Itch.io] Ready: " + zip);
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
            PlayerSettings.WebGL.template = template;
            PlayerSettings.WebGL.threadsSupport = threads;
            PlayerSettings.defaultWebScreenWidth = width;
            PlayerSettings.defaultWebScreenHeight = height;
            EditorUserBuildSettings.development = development;
        }
    }

    public static void ValidateWebFiles(string root)
    {
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(path => new FileInfo(path)).ToArray();
        if (!File.Exists(Path.Combine(root, "index.html")) || files.Length > 1000 || files.Sum(file => file.Length) > 500_000_000 ||
            files.Any(file => file.Length > 200_000_000 || file.FullName.Substring(root.Length).Length > 240))
            throw new InvalidOperationException("WebGL files do not meet itch.io HTML5 archive limits.");
    }
}
