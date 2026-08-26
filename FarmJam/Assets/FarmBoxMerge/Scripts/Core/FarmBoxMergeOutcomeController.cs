using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

[DisallowMultipleComponent]
public class FarmBoxMergeOutcomeController : MonoBehaviour
{
    private enum PendingOutcome
    {
        None,
        Win,
        Fail
    }

    [Header("Game References")]
    [SerializeField] private FarmBoxMergeGameController gameController;
    [SerializeField] private CardMergeBoard cardMergeBoard;
    [SerializeField] private MergeItemSpawner itemSpawner;
    [SerializeField] private FarmBoxMergeActionBudget actionBudget;

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject failPanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryLevelButton;
    [SerializeField, Min(0f)] private float winDelay = 3f;
    [FormerlySerializedAs("outcomeDelay")]
    [SerializeField, Min(0f)] private float failDelay = 5f;

    private PendingOutcome _pendingOutcome;
    private float _pendingElapsed;
    private bool _monitoring;
    private bool _outcomeShown;
    private Coroutine _enableMonitoringRoutine;
    private IFarmBoxMergeFeedbackService _feedback;
    private bool _initialized;

    [Inject]
    public void Construct(
        IFarmBoxMergeFeedbackService feedback,
        FarmBoxMergeGameController injectedGameController,
        CardMergeBoard injectedCardMergeBoard,
        MergeItemSpawner injectedItemSpawner,
        FarmBoxMergeActionBudget injectedActionBudget)
    {
        _feedback = feedback;
        gameController = injectedGameController;
        cardMergeBoard = injectedCardMergeBoard;
        itemSpawner = injectedItemSpawner;
        actionBudget = injectedActionBudget;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResolveReferences();
        SetPanelState(showWin: false, showFail: false);
        ConfigureButtons();
        SubscribeToGameEvents();
        RestartMonitoringAfterSetup();
    }

    public void Tick()
    {
        if (!_monitoring || _outcomeShown || gameController == null || gameController.IsResetting)
        {
            return;
        }

        bool hasActiveBoxGroups = cardMergeBoard != null && cardMergeBoard.HasActiveBoxGroups;
        bool collectableItemsFinished = itemSpawner != null && !itemSpawner.HasRemainingItems;
        if (collectableItemsFinished)
        {
            gameController.SetGameplayInputEnabled(false);
        }

        if (collectableItemsFinished && !hasActiveBoxGroups)
        {
            AdvancePendingOutcome(PendingOutcome.Win);
            return;
        }

        bool allBoxSlotsOccupied = cardMergeBoard != null && cardMergeBoard.AreAllSpawnPointsOccupied;
        bool cardBoardIsFull = cardMergeBoard != null && !cardMergeBoard.HasCardCapacity;
        bool nextItemCannotJump = itemSpawner != null && itemSpawner.IsNextQueuedItemBlocked;
        bool impossibleBoxDemand = cardMergeBoard != null && cardMergeBoard.HasImpossibleBoxDemand(itemSpawner);
        bool boardIsBlocked = impossibleBoxDemand || (itemSpawner != null
            && itemSpawner.HasRemainingItems
            && allBoxSlotsOccupied
            && (cardBoardIsFull || nextItemCannotJump));

        AdvancePendingOutcome(boardIsBlocked ? PendingOutcome.Fail : PendingOutcome.None);
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
        }

