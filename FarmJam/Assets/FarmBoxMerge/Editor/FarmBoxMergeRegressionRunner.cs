using System;
using System.Collections.Generic;
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
public static class FarmBoxMergeRegressionRunner
{
    private const string SessionKey = "FarmBoxMerge.Regression";
    private const string GameplayScene = "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Results = new List<string>();
    private static readonly Dictionary<FarmBoxMergeBoxSlotView, int> Targets = new Dictionary<FarmBoxMergeBoxSlotView, int>();
    private static readonly HashSet<FarmBoxMergeBoxSlotView> Occupied = new HashSet<FarmBoxMergeBoxSlotView>();
    private static FarmBoxMergeGameController _game;
    private static FarmBoxMergeLevelRuntime _levels;
    private static CardMergeBoard _board;
    private static MergeItemSpawner _items;
    private static FarmBoxMergeOutcomeController _outcome;
    private static FarmBoxMergeBoxSlotView[] _slots;
    private static string _originalScene;
    private static int _index;
    private static int _nextTarget;
    private static int _stage;
    private static double _nextAction;
    private static double _deadline;
    private static float _originalTimeScale = 1f;
    private static bool _running;
    private static bool _originalRunInBackground;
    private static double _edgeStarted;
    private static string _retrySignature;
    private static FarmBoxMergeScreenChecks _screenChecks;
    private static bool _screensOnly;
    public static string Status { get; private set; } = "Not run";

