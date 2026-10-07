using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

[DisallowMultipleComponent]
public class FarmBoxMergeGameController : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private CardSpawner cardSpawner;
    [SerializeField] private CardMergeBoard cardMergeBoard;
    [SerializeField] private MergeItemSpawner itemSpawner;
    [SerializeField] private FarmBoxMergeActionBudget actionBudget;
    [SerializeField] private FarmBoxMergeLevelRuntime levelRuntime;

    [Header("Reset UI")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button addCardButton;
    [SerializeField] private string refreshLabel = "REFRESH";
    [SerializeField] private string retryLabel = "RETRY";
    [SerializeField] private string addCardLabel = "ADD CARD";
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.67f, 0.38f, 1f);
    [SerializeField] private Color retryButtonColor = new Color(0.95f, 0.55f, 0.18f, 1f);
    [SerializeField] private Color addCardButtonColor = new Color(0.2f, 0.55f, 0.9f, 1f);

    private Coroutine _resetRoutine;
    private IFarmBoxMergeAdsService _ads;
    private bool _gameplayInputEnabled = true;
    private bool _settingsOpen;
    private bool _tutorialActive;
    private bool _levelTransitionPending;
    private bool _initialized;

    public bool IsResetting => _resetRoutine != null;
    public bool GameplayInputEnabled => _gameplayInputEnabled && !_settingsOpen && !IsResetting && !IsAdInProgress;
    public bool IsAdInProgress => _ads != null && _ads.IsShowingAd;
    public event Action AttemptResetStarted;
    public event Action AttemptReady;
    public event Action<bool> GameplayInputChanged;

    [Inject]
    public void Construct(
        CardSpawner injectedCardSpawner,
        CardMergeBoard injectedCardMergeBoard,
        MergeItemSpawner injectedItemSpawner,
        FarmBoxMergeActionBudget injectedActionBudget,
        FarmBoxMergeLevelRuntime injectedLevelRuntime,
        IFarmBoxMergeAdsService ads)
    {
        cardSpawner = injectedCardSpawner;
        cardMergeBoard = injectedCardMergeBoard;
        itemSpawner = injectedItemSpawner;
        actionBudget = injectedActionBudget;
        levelRuntime = injectedLevelRuntime;
        _ads = ads;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResolveReferences();
        ConfigureButton(refreshButton, RequestRefreshWithAd, refreshLabel, buttonColor, "refresh");
        ConfigureButton(retryButton, RequestRetryWithAd, retryLabel, retryButtonColor, "retry");
        ConfigureButton(addCardButton, AddRecommendedCard, addCardLabel, addCardButtonColor, "add card");

        if (_ads != null)
        {
            _ads.StateChanged += HandleAdsStateChanged;
        }

        if (cardMergeBoard != null)
        {
            cardMergeBoard.CardCountChanged += RefreshAddCardButtonState;
        }

        if (actionBudget != null)
        {
            actionBudget.Changed += RefreshAddCardButtonState;
        }

        RefreshAddCardButtonState();
        if (levelRuntime != null && levelRuntime.HasLevels)
        {
            levelRuntime.SpawnCurrentLevel();
            AttemptReady?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RequestRefreshWithAd);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RequestRetryWithAd);
        }

        if (addCardButton != null)
        {
            addCardButton.onClick.RemoveListener(AddRecommendedCard);
        }

        if (cardMergeBoard != null)
        {
            cardMergeBoard.CardCountChanged -= RefreshAddCardButtonState;
        }

        if (actionBudget != null)
        {
            actionBudget.Changed -= RefreshAddCardButtonState;
        }

        if (_ads != null)
        {
            _ads.StateChanged -= HandleAdsStateChanged;
        }
    }

    [ContextMenu("Refresh Game")]
    public void RefreshGame()
    {
        StartReset(replaySameLevel: false);
    }

    [ContextMenu("Retry Level")]
    public void RetryLevel()
    {
        StartReset(replaySameLevel: true);
    }

    public void NextLevel()
    {
        if (_levelTransitionPending || IsResetting)
        {
            return;
        }

        _levelTransitionPending = true;
        int completedLevelNumber = levelRuntime != null ? levelRuntime.CurrentLevelIndex + 1 : 0;
        bool adStarted = _ads != null
            && _ads.ShowInterstitialAfterLevel(completedLevelNumber, CompleteLevelTransition);
        if (!adStarted)
        {
            CompleteLevelTransition();
        }
    }

    public void AddRecommendedCard()
    {
        if (!Application.isPlaying || !GameplayInputEnabled || _tutorialActive)
        {
            return;
        }

        if (actionBudget == null || cardSpawner == null || !cardSpawner.CanSpawnCard())
        {
            return;
        }

        if (actionBudget.CanAddCard)
        {
            ExecuteAddRecommendedCard(consumeFreeUse: true);
            return;
        }

        _ads?.ShowRewarded(
            _ads.AddCardPlacement,
            () => ExecuteAddRecommendedCard(consumeFreeUse: false),
            () => cardMergeBoard?.ShowInteractionHint("AD UNAVAILABLE OR NOT COMPLETED — NO USE SPENT"));
    }

    private void ExecuteAddRecommendedCard(bool consumeFreeUse)
    {
        if (!Application.isPlaying || !GameplayInputEnabled || _tutorialActive || actionBudget == null
            || cardSpawner == null || !cardSpawner.CanSpawnCard())
        {
            return;
        }

        if (consumeFreeUse && !actionBudget.TryConsumeAddCardUse())
        {
            return;
        }

        Card spawnedCard = cardSpawner?.SpawnRecommendedCard(itemSpawner?.SpawnedItems);
        if (spawnedCard == null && consumeFreeUse)
        {
            actionBudget.GrantAddCardUses();
        }

        RefreshAddCardButtonState();
    }

    public void AddRandomCard()
    {
        AddRecommendedCard();
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        bool stateChanged = _gameplayInputEnabled != enabled;
        _gameplayInputEnabled = enabled;
        SetButtonsInteractable(GameplayInputEnabled);

        if (stateChanged)
        {
            GameplayInputChanged?.Invoke(GameplayInputEnabled);
        }
    }

    public void SetSettingsOpen(bool open)
    {
        if (_settingsOpen == open) return;
        _settingsOpen = open;
        SetButtonsInteractable(GameplayInputEnabled);
        GameplayInputChanged?.Invoke(GameplayInputEnabled);
    }

    public void SetTutorialActive(bool active)
    {
        _tutorialActive = active;
        SetButtonsInteractable(GameplayInputEnabled);
    }

    private void StartReset(bool replaySameLevel)
    {
        if (!Application.isPlaying || _resetRoutine != null)
        {
            return;
        }

        SetGameplayInputEnabled(false);
        actionBudget?.ResetForAttempt();
        AttemptResetStarted?.Invoke();
        _resetRoutine = StartCoroutine(ResetRoutine(replaySameLevel));
    }

    private void RequestRefreshWithAd()
    {
        if (CanRequestGameplayReward())
        {
            _ads.ShowRewarded(_ads.RefreshPlacement, RefreshGame,
                () => cardMergeBoard?.ShowInteractionHint("AD UNAVAILABLE OR NOT COMPLETED — TRY AGAIN"));
        }
    }

    private void RequestRetryWithAd()
    {
        if (CanRequestGameplayReward())
        {
            _ads.ShowRewarded(_ads.RetryPlacement, RetryLevel,
                () => cardMergeBoard?.ShowInteractionHint("AD UNAVAILABLE OR NOT COMPLETED — TRY AGAIN"));
        }
    }

    private bool CanRequestGameplayReward()
    {
        return Application.isPlaying && GameplayInputEnabled && !_tutorialActive && _ads != null;
    }

    private void CompleteLevelTransition()
    {
        levelRuntime?.MoveNext();
        _levelTransitionPending = false;
        StartReset(replaySameLevel: false);
    }

    private IEnumerator ResetRoutine(bool replaySameLevel)
    {
        SetButtonsInteractable(false);

        cardMergeBoard?.ClearSpawnedBoxGroups();
        if (cardSpawner != null)
        {
            // Stop the level deck before cards are destroyed. Otherwise each
            // removal could refill a slot while a retry/refresh is in progress.
            cardSpawner.ClearCards();
        }
        else
        {
            cardMergeBoard?.ClearCards();
        }
        itemSpawner?.ClearSpawnedItems();

        // Destroy is deferred until the end of the frame. Waiting keeps old and new
        // runtime objects from sharing layout/registry state during a refresh.
        yield return null;

        if (levelRuntime != null && levelRuntime.HasLevels)
        {
            levelRuntime.SpawnCurrentLevel();
        }
        else if (replaySameLevel)
        {
            cardSpawner?.ReplayLastCards();
            itemSpawner?.ReplayInitialItems();
        }
        else
        {
            cardSpawner?.SpawnConfiguredCards();
            itemSpawner?.SpawnInitialItems();
        }

        _resetRoutine = null;
        _levelTransitionPending = false;
        SetGameplayInputEnabled(true);
        AttemptReady?.Invoke();
    }

    private void ResolveReferences()
    {
        cardSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardSpawner>();
        cardMergeBoard ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardMergeBoard>();
        itemSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<MergeItemSpawner>();
        actionBudget ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeActionBudget>();
        levelRuntime ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeLevelRuntime>();

        if (refreshButton == null)
        {
            Transform buttonTransform = transform.Find("RefreshButton");
            if (buttonTransform != null)
            {
                refreshButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (retryButton == null)
        {
            Transform buttonTransform = transform.Find("RetryButton");
            if (buttonTransform != null)
            {
                retryButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (addCardButton == null)
        {
            Transform buttonTransform = transform.Find("AddCardButton");
            if (buttonTransform != null)
            {
                addCardButton = buttonTransform.GetComponent<Button>();
            }
        }
    }

    private void ConfigureButton(Button button, UnityEngine.Events.UnityAction action, string labelText, Color color, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"FarmBoxMerge {buttonName} button reference is missing.", this);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);

        if (button.targetGraphic is Image image)
        {
            image.color = color;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = labelText;
        }

        FarmBoxMergeRewardedAdBadge.CreateOrUpdate(button.transform);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        interactable &= !_tutorialActive;
        bool rewardedReady = _ads != null && _ads.IsRewardedReady;
        if (refreshButton != null)
        {
            refreshButton.interactable = interactable && rewardedReady;
        }

        if (retryButton != null)
        {
            retryButton.interactable = interactable && rewardedReady;
        }

        if (addCardButton != null)
        {
            addCardButton.interactable = interactable
                && actionBudget != null
                && cardSpawner != null
                && cardSpawner.CanSpawnCard();
        }

        RefreshAddCardButtonLabel();
    }

    private void RefreshAddCardButtonState()
    {
        if (addCardButton != null)
        {
            addCardButton.interactable = GameplayInputEnabled && !_tutorialActive
                && actionBudget != null
                && cardSpawner != null
                && cardSpawner.CanSpawnCard();
        }

        RefreshAddCardButtonLabel();
    }

    private void HandleAdsStateChanged()
    {
        SetButtonsInteractable(GameplayInputEnabled);
        GameplayInputChanged?.Invoke(GameplayInputEnabled);
    }

    private void RefreshAddCardButtonLabel()
    {
        if (addCardButton == null)
        {
            return;
        }

        int remainingUses = actionBudget != null ? actionBudget.RemainingAddCardUses : 0;
        TextMeshProUGUI label = FarmBoxMergeRewardedAdBadge.FindPrimaryLabel(addCardButton.transform);
        if (label != null)
        {
            label.text = remainingUses > 0 ? $"{addCardLabel} ({remainingUses})" : addCardLabel;
        }

        FarmBoxMergeRewardedAdBadge.SetVisible(addCardButton.transform, remainingUses <= 0);
    }
}