        if (retryLevelButton != null)
        {
            retryLevelButton.onClick.RemoveListener(HandleRetryClicked);
        }
    }

    private void ConfigureButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
            nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
        }

        if (retryLevelButton != null)
        {
            retryLevelButton.onClick.RemoveListener(HandleRetryClicked);
            retryLevelButton.onClick.AddListener(HandleRetryClicked);
        }
    }

    private void SubscribeToGameEvents()
    {
        if (gameController != null)
        {
            gameController.AttemptResetStarted += HandleAttemptResetStarted;
            gameController.AttemptReady += RestartMonitoringAfterSetup;
        }

        if (cardMergeBoard != null)
        {
            cardMergeBoard.CardCountChanged += HandleGameplayActivity;
        }

        if (itemSpawner != null)
        {
            itemSpawner.ItemCountChanged += HandleGameplayActivity;
            itemSpawner.RemainingColorCountsChanged += HandleGameplayActivity;
        }

        if (actionBudget != null)
        {
            actionBudget.Changed += HandleGameplayActivity;
        }
    }

    private void UnsubscribeFromGameEvents()
    {
        if (gameController != null)
        {
            gameController.AttemptResetStarted -= HandleAttemptResetStarted;
            gameController.AttemptReady -= RestartMonitoringAfterSetup;
        }

        if (cardMergeBoard != null)
        {
            cardMergeBoard.CardCountChanged -= HandleGameplayActivity;
        }

        if (itemSpawner != null)
        {
            itemSpawner.ItemCountChanged -= HandleGameplayActivity;
            itemSpawner.RemainingColorCountsChanged -= HandleGameplayActivity;
        }

        if (actionBudget != null)
        {
            actionBudget.Changed -= HandleGameplayActivity;
        }
    }

    private void AdvancePendingOutcome(PendingOutcome outcome)
    {
        if (outcome == PendingOutcome.None)
        {
            CancelPendingOutcome();
            return;
        }

        if (_pendingOutcome != outcome)
        {
            _pendingOutcome = outcome;
            _pendingElapsed = 0f;
        }

        _pendingElapsed += Time.unscaledDeltaTime;
        float requiredDelay = outcome == PendingOutcome.Win ? winDelay : failDelay;
        if (_pendingElapsed < Mathf.Max(0f, requiredDelay))
        {
            return;
        }

        ShowOutcome(outcome);
    }

    private void ShowOutcome(PendingOutcome outcome)
    {
        _outcomeShown = true;
        _monitoring = false;
        gameController?.SetGameplayInputEnabled(false);
        bool won = outcome == PendingOutcome.Win;
        SetPanelState(won, outcome == PendingOutcome.Fail);
        _feedback?.PlayOutcome(won ? winPanel : failPanel, won);
    }

    private void HandleGameplayActivity()
    {
        if (!_outcomeShown && itemSpawner != null && !itemSpawner.HasRemainingItems)
        {
            gameController?.SetGameplayInputEnabled(false);
        }

        if (!_outcomeShown && _pendingOutcome == PendingOutcome.Fail)
        {
            CancelPendingOutcome();
        }
    }

    private void HandleAttemptResetStarted()
    {
        StopMonitoringRoutine();
        gameController?.SetGameplayInputEnabled(false);
        _monitoring = false;
        _outcomeShown = false;
        CancelPendingOutcome();
        SetPanelState(showWin: false, showFail: false);
    }

    private void RestartMonitoringAfterSetup()
    {
        StopMonitoringRoutine();
        bool hasCollectableItems = itemSpawner == null || itemSpawner.HasRemainingItems;
        gameController?.SetGameplayInputEnabled(hasCollectableItems);
        _enableMonitoringRoutine = StartCoroutine(EnableMonitoringAfterSetup());
    }

    private IEnumerator EnableMonitoringAfterSetup()
    {
        yield return null;

        _outcomeShown = false;
        CancelPendingOutcome();
        _monitoring = true;
        _enableMonitoringRoutine = null;
    }

    private void StopMonitoringRoutine()
    {
        if (_enableMonitoringRoutine == null)
        {
            return;
        }

        StopCoroutine(_enableMonitoringRoutine);
        _enableMonitoringRoutine = null;
    }

    private void HandleNextLevelClicked()
    {
        gameController?.NextLevel();
    }

    private void HandleRetryClicked()
    {
        gameController?.RetryLevel();
    }

    private void CancelPendingOutcome()
    {
        _pendingOutcome = PendingOutcome.None;
        _pendingElapsed = 0f;
    }

    private void SetPanelState(bool showWin, bool showFail)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(showWin);
        }

        if (failPanel != null)
        {
            failPanel.SetActive(showFail);
        }
    }

    private void ResolveReferences()
    {
        gameController ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeGameController>();
        cardMergeBoard ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardMergeBoard>();
        itemSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<MergeItemSpawner>();
        actionBudget ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeActionBudget>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FarmBoxMergeObjectUtility.FindSceneComponent<Canvas>();
        }

        Transform canvasTransform = canvas != null ? canvas.transform : transform;
        winPanel ??= canvasTransform.Find("WinPanel")?.gameObject;
        failPanel ??= canvasTransform.Find("FailPanel")?.gameObject;

        if (nextLevelButton == null && winPanel != null)
        {
            nextLevelButton = winPanel.GetComponentInChildren<Button>(true);
        }

        if (retryLevelButton == null && failPanel != null)
        {
            retryLevelButton = failPanel.GetComponentInChildren<Button>(true);
        }
    }
}
