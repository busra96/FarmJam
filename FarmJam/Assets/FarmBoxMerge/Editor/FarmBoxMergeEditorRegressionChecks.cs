using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FarmBoxMergeEditorRegressionChecks
{
    [MenuItem("Tools/FarmBoxMerge/Run Font and Build Path Checks")]
    public static void Run()
    {
        string desktopRoot = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "FARMJAMBUILD", "Android"));
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;
        string first = FarmBoxMergeAndroidBuild.GetOutputDirectory();
        string second = FarmBoxMergeAndroidBuild.GetOutputDirectory();
        Assert(Path.IsPathRooted(first), "Build output must be absolute.");
        Assert(string.Equals(Path.GetDirectoryName(first), desktopRoot, StringComparison.OrdinalIgnoreCase),
            "Build output must be inside Desktop/FARMJAMBUILD/Android.");
        Assert(!first.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase), "Build output is inside the project.");
        Assert(first != second, "Each build must have a unique folder to preserve earlier builds.");
        Assert(!Directory.Exists(first) && !Directory.Exists(second), "Querying the build path must not create folders.");

        FarmBoxMergeRoundedFontChecks.Run();
        string report = "PASS Editor compilation, Desktop build output, unique/non-mutating path generation, " +
            "rounded font coverage, repeat rebuild identity, original font preservation and four-language rendering.";
        Directory.CreateDirectory("Logs/FarmBoxMergeTests");
        File.WriteAllText("Logs/FarmBoxMergeTests/editor-regression.txt", report);
        Debug.Log("[FarmBoxMerge Regression] " + report);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