    static FarmBoxMergeRegressionRunner()
    {
        _originalScene = SessionState.GetString(SessionKey + ".Scene", "");
        _running = SessionState.GetBool(SessionKey, false);
        _screensOnly = SessionState.GetBool(SessionKey + ".ScreensOnly", false);
        if (_running)
        {
            Results.AddRange(SessionState.GetString(SessionKey + ".Data", "Data validation passed").Split('\n'));
            _deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update += Update;
        }
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (!_running && SessionState.GetBool(SessionKey + ".Restore", false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += RestoreScene;
    }

    [MenuItem("Tools/FarmBoxMerge/Run Gameplay Regression _F8")]
    public static void Run()
    {
        StartRun(false);
    }

    [MenuItem("Tools/FarmBoxMerge/Repeat Presentation Checks")]
    public static void RepeatPresentationChecks()
    {
        StartRun(true);
    }

    private static void StartRun(bool screensOnly)
    {
        if (_running || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before running gameplay regression.");
            return;
        }
        if (SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("Save your scene before running gameplay regression.");
            return;
        }
        Results.Clear();
        int validated = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:FarmBoxMergeLevelDefinition", new[] { "Assets/FarmBoxMerge" }))
        {
            var level = AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (!FarmBoxMergeSlotPlanBuilder.TryValidateAuthoredPlan(level, out string error))
                throw new InvalidOperationException(level.name + ": " + error);
            ValidateDeckIndependently(level);
            validated++;
        }
        Results.Add("PASS authored plans + independent 12-card deck simulation: " + validated + " levels");
        ValidateSettingsPersistence();
        ValidateRewardCallbacks();
        ValidatePatternsAndFraming();
        if (screensOnly)
        {
            string reportPath = "Temp/FarmBoxMergeTests/regression.txt";
            string[] previous = File.Exists(reportPath) ? File.ReadAllLines(reportPath) : Array.Empty<string>();
            if (previous.Count(line => line.StartsWith("PASS runtime level ")) != validated)
            {
                Debug.LogWarning("Complete the level sweep before repeating presentation checks.");
                return;
            }
            Results.AddRange(previous.Where(line => line.StartsWith("PASS runtime level ")
                || line.StartsWith("PASS runtime settings") || line.StartsWith("PASS exhausted")
                || line.StartsWith("PASS repeated") || line.StartsWith("PASS win after")));
        }
        _screensOnly = screensOnly;
        SessionState.SetBool(SessionKey + ".ScreensOnly", screensOnly);
        _originalScene = SceneManager.GetActiveScene().path;
        SessionState.SetString(SessionKey + ".Data", string.Join("\n", Results));
        SessionState.SetString(SessionKey + ".Scene", _originalScene);
        SessionState.SetBool(SessionKey, true);
        SessionState.SetBool(SessionKey + ".Restore", true);
        _index = 0;
        _stage = 0;
        _running = true;
        Status = "Starting Play Mode";
        EditorSceneManager.OpenScene(GameplayScene);
        EditorApplication.update += Update;
        EditorApplication.isPlaying = true;
        _deadline = EditorApplication.timeSinceStartup + 60;
    }

    [MenuItem("Tools/FarmBoxMerge/Show Regression Status")]
    public static void ShowStatus()
    {
        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        string status = Status + " running=" + _running + " session=" + SessionState.GetBool(SessionKey, false)
            + " playing=" + EditorApplication.isPlaying + " stage=" + _stage + " " + DescribeState();
        File.WriteAllText("Temp/FarmBoxMergeTests/status.txt", status);
        Debug.Log("[FarmBoxMerge Regression] " + status);
    }

    private static void ValidateDeckIndependently(FarmBoxMergeLevelDefinition level)
    {
        var deck = new List<ColorType>();
        if (!FarmBoxMergeCardDeckBuilder.TryBuild(level, deck, out string error)) throw new Exception(error);
        var cards = new List<(ColorType color, int value)>();
        int dealt = 0;
        Action fill = () => { while (cards.Count < 12 && dealt < deck.Count) cards.Add((deck[dealt++], 1)); };
        fill();
        foreach (var entry in level.BoxSlotPlan)
        {
            int steps = 0;
            while (!cards.Any(c => c.color == entry.intendedColor && c.value == entry.boxSize))
            {
                bool merged = false;
                for (int value = entry.boxSize - 1; value >= 1; value--)
                {
                    int a = cards.FindIndex(c => c.color == entry.intendedColor && c.value == value);
                    int b = a < 0 ? -1 : cards.FindIndex(a + 1, c => c.color == entry.intendedColor && c.value == value);
                    if (b < 0) continue;
                    cards[a] = (entry.intendedColor, value + 1);
                    cards.RemoveAt(b);
                    fill();
                    merged = true;
                    break;
                }
                if (!merged || ++steps > 16) throw new Exception(level.name + ": deck deadlock at target " + entry.boxSize);
            }
            cards.RemoveAt(cards.FindIndex(c => c.color == entry.intendedColor && c.value == entry.boxSize));
            fill();
            if (cards.Count > 12) throw new Exception("Card capacity exceeded");
        }
        if (cards.Count != 0 || dealt != deck.Count) throw new Exception(level.name + ": unused authored card resources");
    }

    private static void ValidateSettingsPersistence()
    {
        var defaults = ScriptableObject.CreateInstance<FarmBoxMergeSettings>();
        string prefix = "FarmBoxMerge.Regression." + Guid.NewGuid().ToString("N");
        Set(defaults, "<PlayerPrefsPrefix>k__BackingField", prefix);
        try
        {
            var settings = new FarmBoxMergeSettingsService(defaults);
            settings.SetAudioEnabled(false);
            settings.SetHapticsEnabled(false);
            settings.SetSfxVolume(2);
            settings.SetMusicVolume(-1);
            var reloaded = new FarmBoxMergeSettingsService(defaults);
            if (reloaded.SoundEnabled || reloaded.MusicEnabled || reloaded.HapticsEnabled
                || reloaded.SfxVolume != 1 || reloaded.MusicVolume != 0)
                throw new Exception("Settings persistence/clamping failed");
            Results.Add("PASS isolated settings save/reload, sound/music/haptic, volume bounds");
        }
        finally
        {
            foreach (string key in new[] { "SoundEnabled", "MusicEnabled", "HapticsEnabled", "SfxVolume", "MusicVolume" })
                PlayerPrefs.DeleteKey(prefix + "." + key);
            PlayerPrefs.Save();
            Object.DestroyImmediate(defaults);
        }
    }

    private sealed class QueuedContext : System.Threading.SynchronizationContext
    {
        private readonly Queue<Action> _callbacks = new Queue<Action>();
        public override void Post(System.Threading.SendOrPostCallback callback, object state) => _callbacks.Enqueue(() => callback(state));
        public void Drain() { while (_callbacks.Count > 0) _callbacks.Dequeue()(); }
    }

    private static void ValidateRewardCallbacks()
    {
        var settings = ScriptableObject.CreateInstance<FarmBoxMergeAdsSettings>();
        try
        {
            foreach (string scenario in new[] { "cancel", "early", "complete", "close-before-reward", "disposed" })
            {
                int successes = 0, failures = 0;
                var ads = new FarmBoxMergeLevelPlayAdsService(settings);
                var context = new QueuedContext();
                Set(ads, "_mainThreadContext", context);
                Set(ads, "_pendingRewardedSuccess", (Action)(() => successes++));
                Set(ads, "_pendingRewardedFailure", (Action)(() => failures++));
                Set(ads, "_rewardedShownAt", Time.realtimeSinceStartupAsDouble - (scenario == "early" ? 0 : 6));
                Invoke(ads, "SetAdShowing", true);
                if (scenario == "complete" || scenario == "early") Invoke(ads, "HandleRewardedEarned", null, default(Unity.Services.LevelPlay.LevelPlayReward));
                Invoke(ads, "HandleRewardedClosed", new object[] { null });
                if (scenario == "close-before-reward") Invoke(ads, "HandleRewardedEarned", null, default(Unity.Services.LevelPlay.LevelPlayReward));
                if (scenario == "disposed") ads.Dispose();
                context.Drain();
                context.Drain();
                bool shouldSucceed = scenario == "complete" || scenario == "close-before-reward";
                if (successes != (shouldSucceed ? 1 : 0) || failures != (scenario == "cancel" || scenario == "early" ? 1 : 0) || ads.IsShowingAd)
                    throw new Exception("Reward callback regression: " + scenario);
                ads.Dispose();
            }
            Results.Add("PASS rewarded callbacks: cancel, early close, complete, reversed order, disposal, exactly-once");
        }
        finally { Object.DestroyImmediate(settings); }
    }

    private static void ValidatePatternsAndFraming()
    {
        for (int size = 1; size <= 4; size++)
        for (int variant = 0; variant < 4; variant++)
        {
            var cells = BoxPatternLibrary.ResolveAuthored(size, variant).Cells;
            if (cells.Length != size || cells.Distinct().Count() != size || cells.Max(c => c.x) - cells.Min(c => c.x) > 1)
                throw new Exception("Pattern bounds/duplicate cells invalid");
            var connected = new HashSet<Vector2Int> { cells[0] };
            bool added;
            do { added = false; foreach (var cell in cells) if (!connected.Contains(cell) && connected.Any(c => Mathf.Abs(c.x - cell.x) + Mathf.Abs(c.y - cell.y) == 1)) { connected.Add(cell); added = true; } } while (added);
            if (connected.Count != size) throw new Exception("Disconnected box pattern");
        }
        foreach (var size in new[] { new Vector2Int(1080,1920), new Vector2Int(828,1792), new Vector2Int(1080,2520), new Vector2Int(1768,2208), new Vector2Int(1536,2048), new Vector2Int(1600,2560) })
        {
            float fov = FarmBoxMergeAdaptiveLayout.CalculateVerticalFieldOfView(50, 1080f / 1920, size.x / (float)size.y);
            if (float.IsNaN(fov) || fov < 50 || fov > 88) throw new Exception("FOV invalid: " + size);
        }
        Results.Add("PASS all 16 authored patterns: connected, max 2 wide; 6 phone/tablet framing calculations");
    }

    private static void Update()
    {
        if (!_running) return;
        try
        {
            double now = EditorApplication.timeSinceStartup;
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (now > _deadline) throw new Exception("Timeout: " + Status + " " + DescribeState());
            if (now < _nextAction) return;
            if (_stage == 0)
            {
                _game = Object.FindFirstObjectByType<FarmBoxMergeGameController>();
                Object.FindFirstObjectByType<FarmBoxMergeTutorialController>()?.SetSuppressedForSession(true);
                _levels = Object.FindFirstObjectByType<FarmBoxMergeLevelRuntime>();
                _board = Object.FindFirstObjectByType<CardMergeBoard>();
                _items = Object.FindFirstObjectByType<MergeItemSpawner>();
                _outcome = Object.FindFirstObjectByType<FarmBoxMergeOutcomeController>();
                if (_game == null || _levels == null || _outcome == null || _board == null
                    || !Get<bool>(_game, "_initialized") || !Get<bool>(_board, "_initialized")
                    || !Get<bool>(_outcome, "_initialized")) return;
                _originalTimeScale = Time.timeScale;
                _originalRunInBackground = Application.runInBackground;
                Application.runInBackground = true;
                Time.timeScale = 4;
                Set(_outcome, "winDelay", 0.25f);
                if (_screensOnly)
                {
                    Set(_levels, "_currentLevelIndex", Mathf.Min(24, _levels.Catalog.Count - 1));
                    _game.RetryLevel();
                    _stage = 20;
                    _nextAction = now + 0.5;
                    _deadline = now + 30;
                    Status = "Presentation checks (prior runtime sweep retained)";
                    return;
                }
                _stage = 1;
            }
            if (_stage == 1)
            {
                if (_index >= _levels.Catalog.Count) { _stage = 10; }
            }
            if (_stage >= 10) { TestEdgeCases(now); return; }
            if (_stage == 1)
            {
                if (_slots != null) foreach (var slot in _slots) slot.BecameAvailable -= OnSlotAvailable;
                Set(_levels, "_currentLevelIndex", _index);
                _game.RetryLevel();
                _stage = 2;
                _nextAction = now + 0.3;
                _deadline = now + 45;
                Status = "Level " + (_index + 1) + " retry/solve";
                return;
            }
            if (_game.IsResetting) return;
            if (_stage == 2)
            {
                _slots = Object.FindObjectsByType<FarmBoxMergeBoxSlotView>(FindObjectsSortMode.None)
                    .OrderBy(s => s.transform.GetSiblingIndex()).ToArray();
                Targets.Clear();
                Occupied.Clear();
                _nextTarget = 0;
                foreach (var slot in _slots)
                {
                    slot.BecameAvailable += OnSlotAvailable;
                    Targets[slot] = _nextTarget++;
                    var entry = _levels.CurrentLevel.BoxSlotPlan[Targets[slot] % _levels.CurrentLevel.BoxSlotPlan.Count];
                    if (slot.AcceptedCardValue != entry.boxSize) throw new Exception("Retry silhouette mismatch");
                }
                if (_board.CardCount > 12 || Cards().Any(c => c.CounterValue != 1)) throw new Exception("Initial cards invalid");
                if (_index == 0 || _index == 18 || _index == 24 || _index == 34)
                {
                    Directory.CreateDirectory("Temp/FarmBoxMergeTests");
                    ScreenCapture.CaptureScreenshot("Temp/FarmBoxMergeTests/level-" + (_index + 1) + ".png");
                }
                _stage = 3;
            }
            if (_board.CardCount > 12) throw new Exception("Runtime card capacity exceeded");
            var fail = Get<GameObject>(_outcome, "failPanel");
            if (fail != null && fail.activeSelf) throw new Exception("Unexpected fail on authored solution at level " + (_index + 1));
            var win = Get<GameObject>(_outcome, "winPanel");
            if (win != null && win.activeSelf)
            {
                if (_items.HasRemainingItems || _board.HasActiveBoxGroups || _game.GameplayInputEnabled)
                    throw new Exception("Win/input lock invariant failed");
                Results.Add("PASS runtime level " + (_index + 1) + ": real merge, targeted drag/drop, refill, item jump, clear, win, input lock");
                Debug.Log("[FarmBoxMerge Regression] " + Results[Results.Count - 1]);
                _index++;
                _stage = 1;
                return;
            }
            foreach (var slot in _slots)
            {
                if (slot.IsOccupied) Occupied.Add(slot);
                if (!slot.IsOccupied && !slot.HasRequirement) throw new Exception("Missing silhouette");
            }
            if (!_game.GameplayInputEnabled) return;
            foreach (var slot in _slots.OrderBy(s => Targets[s]))
            {
                int targetIndex = Targets[slot];
                if (slot.IsOccupied || targetIndex >= _levels.CurrentLevel.BoxSlotPlan.Count) continue;
                var target = _levels.CurrentLevel.BoxSlotPlan[targetIndex];
                Card[] cards = Cards();
                Card ready = cards.FirstOrDefault(c => !c.IsBusy && c.CardColorType == target.intendedColor && c.CounterValue == target.boxSize);
                if (ready != null)
                {
                    var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(slot.transform.position) };
                    ready.OnBeginDrag(pointer);
                    ready.OnEndDrag(pointer);
                    if (!slot.IsOccupied)
                    {
                        if (Targets[slot] != targetIndex && ready.MergeCompleted)
                        {
                            _nextAction = now + 0.18;
                            return;
                        }
                        throw new Exception("Targeted UI drop rejected at " + slot.name + " pos=" + pointer.position
                            + " expected=" + target.boxSize + " actual=" + slot.AcceptedCardValue
                            + " card=" + ready.CounterValue + " busy=" + ready.IsBusy
                            + " input=" + _game.GameplayInputEnabled + " " + DescribeState());
                    }
                    var group = slot.GetComponentInChildren<MergeBoxParent>();
                    if (group.ColorType != target.intendedColor || group.CounterValue != target.boxSize) throw new Exception("Wrong slot placement");
                    Occupied.Add(slot);
                    _nextAction = now + 0.18;
                    return;
                }
                for (int value = target.boxSize - 1; value >= 1; value--)
                {
                    var pair = cards.Where(c => !c.IsBusy && c.CardColorType == target.intendedColor && c.CounterValue == value).Take(2).ToArray();
                    if (pair.Length != 2) continue;
                    var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(_board.EventCamera, pair[0].transform.position) };
                    pair[0].OnBeginDrag(pointer);
                    if (!_board.TryMerge(pair[0], pair[1])) throw new Exception("Valid merge rejected");
                    if (_board.TryMerge(pair[0], pair[1])) throw new Exception("Double merge accepted");
                    _nextAction = now + 0.2;
                    return;
                }
            }
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }

    private static Card[] Cards() => Object.FindObjectsByType<Card>(FindObjectsSortMode.None);

    private static void TestEdgeCases(double now)
    {
        if (_stage == 20)
        {
            if (_game.IsResetting || now < _nextAction) return;
            _screenChecks = new FarmBoxMergeScreenChecks(Results);
            _stage = 21;
        }
        if (_stage == 21)
        {
            if (_screenChecks.Step(now))
            {
                _screenChecks.Dispose();
                _screenChecks = null;
                Time.timeScale = _originalTimeScale;
                SceneManager.LoadScene(FarmBoxMergeSceneFlow.MainMenuScene);
                _stage = 30;
                _nextAction = now + 0.8;
                _deadline = now + 30;
                Status = "Main-menu Play transition";
            }
            return;
        }
        if (_stage == 30)
        {
            if (SceneManager.GetActiveScene().name != FarmBoxMergeSceneFlow.MainMenuScene) return;
            var play = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                .FirstOrDefault(button => button.name == "PlayButton");
            if (play == null || !play.interactable || Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length != 1)
                throw new Exception("Main-menu authored Canvas/PlayButton invalid");
            play.onClick.Invoke();
            _stage = 31;
            _nextAction = now + 0.5;
            return;
        }
        if (_stage == 31)
        {
            if (SceneManager.GetActiveScene().name != FarmBoxMergeSceneFlow.GameplayScene) return;
            var game = Object.FindFirstObjectByType<FarmBoxMergeGameController>();
            var board = Object.FindFirstObjectByType<CardMergeBoard>();
            var levels = Object.FindFirstObjectByType<FarmBoxMergeLevelRuntime>();
            if (game == null || !Get<bool>(game, "_initialized")) return;
            if (!game.GameplayInputEnabled || board.CardCount == 0 || board.CardCount > 12
                || levels.CurrentLevelIndex != PlayerPrefs.GetInt("FarmBoxMerge.CurrentLevel", levels.CurrentLevelIndex))
                throw new Exception("Main-menu gameplay/progress restoration invalid");
            Results.Add("PASS authored main-menu Canvas/PlayButton, scene transition, playable saved level without changing progress");
            Finish(null);
            return;
        }
        if (_stage == 10)
        {
            foreach (var slot in _slots) slot.BecameAvailable -= OnSlotAvailable;
            Set(_levels, "_currentLevelIndex", 0);
            _game.RetryLevel();
            _stage = 11;
            _nextAction = now + 0.3;
            _deadline = now + 30;
            Status = "Runtime edge cases";
            return;
        }
        if (_game.IsResetting) return;
        var panel = Object.FindFirstObjectByType<FarmBoxMergeSettingsPanelController>(FindObjectsInactive.Include);
        var budget = Object.FindFirstObjectByType<FarmBoxMergeActionBudget>();
        var spawner = Object.FindFirstObjectByType<CardSpawner>();
        var fail = Get<GameObject>(_outcome, "failPanel");
        var win = Get<GameObject>(_outcome, "winPanel");
        if (_stage == 11)
        {
            _retrySignature = string.Join(",", _board.CardContainer.GetComponentsInChildren<Card>().Select(c => c.CardColorType + ":" + c.CounterValue));
            if (budget.RemainingAddCardUses != 1 || budget.RemainingTrashUses != 1) throw new Exception("Attempt budget not one");
            panel.Open();
            if (_game.GameplayInputEnabled) throw new Exception("Settings did not lock gameplay");
            var settings = Get<IFarmBoxMergeSettingsService>(panel, "_settings");
            if (Get<FarmBoxMergeToggleView>(panel, "soundToggle").IsOn != (settings.SoundEnabled && settings.MusicEnabled)
                || Get<FarmBoxMergeToggleView>(panel, "hapticToggle").IsOn != settings.HapticsEnabled) throw new Exception("Saved toggle state mismatch");
            panel.Close();
            if (!_game.GameplayInputEnabled) throw new Exception("Settings close did not restore input");
            int count = _board.CardCount;
            _game.AddRecommendedCard();
            if (_board.CardCount != count + 1 || budget.RemainingAddCardUses != 0) throw new Exception("Free add failed");
            Card added = Cards().FirstOrDefault(c => c.transform.GetSiblingIndex() == _board.CardContainer.childCount - 1);
            if (added == null || added.CounterValue != 1) throw new Exception("Added card is not level one");
            RectTransform trash = Get<RectTransform>(_board, "trashDropLayer");
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(_board.EventCamera, trash.position) };
            added.OnBeginDrag(pointer);
            added.OnEndDrag(pointer);
            if (budget.RemainingTrashUses != 0) throw new Exception("Free trash failed");
            _stage = 12;
            _nextAction = now + 0.25;
            return;
        }
        if (_stage == 12)
        {
            spawner.ClearCards();
            Set(_outcome, "failDelay", 5f);
            _edgeStarted = now;
            _stage = 13;
            Results.Add("PASS runtime settings lock/restore + saved toggles + one free Add/Trash");
            return;
        }
        if (_stage == 13)
        {
            double elapsed = now - _edgeStarted;
            if (elapsed < 4.5 && fail.activeSelf) throw new Exception("Fail appeared before five seconds");
            if (elapsed < 5.6) { if (elapsed > 3 && !fail.activeSelf && !Get<GameObject>(panel, "settingsPanel").activeSelf) panel.Open(); return; }
            if (!fail.activeSelf || Get<GameObject>(panel, "settingsPanel").activeSelf || _game.GameplayInputEnabled)
                throw new Exception("No-card fail / modal close / input lock failed");
            Results.Add("PASS exhausted-card fail after 5s, settings auto-close, input lock");
            _game.RetryLevel();
            _stage = 14;
            _nextAction = now + 0.3;
            return;
        }
        if (_stage == 14)
        {
            string signature = string.Join(",", _board.CardContainer.GetComponentsInChildren<Card>().Select(c => c.CardColorType + ":" + c.CounterValue));
            if (signature != _retrySignature || budget.RemainingAddCardUses != 1 || budget.RemainingTrashUses != 1)
                throw new Exception("Retry did not reproduce cards/budgets");
            Results.Add("PASS repeated retry reproduces initial card order and resets budgets");
            spawner.ClearCards();
            _items.ClearSpawnedItems();
            _board.ClearSpawnedBoxGroups();
            Set(_outcome, "winDelay", 3f);
            panel.Open();
            _edgeStarted = now;
            _stage = 15;
            return;
        }
        if (_stage == 15)
        {
            double elapsed = now - _edgeStarted;
            if (elapsed < 2.5 && win.activeSelf) throw new Exception("Win appeared before three seconds");
            if (elapsed < 3.6) return;
            if (!win.activeSelf || fail.activeSelf || Get<GameObject>(panel, "settingsPanel").activeSelf || _game.GameplayInputEnabled)
                throw new Exception("Win timing/modal/input lock failed");
            Results.Add("PASS win after 3s (separate fail timer), settings auto-close, post-win input lock");
            Set(_levels, "_currentLevelIndex", Mathf.Min(24, _levels.Catalog.Count - 1));
            _game.RetryLevel();
            _stage = 20;
            _nextAction = now + 0.5;
            _deadline = now + 30;
        }
    }
    private static void OnSlotAvailable(FarmBoxMergeBoxSlotView slot)
    {
        Occupied.Remove(slot);
        Targets[slot] = _nextTarget++;
    }
    private static string DescribeState()
    {
        if (_board == null || _items == null) return "not initialized";
        string front = _items.SpawnedItems.Count > 0 ? _items.SpawnedItems[0].ColorType.ToString() : "none";
        return "front=" + front + " remaining=" + _items.RemainingItemCount
            + " cards=" + string.Join(",", Cards().Select(c => c.CardColorType + ":" + c.CounterValue))
            + " slots=" + string.Join(",", Targets.Select(t => t.Key.name + ":" + t.Value + ":" + t.Key.IsOccupied));
    }
    private static T Get<T>(object instance, string field) => (T)instance.GetType().GetField(field, PrivateInstance).GetValue(instance);
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, PrivateInstance).SetValue(instance, value);
    private static void Invoke(object instance, string method, params object[] arguments) => instance.GetType().GetMethod(method, PrivateInstance).Invoke(instance, arguments);

    private static void Finish(string error)
    {
        _running = false;
        SessionState.SetBool(SessionKey, false);
        _screenChecks?.Dispose();
        _screenChecks = null;
        EditorApplication.update -= Update;
        Time.timeScale = _originalTimeScale;
        Application.runInBackground = _originalRunInBackground;
        if (_slots != null) foreach (var slot in _slots) if (slot != null) slot.BecameAvailable -= OnSlotAvailable;
        Status = error == null
            ? (_screensOnly ? "PASS presentation checks; prior runtime sweep retained" : "PASS all runtime levels and presentation checks")
            : "FAIL " + error;
        Results.Add(Status);
        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        File.WriteAllLines("Temp/FarmBoxMergeTests/regression.txt", Results);
        if (error == null) Debug.Log("[FarmBoxMerge Regression] " + Status);
        else Debug.LogError("[FarmBoxMerge Regression] " + Status);
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SessionKey, false))
        {
            _running = true;
            _deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            return;
        }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (_running) Finish("Test cancelled before completion");
        if (SessionState.GetBool(SessionKey + ".Restore", false)) RestoreScene();
    }

    private static void RestoreScene()
    {
        SessionState.SetBool(SessionKey + ".Restore", false);
        if (!string.IsNullOrEmpty(_originalScene)) EditorSceneManager.OpenScene(_originalScene);
    }
}
