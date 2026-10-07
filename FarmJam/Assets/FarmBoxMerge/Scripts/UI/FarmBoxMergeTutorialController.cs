using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using VContainer;

public interface IFarmBoxMergeTutorialFeature
{
    void Initialize();
}

public sealed class FarmBoxMergeNullTutorialFeature : IFarmBoxMergeTutorialFeature
{
    public void Initialize() { }
}

public interface IFarmBoxMergeCardInteractionGate
{
    bool CanDrag(Card card);
    bool CanMerge(Card source, Card target);
    bool CanPlace(Card card, FarmBoxMergeBoxSlotView slot);
    bool CanDiscard { get; }
}

[DisallowMultipleComponent]
public sealed class FarmBoxMergeTutorialController : MonoBehaviour,
    IFarmBoxMergeTutorialFeature, IFarmBoxMergeCardInteractionGate
{
    public enum TutorialStep { Inactive, Merge, Place }

    [Header("Authored Canvas View")]
    [SerializeField] private RectTransform overlay;
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private TextMeshProUGUI instruction;
    [SerializeField] private Image hand;
    [SerializeField] private RectTransform targetHighlight;
    [FormerlySerializedAs("mergeHand")]
    [SerializeField] private Sprite tutorialHand;
    [SerializeField] private Vector2 fingerPivot = new Vector2(0.38f, 0.96f);

    [Header("Guide")]
    [SerializeField, Min(0.5f)] private float gestureDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float gesturePause = 0.65f;

    private CardMergeBoard _board;
    private FarmBoxMergeGameController _game;
    private FarmBoxMergeLevelRuntime _levels;
    private IFarmBoxMergeTutorialProgress _progress;
    private IFarmBoxMergeLocalizationService _localization;
    private Camera _worldCamera;
    private Card _firstCard;
    private Card _secondCard;
    private Card _mergedCard;
    private FarmBoxMergeBoxSlotView _slot;
    private Coroutine _gesture;
    private bool _initialized;
    private bool _suppressedForSession;
    private readonly Vector3[] _corners = new Vector3[4];

    public TutorialStep Step { get; private set; }
    public bool IsInitialized => _initialized;
    public bool IsActive => Step != TutorialStep.Inactive;
    public bool CanDiscard => !IsActive;

    [Inject]
    public void Construct(CardMergeBoard board, FarmBoxMergeGameController game,
        FarmBoxMergeLevelRuntime levels, IFarmBoxMergeTutorialProgress progress,
        IFarmBoxMergeLocalizationService localization)
    {
        _board = board;
        _game = game;
        _levels = levels;
        _progress = progress;
        _localization = localization;
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _localization.Changed += RefreshInstruction;
        HideView();
        _game.AttemptResetStarted += StopTutorial;
        _game.AttemptReady += BeginAttempt;
        _board.CardMergeCompleted += OnMergeCompleted;
        _board.BoxGroupPlaced += OnBoxPlaced;
        BeginAttempt();
    }

    public bool CanDrag(Card card) => !IsActive || (Step == TutorialStep.Merge
        ? card == _firstCard || card == _secondCard : card == _mergedCard);

    public bool CanMerge(Card source, Card target) => !IsActive || (Step == TutorialStep.Merge
        && ((source == _firstCard && target == _secondCard)
            || (source == _secondCard && target == _firstCard)));

    public bool CanPlace(Card card, FarmBoxMergeBoxSlotView slot) => !IsActive
        || (Step == TutorialStep.Place && card == _mergedCard && slot != null && slot.CanAccept(2));

#if UNITY_EDITOR
    public void SetSuppressedForSession(bool suppressed)
    {
        _suppressedForSession = suppressed;
        if (suppressed) StopTutorial();
        else if (_initialized) BeginAttempt();
    }
#endif

    private void BeginAttempt()
    {
        StopTutorial();
        if (_suppressedForSession || _progress.Completed || _levels.CurrentLevelIndex != 0
            || _game.IsResetting || !_game.GameplayInputEnabled) return;
        if (tutorialHand == null && hand != null) tutorialHand = hand.sprite;
        if (overlay == null || overlayGroup == null || instruction == null || hand == null
            || targetHighlight == null || tutorialHand == null)
        {
            string missing = (overlay == null ? " Overlay" : "") + (overlayGroup == null ? " CanvasGroup" : "")
                + (instruction == null ? " Instruction" : "") + (hand == null ? " HandImage" : "")
                + (targetHighlight == null ? " TargetHighlight" : "") + (tutorialHand == null ? " HandSprite" : "");
            Debug.LogWarning("FarmBoxMerge tutorial view is incomplete. Missing:" + missing, this);
            return;
        }

        foreach (Card card in _board.CardContainer.GetComponentsInChildren<Card>())
        {
            if (card.IsBusy || card.CounterValue != 1) continue;
            if (_firstCard == null) _firstCard = card;
            else if (card.CardColorType == _firstCard.CardColorType) { _secondCard = card; break; }
        }
        _slot = _board.FindAvailableBoxSlot(2);
        _worldCamera = Camera.main;
        if (_firstCard == null || _secondCard == null || _slot == null || _worldCamera == null) return;

        Step = TutorialStep.Merge;
        _board.SetInteractionGate(this);
        _game.SetTutorialActive(true);
        RefreshInstruction();
        hand.sprite = tutorialHand;
        hand.rectTransform.pivot = fingerPivot;
        overlay.gameObject.SetActive(true);
        _gesture = StartCoroutine(AnimateGesture());
    }

    private void OnMergeCompleted(Card source, Card target)
    {
        if (Step != TutorialStep.Merge || !CanMerge(source, target) || target.CounterValue != 2) return;
        _mergedCard = target;
        Step = TutorialStep.Place;
        RefreshInstruction();
        RestartGesture();
    }

    private void OnBoxPlaced(Card card, FarmBoxMergeBoxSlotView slot)
    {
        if (Step != TutorialStep.Place || card != _mergedCard || slot == null || slot.AcceptedCardValue != 2) return;
        _progress.Complete();
        StopTutorial();
    }

    private void RestartGesture()
    {
        if (_gesture != null) StopCoroutine(_gesture);
        _gesture = StartCoroutine(AnimateGesture());
    }

    private IEnumerator AnimateGesture()
    {
        float elapsed = 0f;
        while (IsActive)
        {
            bool inputAvailable = _game.GameplayInputEnabled;
            overlayGroup.alpha = inputAvailable ? 1f : 0f;
            Card source = Step == TutorialStep.Merge ? _firstCard : _mergedCard;
            if (source == null || (Step == TutorialStep.Merge && _secondCard == null))
            {
                StopTutorial();
                yield break;
            }
            bool demonstrating = inputAvailable && !source.IsDragging && !source.IsBusy
                && (Step != TutorialStep.Merge || (!_secondCard.IsDragging && !_secondCard.IsBusy));
            hand.enabled = demonstrating;
            if (Step == TutorialStep.Place && (_slot == null || !_slot.CanAccept(2)))
                _slot = _board.FindAvailableBoxSlot(2);
            if (inputAvailable)
            {
                Vector2 start = CanvasPoint(RectTransformUtility.WorldToScreenPoint(_board.EventCamera,
                    source.RectTransform.TransformPoint(source.RectTransform.rect.center)));
                Vector2 end = Step == TutorialStep.Merge
                    ? CanvasPoint(RectTransformUtility.WorldToScreenPoint(_board.EventCamera,
                        _secondCard.RectTransform.TransformPoint(_secondCard.RectTransform.rect.center)))
                    : CanvasPoint(_worldCamera.WorldToScreenPoint(_slot != null ? _slot.transform.position : Vector3.zero));
                targetHighlight.anchoredPosition = end;
                targetHighlight.sizeDelta = Step == TutorialStep.Merge
                    ? _secondCard.RectTransform.rect.size + Vector2.one * 16f : new Vector2(240f, 230f);
                float pulse = 1f + 0.045f * Mathf.Sin(Time.unscaledTime * 4f);
                targetHighlight.localScale = Vector3.one * pulse;
                PositionInstruction();
                if (demonstrating)
                {
                    elapsed = (elapsed + Time.unscaledDeltaTime) % (gestureDuration + gesturePause);
                    float t = Mathf.Clamp01(elapsed / gestureDuration);
                    hand.rectTransform.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                    hand.rectTransform.localScale = Vector3.one * (1f - 0.07f * Mathf.Sin(t * Mathf.PI));
                    hand.color = new Color(1f, 1f, 1f, elapsed <= gestureDuration ? 1f : 0.4f);
                }
                else elapsed = 0f;
            }
            yield return null;
        }
    }

    private Vector2 CanvasPoint(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screenPoint, _board.EventCamera, out Vector2 point);
        return point;
    }

    private void PositionInstruction()
    {
        if (_board.CardContainer.parent is not RectTransform panel) return;
        panel.GetWorldCorners(_corners);
        float panelTop = CanvasPoint(RectTransformUtility.WorldToScreenPoint(_board.EventCamera, _corners[1])).y;
        var rect = instruction.transform.parent as RectTransform;
        if (rect == null) return;
        rect.sizeDelta = new Vector2(Mathf.Min(860f, overlay.rect.width - 48f), 112f);
        rect.anchoredPosition = new Vector2(0f, panelTop + 80f);
    }

    private void StopTutorial()
    {
        if (_gesture != null) { StopCoroutine(_gesture); _gesture = null; }
        Step = TutorialStep.Inactive;
        _board?.ClearInteractionGate(this);
        _game?.SetTutorialActive(false);
        _firstCard = _secondCard = _mergedCard = null;
        _slot = null;
        HideView();
    }

    private void HideView()
    {
        if (overlay != null) overlay.gameObject.SetActive(false);
        if (overlayGroup != null) { overlayGroup.blocksRaycasts = false; overlayGroup.interactable = false; }
    }

    private void OnDestroy()
    {
        StopTutorial();
        if (!_initialized) return;
        if (_localization != null) _localization.Changed -= RefreshInstruction;
        if (_game != null)
        {
            _game.AttemptResetStarted -= StopTutorial;
            _game.AttemptReady -= BeginAttempt;
        }
        if (_board != null)
        {
            _board.CardMergeCompleted -= OnMergeCompleted;
            _board.BoxGroupPlaced -= OnBoxPlaced;
        }
    }

    private void RefreshInstruction()
    {
        if (IsActive && instruction != null)
            instruction.text = _localization.Get(Step == TutorialStep.Merge ? "tutorial_merge" : "tutorial_place");
    }
}
