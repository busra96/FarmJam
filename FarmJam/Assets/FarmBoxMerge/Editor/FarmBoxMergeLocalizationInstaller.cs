using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FarmBoxMergeLocalizationInstaller
{
    public const string CatalogPath = "Assets/FarmBoxMerge/Config/FarmBoxMergeLocalizationCatalog.asset";
    private const string SettingsPath = "Assets/FarmBoxMerge/Config/FarmBoxMergeSettings.asset";

    [MenuItem("Tools/FarmBoxMerge/Localization/Install or Update Scene Bindings")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var settings = AssetDatabase.LoadAssetAtPath<FarmBoxMergeSettings>(SettingsPath);
        var catalog = AssetDatabase.LoadAssetAtPath<FarmBoxMergeLocalizationCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<FarmBoxMergeLocalizationCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var config = new SerializedObject(settings);
        config.FindProperty("<Localization>k__BackingField").objectReferenceValue = catalog;
        config.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        string previous = SceneManager.GetActiveScene().path;
        try
        {
            foreach (string sceneName in new[] { "FarmBoxMerge", "FarmBoxMergeMainMenu" })
            {
                string path = "Assets/FarmBoxMerge/Scenes/" + sceneName + ".unity";
                Scene scene = SceneManager.GetActiveScene().path == path ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(path);
                Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
                if (canvas == null) throw new System.InvalidOperationException("Authored Canvas missing: " + sceneName);
                var localization = canvas.GetComponent<FarmBoxMergeCanvasLocalization>();
                if (localization == null) localization = Undo.AddComponent<FarmBoxMergeCanvasLocalization>(canvas.gameObject);
                if (sceneName == "FarmBoxMerge") InstallSelector(canvas);
                else
                {
                    var menu = Object.FindFirstObjectByType<FarmBoxMergeMainMenuController>();
                    var serialized = new SerializedObject(menu);
                    serialized.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FarmBoxMergeSettings>(SettingsPath);
                    serialized.FindProperty("localization").objectReferenceValue = localization;
                    serialized.ApplyModifiedProperties();
                }
                BindStaticText(localization);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[FarmBoxMerge Localization] Installed four-language settings UI, catalog and both scene bindings.");
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous) && previous != SceneManager.GetActiveScene().path)
                EditorSceneManager.OpenScene(previous);
        }
    }

    private static void InstallSelector(Canvas canvas)
    {
        var panel = canvas.GetComponentInChildren<FarmBoxMergeSettingsPanelController>(true);
        Transform background = panel.transform.Find("BG");
        var rect = (RectTransform)background;
        rect.sizeDelta = new Vector2(400f, 520f);
        SetTopPosition(background.Find("Sound-Toggle-Panel") as RectTransform, new Vector2(200f, -100f));
        SetTopPosition(background.Find("Haptic-Toggle-Panel") as RectTransform, new Vector2(200f, -215f));
        var cancel = new SerializedObject(panel).FindProperty("cancelButton").objectReferenceValue as Button;
        if (cancel != null)
        {
            RectTransform cancelRect = (RectTransform)cancel.transform;
            cancelRect.anchorMin = cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(0f, 36f);
        }
        TMP_FontAsset font = background.GetComponentInChildren<TextMeshProUGUI>(true).font;
        var title = Label("SettingsTitle", background, font);
        SetTopPosition(title.rectTransform, new Vector2(200f, -30f));
        title.rectTransform.sizeDelta = new Vector2(350f, 40f);
        title.text = "SETTINGS";
        title.transform.SetAsFirstSibling();
        var root = Rect("LanguageSelector", background);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -353f);
        root.sizeDelta = new Vector2(360f, 154f);
        if (cancel != null) cancel.transform.SetAsLastSibling();
        var languageTitle = Label("LanguageLabel", root, font);
        languageTitle.rectTransform.anchoredPosition = new Vector2(0f, 62f);
        languageTitle.rectTransform.sizeDelta = new Vector2(350f, 32f);
        languageTitle.text = "LANGUAGE";
        var selector = root.GetComponent<FarmBoxMergeLanguageSelector>();
        if (selector == null) selector = Undo.AddComponent<FarmBoxMergeLanguageSelector>(root.gameObject);
        var serialized = new SerializedObject(selector);
        var choices = serialized.FindProperty("choices");
        choices.arraySize = 4;
        string[] names = { "English", "Türkçe", "Español", "Chinese" };
        Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FarmBoxMerge/Visuals/UI/RoundedPanel.png");
        for (int i = 0; i < 4; i++)
        {
            var buttonRect = Rect("Language_" + (FarmBoxMergeLanguage)i, root);
            buttonRect.sizeDelta = new Vector2(171f, 44f);
            buttonRect.anchoredPosition = new Vector2(i % 2 == 0 ? -91f : 91f, i < 2 ? 14f : -40f);
            var image = buttonRect.GetComponent<Image>();
            if (image == null) image = Undo.AddComponent<Image>(buttonRect.gameObject);
            image.sprite = rounded; image.type = Image.Type.Sliced; image.color = new Color(0.24f, 0.57f, 0.31f);
            var button = buttonRect.GetComponent<Button>();
            if (button == null) button = Undo.AddComponent<Button>(buttonRect.gameObject);
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = Color.white; button.colors = colors;
            var text = Label("Label", buttonRect, font);
            text.text = names[i]; text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(155f, 40f);
            choices.GetArrayElementAtIndex(i).FindPropertyRelative("language").enumValueIndex = i;
            choices.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue = button;
        }
        serialized.ApplyModifiedProperties();
        var panelSerialized = new SerializedObject(panel);
        panelSerialized.FindProperty("languageSelector").objectReferenceValue = selector;
        panelSerialized.ApplyModifiedProperties();
    }

    private static void BindStaticText(FarmBoxMergeCanvasLocalization localization)
    {
        var keys = new Dictionary<string, string>
        {
            ["PLAY"] = "play", ["SETTINGS"] = "settings", ["SOUND"] = "sound", ["VIBRATION"] = "haptics",
            ["CANCEL"] = "cancel", ["CLOSE"] = "cancel", ["LANGUAGE"] = "language", ["NEXT LEVEL"] = "next_level",
            ["RETRY LEVEL"] = "retry_level", ["HARVEST COMPLETE!"] = "win", ["FAIL!"] = "fail", ["TRY AGAIN!"] = "fail",
            ["ITEMS\nLEFT"] = "items_left", ["GREEN"] = "green", ["ORANGE"] = "orange", ["PURPLE"] = "purple",
            ["RED"] = "red", ["YELLOW"] = "yellow", ["AD"] = "ad"
        };
        var existing = new SerializedObject(localization);
        var bindings = existing.FindProperty("bindings");
        // Preserve authored key assignments when the installer is rerun after a translation edit.
        var texts = new Dictionary<TMP_Text, string>();
        for (int i = 0; i < bindings.arraySize; i++)
        {
            var binding = bindings.GetArrayElementAtIndex(i);
            var text = binding.FindPropertyRelative("text").objectReferenceValue as TMP_Text;
            if (text != null) texts[text] = binding.FindPropertyRelative("key").stringValue;
        }
        foreach (TMP_Text text in localization.GetComponentsInChildren<TMP_Text>(true))
        {
            string source = text.text.Trim().Replace("\r\n", "\n");
            if (keys.TryGetValue(source, out string key)) texts[text] = key;
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(text.fontSizeMin, text.fontSize * 0.45f);
            EditorUtility.SetDirty(text);
        }
        bindings.arraySize = texts.Count;
        int index = 0;
        foreach (var entry in texts)
        {
            var binding = bindings.GetArrayElementAtIndex(index++);
            binding.FindPropertyRelative("text").objectReferenceValue = entry.Key;
            binding.FindPropertyRelative("key").stringValue = entry.Value;
        }
        existing.ApplyModifiedProperties();
    }

    private static void SetTopPosition(RectTransform rect, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        if (parent.Find(name) is RectTransform existing) return existing;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Install localization UI");
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }
    private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font)
    {
        var rect = Rect(name, parent);
        var label = rect.GetComponent<TextMeshProUGUI>();
        if (label == null) label = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        label.font = font; label.fontSize = 22f; label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.25f, 0.21f, 0.16f); label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
}
