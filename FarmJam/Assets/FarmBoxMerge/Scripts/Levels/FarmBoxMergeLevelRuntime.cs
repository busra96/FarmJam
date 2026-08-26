using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class FarmBoxMergeLevelRuntime : MonoBehaviour
{
    [Header("Level Source")]
    [SerializeField] private FarmBoxMergeLevelCatalog catalog;
    [SerializeField, Min(0)] private int startLevelIndex;
    [SerializeField] private bool loopAfterLastLevel = true;
    [SerializeField] private bool useSavedProgress;
    [SerializeField] private string progressKey = "FarmBoxMerge.CurrentLevel";

    [Header("Spawners")]
    [SerializeField] private CardSpawner cardSpawner;
    [SerializeField] private MergeItemSpawner itemSpawner;

    private int _currentLevelIndex;
    private bool _initialized;

    public FarmBoxMergeLevelCatalog Catalog => catalog;
    public bool HasLevels => catalog != null && catalog.Count > 0;
    public int CurrentLevelIndex => _currentLevelIndex;
    public FarmBoxMergeLevelDefinition CurrentLevel => HasLevels ? catalog.GetLevel(_currentLevelIndex) : null;

    [Inject]
    public void Construct(CardSpawner injectedCardSpawner, MergeItemSpawner injectedItemSpawner)
    {
        cardSpawner = injectedCardSpawner;
        itemSpawner = injectedItemSpawner;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResolveReferences();
        InitializeLevelIndex();

        if (HasLevels)
        {
            cardSpawner?.SetSpawnOnStart(false);
            itemSpawner?.SetSpawnOnStart(false);
        }
    }

    private void OnValidate()
    {
        startLevelIndex = Mathf.Max(0, startLevelIndex);
        progressKey = string.IsNullOrWhiteSpace(progressKey) ? "FarmBoxMerge.CurrentLevel" : progressKey.Trim();
    }

    public void SpawnCurrentLevel()
    {
        FarmBoxMergeLevelDefinition level = CurrentLevel;
        if (level == null)
        {
            return;
        }

        cardSpawner?.SpawnLevelCards(level.StartingCards);
        itemSpawner?.SpawnLevelItems(level.ItemSequence);
    }

    public bool MoveNext()
    {
        if (!HasLevels)
        {
            return false;
        }

        int nextIndex = _currentLevelIndex + 1;
        if (nextIndex >= catalog.Count && !loopAfterLastLevel)
        {
            return false;
        }

        _currentLevelIndex = catalog.NormalizeIndex(nextIndex, loopAfterLastLevel);
        SaveProgress();
        return true;
    }

    [ContextMenu("Reset Saved Progress")]
    public void ResetSavedProgress()
    {
        if (!string.IsNullOrWhiteSpace(progressKey))
        {
            PlayerPrefs.DeleteKey(progressKey);
        }

        _currentLevelIndex = HasLevels ? catalog.NormalizeIndex(startLevelIndex, loopAfterLastLevel) : 0;
    }

    private void InitializeLevelIndex()
    {
        int requestedIndex = useSavedProgress
            ? PlayerPrefs.GetInt(progressKey, startLevelIndex)
            : startLevelIndex;
        _currentLevelIndex = HasLevels ? catalog.NormalizeIndex(requestedIndex, loopAfterLastLevel) : 0;
    }

    private void SaveProgress()
    {
        if (!useSavedProgress || string.IsNullOrWhiteSpace(progressKey))
        {
            return;
        }

        PlayerPrefs.SetInt(progressKey, _currentLevelIndex);
        PlayerPrefs.Save();
    }

    private void ResolveReferences()
    {
        cardSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardSpawner>();
        itemSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<MergeItemSpawner>();
    }
}
