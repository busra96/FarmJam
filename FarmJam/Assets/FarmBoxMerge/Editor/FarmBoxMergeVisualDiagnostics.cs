using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-only QA helpers used while reviewing visuals and level design.
/// Level jumps restore the player's saved level index when Play Mode ends.
/// </summary>
[InitializeOnLoad]
public static class FarmBoxMergeVisualDiagnostics
{
    private const string GameplayScene = "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity";
    private const string ProgressKey = "FarmBoxMerge.CurrentLevel";
    private const string SavedKey = "FBM.Debug.SavedProgress";
    private const string HasSavedKey = "FBM.Debug.HasSavedProgress";
    private static int _shot;

    static FarmBoxMergeVisualDiagnostics()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(HasSavedKey, false))
            {
                int saved = SessionState.GetInt(SavedKey, 0);
                if (saved < 0) PlayerPrefs.DeleteKey(ProgressKey);
                else PlayerPrefs.SetInt(ProgressKey, saved);
                PlayerPrefs.Save();
                SessionState.SetBool(HasSavedKey, false);
                Debug.Log("FBM_DEBUG progress restored to " + saved);
            }
        };
    }

    [MenuItem("Tools/FarmBoxMerge/Debug/Play Gameplay Scene %#&p")]
    public static void PlayGameplay()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/FarmBoxMerge/Debug/Next Level %#&n")]
    public static void NextLevel() => Jump(+1);

    [MenuItem("Tools/FarmBoxMerge/Debug/Previous Level %#&b")]
    public static void PreviousLevel() => Jump(-1);

    [MenuItem("Tools/FarmBoxMerge/Debug/Jump +5 Levels")]
    public static void Next5() => Jump(+5);

    private static void Jump(int delta)
    {
        if (!Application.isPlaying) return;
        var runtime = Object.FindFirstObjectByType<FarmBoxMergeLevelRuntime>();
        var game = Object.FindFirstObjectByType<FarmBoxMergeGameController>();
        if (runtime == null || game == null || !runtime.HasLevels) return;
        if (!SessionState.GetBool(HasSavedKey, false))
        {
            SessionState.SetInt(SavedKey, PlayerPrefs.HasKey(ProgressKey) ? PlayerPrefs.GetInt(ProgressKey) : -1);
            SessionState.SetBool(HasSavedKey, true);
        }

        int count = runtime.Catalog.Count;
        int target = ((runtime.CurrentLevelIndex + delta) % count + count) % count;
        FieldInfo field = typeof(FarmBoxMergeLevelRuntime).GetField("_currentLevelIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(runtime, target);
        game.RetryLevel();
        Debug.Log("FBM_DEBUG jumped to level " + (target + 1));
    }

    [MenuItem("Tools/FarmBoxMerge/Debug/Preview Win Panel %#&w")]
    public static void PreviewWin() => PreviewOutcome("Win");

    [MenuItem("Tools/FarmBoxMerge/Debug/Preview Fail Panel %#&f")]
    public static void PreviewFail() => PreviewOutcome("Fail");

    private static void PreviewOutcome(string outcome)
    {
        if (!Application.isPlaying) return;
        var controller = Object.FindFirstObjectByType<FarmBoxMergeOutcomeController>();
        if (controller == null) return;
        System.Type enumType = typeof(FarmBoxMergeOutcomeController).GetNestedType("PendingOutcome", BindingFlags.NonPublic);
        MethodInfo show = typeof(FarmBoxMergeOutcomeController).GetMethod("ShowOutcome", BindingFlags.NonPublic | BindingFlags.Instance);
        if (enumType == null || show == null) return;
        show.Invoke(controller, new[] { System.Enum.Parse(enumType, outcome) });
        Debug.Log("FBM_DEBUG previewing " + outcome + " panel (progress not changed)");
    }

    [MenuItem("Tools/FarmBoxMerge/Debug/Capture Game View %#&c")]
    public static void Capture()
    {
        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        string path = "Temp/FarmBoxMergeTests/shot-" + (++_shot) + ".png";
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("FBM_DEBUG captured " + path);
    }

    [MenuItem("Tools/FarmBoxMerge/Debug/Dump Visual State %#&d")]
    public static void Dump()
    {
        StringBuilder sb = new StringBuilder();
        Camera cam = Camera.main;
        sb.AppendLine("playing=" + Application.isPlaying + " cam=" + (cam != null ? cam.name + " pos=" + cam.transform.position + " rot=" + cam.transform.eulerAngles + " fov=" + cam.fieldOfView + " mask=" + cam.cullingMask : "null"));
        foreach (FarmBoxMergeSlotBadge badge in Object.FindObjectsByType<FarmBoxMergeSlotBadge>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform t = badge.transform;
            sb.AppendLine("slot " + t.name + " pos=" + t.position + " lossy=" + t.lossyScale + " rot=" + t.eulerAngles + " children=" + t.childCount);
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                Vector3 sp = cam != null ? cam.WorldToScreenPoint(c.position) : Vector3.zero;
                string extra = "";
                TextMeshPro tmp = c.GetComponentInChildren<TextMeshPro>(true);
                if (tmp != null) extra += " tmp='" + tmp.text + "' font=" + (tmp.font != null ? tmp.font.name : "null") + " size=" + tmp.fontSize + " bounds=" + tmp.bounds.size + " layer=" + tmp.gameObject.layer;
                SpriteRenderer sr = c.GetComponent<SpriteRenderer>();
                if (sr != null) extra += " sprite=" + (sr.sprite != null ? sr.sprite.name : "null") + " mat=" + (sr.sharedMaterial != null ? sr.sharedMaterial.shader.name : "null") + " col=" + sr.color + " scale=" + c.localScale + " b=" + sr.bounds.size;
                Renderer[] rs = c.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine("   child " + c.name + " active=" + c.gameObject.activeInHierarchy + " world=" + c.position + " screen=" + sp + " renderers=" + rs.Length + extra);
            }
        }

        foreach (FarmBoxMergeCardDecor decor in Object.FindObjectsByType<FarmBoxMergeCardDecor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            sb.AppendLine("card " + decor.name + " children=" + decor.transform.childCount);
        }

        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        File.WriteAllText("Temp/FarmBoxMergeTests/visual-state.txt", sb.ToString());
        Debug.Log("FBM_VISUAL_DUMP written");
    }
}
