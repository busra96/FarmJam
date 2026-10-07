using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FarmBoxMergeLocalizationChecks
{
    private const string Session = "FarmBoxMerge.LocalizationChecks";
    private const string Output = "Logs/FarmBoxMergeTests";
    private static bool _running;
    private static int _stage;
    private static double _deadline, _next;
    private static FarmBoxMergeCanvasLocalization _canvas;
    private static FarmBoxMergeSettingsPanelController _panel;
    private static int _initialCards;

    static FarmBoxMergeLocalizationChecks()
    {
        _running = SessionState.GetBool(Session, false);
        if (_running) { _deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.update += Tick; }
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Session + ".Restore", false))
                EditorApplication.delayCall += Restore;
        };
    }

    [MenuItem("Tools/FarmBoxMerge/Localization/Run Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Output);
        try { CheckService(); }
        catch (Exception error) { Finish("FAIL service: " + error); return; }
        var settings = AssetDatabase.LoadAssetAtPath<FarmBoxMergeSettings>("Assets/FarmBoxMerge/Config/FarmBoxMergeSettings.asset");
        string key = settings.PlayerPrefsPrefix.Trim() + ".Language";
        SessionState.SetString(Session + ".Key", key);
        SessionState.SetBool(Session + ".HadKey", PlayerPrefs.HasKey(key));
        SessionState.SetInt(Session + ".Value", PlayerPrefs.GetInt(key));
        SessionState.SetString(Session + ".Scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Session + ".Background", Application.runInBackground);
        SessionState.SetBool(Session + ".Restore", true);
        SessionState.SetBool(Session, true);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity");
        _stage = 0; _running = true; _deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void CheckService()
    {
        var catalog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FarmBoxMergeLocalizationCatalog>(FarmBoxMergeLocalizationInstaller.CatalogPath));
        var settings = ScriptableObject.CreateInstance<FarmBoxMergeSettings>();
        string prefix = "FarmBoxMerge.LocalizationTest." + Guid.NewGuid().ToString("N");
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("<PlayerPrefsPrefix>k__BackingField").stringValue = prefix;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject root = null;
        try
        {
            var service = new FarmBoxMergeLocalizationService(catalog, settings);
            Assert(service.Language == FarmBoxMergeLanguage.English, "Fresh install must default to English");
            int events = 0;
            service.Changed += () => events++;
            foreach (FarmBoxMergeLanguage language in Enum.GetValues(typeof(FarmBoxMergeLanguage)))
            {
                service.SetLanguage(language);
                foreach (var entry in catalog.translations)
                {
                    Assert(!string.IsNullOrWhiteSpace(entry.Get(language)), "Missing " + language + ": " + entry.key);
                    Assert(service.Get(entry.key) == entry.Get(language), "Translation lookup failed");
                    string.Format(CultureInfo.InvariantCulture, service.Get(entry.key), 2, 3);
                }
                Assert(new FarmBoxMergeLocalizationService(catalog, settings).Language == language, "Saved language was not loaded");
            }
            int count = events;
            service.SetLanguage(service.Language);
            Assert(events == count, "Reselecting language must not rebuild UI");
            Assert(service.Get("missing_key") == "missing_key", "Missing key fallback");
            string original = catalog.translations[0].turkish;
            catalog.translations[0].turkish = "";
            service.SetLanguage(FarmBoxMergeLanguage.Turkish);
            Assert(service.Get("play") == "PLAY", "Empty translation must fall back to English");
            catalog.translations[0].turkish = original;
            PlayerPrefs.SetInt(prefix + ".Language", 999);
            Assert(new FarmBoxMergeLocalizationService(catalog, settings).Language == FarmBoxMergeLanguage.English, "Invalid stored language fallback");

            root = new GameObject("TemporaryLocalizationCheck", typeof(RectTransform), typeof(Canvas), typeof(FarmBoxMergeCanvasLocalization));
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            var originalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/FarmBoxMerge/Dependencies/FarmJam/Art/Montserrat/Montserrat-Black SDF.asset");
            if (originalFont == null) originalFont = TMP_Settings.defaultFontAsset;
            text.font = originalFont;
            Material material = text.fontSharedMaterial;
            var alternate = TMP_Settings.defaultFontAsset;
            foreach (var languageFont in catalog.fonts) languageFont.font = null;
            catalog.fonts[(int)FarmBoxMergeLanguage.Turkish].font = alternate;
            service.SetLanguage(FarmBoxMergeLanguage.English);
            var canvas = root.GetComponent<FarmBoxMergeCanvasLocalization>();
            canvas.Initialize(service);
            service.SetLanguage(FarmBoxMergeLanguage.Turkish);
            Assert(text.font == alternate && text.fontSharedMaterial == alternate.material, "Language font assignment");
            service.SetLanguage(FarmBoxMergeLanguage.Spanish);
            Assert(text.font == originalFont && text.fontSharedMaterial == material, "Unassigned language must restore original font/material");
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(catalog); Object.DestroyImmediate(settings);
            PlayerPrefs.DeleteKey(prefix + ".Language"); PlayerPrefs.Save();
        }
    }

    private static void Tick()
    {
        if (!_running || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        double now = EditorApplication.timeSinceStartup;
        if (now < _next) return;
        _next = now + 0.15;
        try
        {
            if (now > _deadline) throw new Exception("Timeout at stage " + _stage);
            if (_stage == 0)
            {
                _canvas = Object.FindFirstObjectByType<FarmBoxMergeCanvasLocalization>();
                _panel = Object.FindFirstObjectByType<FarmBoxMergeSettingsPanelController>(FindObjectsInactive.Include);
                if (_canvas?.Service == null || _panel == null) return;
                var game = Object.FindFirstObjectByType<FarmBoxMergeGameController>();
                if (!Get<bool>(game, "_initialized")) return;
                Application.runInBackground = true;
                Assert(_canvas.Service.Language == FarmBoxMergeLanguage.English, "Runtime default language");
                _initialCards = Object.FindFirstObjectByType<CardMergeBoard>().CardCount;
                _panel.Open(); _stage = 1;
                return;
            }
            if (_stage >= 1 && _stage <= 4)
            {
                var language = (FarmBoxMergeLanguage)(_stage - 1);
                var button = _panel.transform.Find("BG/LanguageSelector/Language_" + language).GetComponent<Button>();
                button.onClick.Invoke();
                Assert(_canvas.Service.Language == language && !button.interactable, "Selector button: " + language);
                Assert(new FarmBoxMergeLocalizationService(
                    AssetDatabase.LoadAssetAtPath<FarmBoxMergeLocalizationCatalog>(FarmBoxMergeLocalizationInstaller.CatalogPath),
                    AssetDatabase.LoadAssetAtPath<FarmBoxMergeSettings>("Assets/FarmBoxMerge/Config/FarmBoxMergeSettings.asset")).Language == language, "Runtime persistence");
                Assert(Object.FindFirstObjectByType<CardMergeBoard>().CardCount == _initialCards, "Language change mutated cards");
                var game = Object.FindFirstObjectByType<FarmBoxMergeGameController>();
                Assert(!game.GameplayInputEnabled, "Settings must keep gameplay locked");
                Assert(_panel.transform.Find("BG/SettingsTitle").GetComponent<TMP_Text>().text == _canvas.Service.Get("settings"), "Settings title");
                var addButton = Get<Button>(game, "addCardButton");
                Assert(FarmBoxMergeRewardedAdBadge.FindPrimaryLabel(addButton.transform).text.StartsWith(_canvas.Service.Get("add_card")), "Dynamic add-card label");
                var presentation = Object.FindFirstObjectByType<FarmBoxMergePresentationController>();
                var levelLabel = Get<TextMeshProUGUI>(presentation, "levelLabel");
                int level = Object.FindFirstObjectByType<FarmBoxMergeLevelRuntime>().CurrentLevelIndex + 1;
                Assert(levelLabel.text == _canvas.Service.Format("level", level), "Dynamic level label");
                foreach (TMP_Text text in _canvas.GetComponentsInChildren<TMP_Text>(true))
                    if (text.name == "AdLabel") Assert(text.text == _canvas.Service.Get("ad"), "Dynamic AD badge");
                Canvas.ForceUpdateCanvases();
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/localization-" + language + ".png"));
                _stage++; _next = now + 0.7;
                return;
            }
            if (_stage == 5)
            {
                _canvas.Service.SetLanguage(FarmBoxMergeLanguage.Turkish);
                _panel.Close();
                Object.FindFirstObjectByType<FarmBoxMergeTutorialController>()?.SetSuppressedForSession(true);
                SceneManager.LoadScene("FarmBoxMergeMainMenu");
                _stage = 6; _next = now + 1;
                return;
            }
            if (_stage == 6)
            {
                var menu = Object.FindFirstObjectByType<FarmBoxMergeMainMenuController>();
                if (menu == null) return;
                var canvas = Object.FindFirstObjectByType<FarmBoxMergeCanvasLocalization>();
                Assert(canvas.Service.Language == FarmBoxMergeLanguage.Turkish, "Main menu language must survive scene transition");
                var button = Get<Button>(menu, "playButton");
                Assert(button.GetComponentInChildren<TMP_Text>().text == "OYNA", "Localized main menu play button");
                Finish("PASS English default, all 4 translation sets/placeholders, persistence, invalid preference/empty text fallback, font/material assignment and restoration, actual settings buttons, live dynamic labels, input/card stability, main-menu scene transition");
            }
        }
        catch (Exception error) { Finish("FAIL " + error); }
    }

    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Finish(string result)
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/localization.txt", result);
        Debug.Log("[FarmBoxMerge Localization] " + result);
        SessionState.SetInt(Session + ".ExitCode", result.StartsWith("PASS") ? 0 : 1);
        _running = false; SessionState.SetBool(Session, false); EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        else Restore();
    }

    private static void Restore()
    {
        _running = false; SessionState.SetBool(Session, false); EditorApplication.update -= Tick;
        if (SessionState.GetBool(Session + ".Restore", false))
        {
            string key = SessionState.GetString(Session + ".Key", "");
            if (SessionState.GetBool(Session + ".HadKey", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Session + ".Value", 0));
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Application.runInBackground = SessionState.GetBool(Session + ".Background", false);
            string scene = SessionState.GetString(Session + ".Scene", "");
            if (!string.IsNullOrEmpty(scene) && scene != SceneManager.GetActiveScene().path) EditorSceneManager.OpenScene(scene);
            SessionState.SetBool(Session + ".Restore", false);
        }
        if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Session + ".ExitCode", 1));
    }
}
