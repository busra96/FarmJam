using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FarmBoxMergeTutorialChecks
{
    private const string Session = "FarmBoxMerge.TutorialChecks";
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FarmBoxMergeTutorialController _tutorial;
    private static FarmBoxMergeGameController _game;
    private static FarmBoxMergeLevelRuntime _levels;
    private static CardMergeBoard _board;
    private static FarmBoxMergeTutorialProgress _progress;
    private static Card _merged;
    private static string _testKey;
    private static int _stage;
    private static double _nextAction;
    private static double _deadline;
    private static bool _running;
    private static bool _preview;
    private static bool _previewReady;
    private static bool _runInBackground;

    static FarmBoxMergeTutorialChecks()
    {
        _running = SessionState.GetBool(Session, false);
        _preview = SessionState.GetBool(Session + ".Preview", false);
        _testKey = SessionState.GetString(Session + ".Key", "");
        _runInBackground = SessionState.GetBool(Session + ".RunInBackground", false);
        if (_running) { _deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.update += Tick; }
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Session + ".Restore", false))
                EditorApplication.delayCall += Restore;
        };
        if (!_running && !EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(Session + ".Restore", false))
            EditorApplication.delayCall += Restore;
    }

    [MenuItem("Tools/FarmBoxMerge/Tutorial/Run Tutorial Checks")]
    public static void Run() => Start(false);

    [MenuItem("Tools/FarmBoxMerge/Tutorial/Preview First-Time Tutorial")]
    public static void Preview() => Start(true);

    private static void Start(bool preview)
    {
        if (EditorApplication.isCompiling || (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorUtility.DisplayDialog("Tutorial", "Unity is changing Play Mode or compiling. Please try again when it finishes.", "OK");
            return;
        }
        if (!preview && (EditorApplication.isPlaying || _running))
        {
            EditorUtility.DisplayDialog("Tutorial checks", "Stop Play Mode before running automated checks. Preview is available during Play Mode.", "OK");
            return;
        }
        if (_running && !_preview)
        {
            EditorUtility.DisplayDialog("Tutorial", "Automated tutorial checks are still running. Please wait for them to finish.", "OK");
            return;
        }
        if (!EditorApplication.isPlaying && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        bool restartingPreview = _running && _preview;
        if (restartingPreview && !string.IsNullOrEmpty(_testKey))
        {
            _tutorial?.SetSuppressedForSession(true);
            PlayerPrefs.DeleteKey(_testKey + ".TutorialCompleted");
            PlayerPrefs.Save();
        }
        if (!restartingPreview)
        {
            SessionState.SetString(Session + ".Scene", SceneManager.GetActiveScene().path);
            _runInBackground = Application.runInBackground;
            SessionState.SetBool(Session + ".RunInBackground", _runInBackground);
        }
        SessionState.SetBool(Session + ".Restore", true);
        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        _testKey = "FarmBoxMerge.TutorialTest." + Guid.NewGuid().ToString("N");
        SessionState.SetString(Session + ".Key", _testKey);
        SessionState.SetBool(Session + ".Preview", preview);
        SessionState.SetBool(Session, true);
        _preview = preview;
        _previewReady = false;
        _stage = 0;
        _nextAction = 0;
        _running = true;
        _deadline = EditorApplication.timeSinceStartup + 90;
        const string gameplay = "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity";
        if (EditorApplication.isPlaying)
        {
            if (SceneManager.GetActiveScene().path != gameplay)
                SceneManager.LoadScene("FarmBoxMerge");
        }
        else
        {
            if (SceneManager.GetActiveScene().path != gameplay) EditorSceneManager.OpenScene(gameplay);
            EditorApplication.isPlaying = true;
        }
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!_running || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        double now = EditorApplication.timeSinceStartup;
        if (now < _nextAction) return;
        _nextAction = now + 0.1;
        try
        {
            if (now > _deadline && !(_preview && _previewReady)) throw new Exception("Tutorial test timeout at stage " + _stage);
            if (_stage == 0)
            {
                _tutorial = Object.FindFirstObjectByType<FarmBoxMergeTutorialController>();
                _game = Object.FindFirstObjectByType<FarmBoxMergeGameController>();
                _levels = Object.FindFirstObjectByType<FarmBoxMergeLevelRuntime>();
                _board = Object.FindFirstObjectByType<CardMergeBoard>();
                if (_tutorial == null || _game == null || _levels == null || _board == null) return;
                if (!_tutorial.IsInitialized || !Get<bool>(_game, "_initialized") || !Get<bool>(_board, "_initialized")) return;
                Application.runInBackground = true;
                _tutorial.SetSuppressedForSession(true);
                var defaults = ScriptableObject.CreateInstance<FarmBoxMergeSettings>();
                Set(defaults, "<PlayerPrefsPrefix>k__BackingField", _testKey);
                _progress = new FarmBoxMergeTutorialProgress(defaults);
                Object.DestroyImmediate(defaults);
                Set(_tutorial, "_progress", _progress);
                Set(_levels, "useSavedProgress", false);
                Set(_levels, "_currentLevelIndex", 0);
                Object.FindFirstObjectByType<FarmBoxMergeSettingsPanelController>(FindObjectsInactive.Include)?.Close();
                _game.RetryLevel();
                _tutorial.SetSuppressedForSession(false);
                _stage = 1;
                return;
            }
            if (_stage == 1)
            {
                if (_game.IsResetting) return;
                if (_preview && !_previewReady && !_tutorial.IsActive)
                    throw new Exception("The tutorial could not start. Check the Canvas tutorial references in the Console.");
                if (_tutorial.Step != FarmBoxMergeTutorialController.TutorialStep.Merge) return;
                if (_preview)
                {
                    if (!_previewReady)
                    {
                        _previewReady = true;
                        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
                        Debug.Log("[FarmBoxMerge Tutorial] Preview ready on level 1. Stop Play Mode to exit; saved player data is unchanged.");
                    }
                    return;
                }
                Assert(!_progress.Completed, "Incomplete tutorial must not be saved");
                var cards = Cards();
                Assert(cards.Length == 2 && cards.All(c => c.CounterValue == 1), "First level must have two matching 1 cards");
                Assert(!_tutorial.CanDiscard && !_tutorial.CanPlace(cards[0], _board.FindAvailableBoxSlot(2)), "Merge step must block trash and early placement");
                Assert(!Get<UnityEngine.UI.Button>(_game, "addCardButton").interactable
                    && !Get<UnityEngine.UI.Button>(_game, "refreshButton").interactable
                    && !Get<UnityEngine.UI.Button>(_game, "retryButton").interactable, "Unrelated controls must be disabled");
                int count = _board.CardCount;
                _game.AddRecommendedCard();
                Assert(_board.CardCount == count, "Direct add-card call must be blocked");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/FarmBoxMergeTests/tutorial-merge.png"));
                _stage = 9;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 9)
            {
                var cards = Cards();
                var budget = Object.FindFirstObjectByType<FarmBoxMergeActionBudget>();
                int uses = budget.RemainingTrashUses;
                var pointer = Pointer(cards[0]);
                _board.BeginDrag(cards[0], pointer);
                pointer.position = RectTransformUtility.WorldToScreenPoint(_board.EventCamera,
                    Get<RectTransform>(_board, "trashDropLayer").position);
                _board.EndDrag(cards[0], pointer);
                Assert(budget.RemainingTrashUses == uses && !cards[0].IsDragging && !cards[0].IsBusy,
                    "Actual trash drop must restore the card without consuming a use");
                _stage = 10;
                _nextAction = now + 0.4;
                return;
            }
            if (_stage == 10)
            {
                var cards = Cards();
                MergeCards(cards[0], cards[1]);
                _merged = cards[1];
                _stage = 2;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 2)
            {
                Assert(_tutorial.Step == FarmBoxMergeTutorialController.TutorialStep.Place && _merged.CounterValue == 2,
                    "Real merge animation must advance to placement");
                Assert(Get<UnityEngine.UI.Image>(_tutorial, "hand").sprite == Get<Sprite>(_tutorial, "tutorialHand"),
                    "Merge and placement must use the same hand sprite");
                Assert(!_progress.Completed && !_tutorial.CanDiscard, "Completion must wait for placement");
                _game.SetSettingsOpen(true);
                _stage = 3;
                return;
            }
            if (_stage == 3)
            {
                Assert(Get<CanvasGroup>(_tutorial, "overlayGroup").alpha == 0, "Guide must hide while settings is open");
                _game.SetSettingsOpen(false);
                _game.RetryLevel();
                _stage = 4;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 4)
            {
                if (_game.IsResetting) return;
                Assert(_tutorial.Step == FarmBoxMergeTutorialController.TutorialStep.Merge && !_progress.Completed,
                    "An unfinished retry must restart both tutorial steps");
                var cards = Cards();
                // Verify both drag directions are accepted, not just the demonstration direction.
                MergeCards(cards[1], cards[0]);
                _merged = cards[0];
                _stage = 5;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 5)
            {
                Assert(_tutorial.Step == FarmBoxMergeTutorialController.TutorialStep.Place, "Reverse-direction merge must work");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/FarmBoxMergeTests/tutorial-place.png"));
                _stage = 6;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 6)
            {
                var slot = _board.FindAvailableBoxSlot(2);
                Assert(slot != null, "Placement target missing");
                var pointer = Pointer(_merged);
                _board.BeginDrag(_merged, pointer);
                pointer.position = Camera.main.WorldToScreenPoint(slot.transform.position);
                _board.EndDrag(_merged, pointer);
                Assert(_progress.Completed && !_tutorial.IsActive && _board.HasActiveBoxGroups,
                    "Real box placement must save completion and release the tutorial");
                var defaults = ScriptableObject.CreateInstance<FarmBoxMergeSettings>();
                Set(defaults, "<PlayerPrefsPrefix>k__BackingField", _testKey);
                Assert(new FarmBoxMergeTutorialProgress(defaults).Completed, "Completion must survive a new progress service");
                Object.DestroyImmediate(defaults);
                Assert(!Get<RectTransform>(_tutorial, "overlay").gameObject.activeSelf, "Completed overlay must be hidden");
                _game.RetryLevel();
                _stage = 7;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 7)
            {
                if (_game.IsResetting) return;
                Assert(!_tutorial.IsActive && _tutorial.CanDiscard, "Completed tutorial must not return on retry");
                PlayerPrefs.DeleteKey(_testKey + ".TutorialCompleted");
                Set(_levels, "_currentLevelIndex", 6);
                _game.RetryLevel();
                _stage = 8;
                _nextAction = now + 0.5;
                return;
            }
            if (_stage == 8)
            {
                if (_game.IsResetting) return;
                Assert(!_tutorial.IsActive, "Existing players on later levels must not see the tutorial");
                Finish("PASS first-time merge/place, both drag directions, input gates, settings pause, incomplete retry, persistent completion, completed retry, existing-player skip");
            }
        }
        catch (Exception error) { Finish("FAIL " + error); }
    }

    private static Card[] Cards() => _board.CardContainer.GetComponentsInChildren<Card>().Where(c => !c.IsBusy).ToArray();
    private static PointerEventData Pointer(Card card) => new PointerEventData(EventSystem.current)
    {
        position = RectTransformUtility.WorldToScreenPoint(_board.EventCamera, card.RectTransform.position),
        pointerDrag = card.gameObject
    };
    private static void MergeCards(Card source, Card target)
    {
        var pointer = Pointer(source);
        _board.BeginDrag(source, pointer);
        Assert(_board.TryMerge(source, target), "Actual merge request was rejected");
        _board.EndDrag(source, pointer);
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static T Get<T>(object instance, string field) => (T)instance.GetType().GetField(field, Fields).GetValue(instance);
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, Fields).SetValue(instance, value);

    private static void Finish(string result)
    {
        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        File.WriteAllText("Temp/FarmBoxMergeTests/tutorial.txt", result);
        if (result.StartsWith("PASS")) Debug.Log("[FarmBoxMerge Tutorial] " + result);
        else Debug.LogError("[FarmBoxMerge Tutorial] " + result);
        if (_preview && result.StartsWith("FAIL"))
            EditorUtility.DisplayDialog("Tutorial preview", result, "OK");
        _running = false;
        SessionState.SetBool(Session, false);
        EditorApplication.update -= Tick;
        Application.runInBackground = _runInBackground;
        EditorApplication.isPlaying = false;
    }

    private static void Restore()
    {
        _running = false;
        EditorApplication.update -= Tick;
        SessionState.SetBool(Session, false);
        SessionState.SetBool(Session + ".Restore", false);
        Application.runInBackground = SessionState.GetBool(Session + ".RunInBackground", false);
        string key = SessionState.GetString(Session + ".Key", "");
        if (!string.IsNullOrEmpty(key)) { PlayerPrefs.DeleteKey(key + ".TutorialCompleted"); PlayerPrefs.Save(); }
        string scene = SessionState.GetString(Session + ".Scene", "");
        if (!string.IsNullOrEmpty(scene) && scene != SceneManager.GetActiveScene().path) EditorSceneManager.OpenScene(scene);
    }
}
