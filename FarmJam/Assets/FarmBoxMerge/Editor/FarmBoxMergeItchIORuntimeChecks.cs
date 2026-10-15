using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FarmBoxMergeItchIORuntimeChecks
{
    private const string Session = "FarmJam.ItchRuntime";
    private static double _started;

    static FarmBoxMergeItchIORuntimeChecks()
    {
        if (SessionState.GetBool(Session, false))
        {
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var settings = AssetDatabase.LoadAssetAtPath<FarmBoxMergeSettings>("Assets/FarmBoxMerge/Config/FarmBoxMergeSettings.asset");
        string[] keys = { "FarmBoxMerge.CurrentLevel", settings.PlayerPrefsPrefix.Trim() + ".Language", FarmBoxMergeTutorialProgress.GetKey(settings) };
        SessionState.SetString(Session + ".Keys", string.Join("|", keys));
        foreach (string key in keys)
        {
            SessionState.SetBool(Session + ".Had." + key, PlayerPrefs.HasKey(key));
            SessionState.SetInt(Session + ".Value." + key, PlayerPrefs.GetInt(key));
        }
        PlayerPrefs.SetInt(keys[0], 0);
        PlayerPrefs.SetInt(keys[1], 0);
        PlayerPrefs.SetInt(keys[2], 1);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/FarmBoxMerge/Scenes/FarmBoxMergeMainMenu.unity");
        SessionState.SetBool(Session, true);
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        FarmBoxMergeRegressionRunner.Run();
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup - _started > 900) { Finish("FAIL runtime regression timed out"); return; }
        string status = FarmBoxMergeRegressionRunner.Status;
        if (status.StartsWith("FAIL")) { Finish(status); return; }
        if (!status.StartsWith("PASS all runtime") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string root = FarmBoxMergeItchIOBuild.ReleaseRoot;
        string screenshots = Path.Combine(root, "Screenshots");
        string qa = Path.Combine(root, "QA");
        Directory.CreateDirectory(screenshots);
        Directory.CreateDirectory(qa);
        foreach (string file in new[] { "level-1.png", "level-19.png", "level-25.png", "level-35.png", "screen-1080x1920.png", "screen-1536x2048.png" })
        {
            string source = Path.Combine("Temp/FarmBoxMergeTests", file);
            if (File.Exists(source)) File.Copy(source, Path.Combine(screenshots, file), true);
        }
        File.Copy("Temp/FarmBoxMergeTests/regression.txt", Path.Combine(qa, "runtime-regression.txt"), true);
        FarmBoxMergeItchIOArtwork.Run();
        Finish(status);
    }

    private static void Finish(string status)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Session, false);
        foreach (string key in SessionState.GetString(Session + ".Keys", "").Split('|'))
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (SessionState.GetBool(Session + ".Had." + key, false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Session + ".Value." + key, 0));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        string qa = Path.Combine(FarmBoxMergeItchIOBuild.ReleaseRoot, "QA");
        Directory.CreateDirectory(qa);
        if (File.Exists("Temp/FarmBoxMergeTests/regression.txt"))
            File.Copy("Temp/FarmBoxMergeTests/regression.txt", Path.Combine(qa, "runtime-regression.txt"), true);
        File.WriteAllText(Path.Combine(qa, "runtime-status.txt"), status);
        Debug.Log("[FarmJam Itch.io Runtime] " + status);
        EditorApplication.Exit(status.StartsWith("PASS") ? 0 : 1);
    }
}
