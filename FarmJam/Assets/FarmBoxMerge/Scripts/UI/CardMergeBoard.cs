using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

[DisallowMultipleComponent]
public class CardMergeBoard : MonoBehaviour
{
    private static readonly ColorType[] AllColorTypes =
    {
        ColorType.Green,
        ColorType.Orange,
        ColorType.Purple,
        ColorType.Red,
        ColorType.Yellow
    };

    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
    private readonly HashSet<Card> _registeredCards = new HashSet<Card>();
    private readonly List<FarmBoxMergeBoxRequirement> _pendingSlotRequirements = new List<FarmBoxMergeBoxRequirement>();
    private readonly List<FarmBoxMergeBoxSlotView> _slotViews = new List<FarmBoxMergeBoxSlotView>();
    private readonly List<FarmBoxMergeBoxSlotView> _missingSlotViews = new List<FarmBoxMergeBoxSlotView>();
    private readonly Dictionary<ColorType, int> _remainingItemDemand = new Dictionary<ColorType, int>();
    private readonly Dictionary<ColorType, int> _remainingCardUnits = new Dictionary<ColorType, int>();

    [Serializable]
    public class ColorPaletteEntry
    {
        public ColorType colorType;
        public Color color = Color.white;
    }

    [Header("UI")]
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private RectTransform dragLayer;
    [SerializeField] private RectTransform spawnDropLayer;
    [SerializeField] private RectTransform trashDropLayer;
    [SerializeField] private FarmBoxMergeActionBudget actionBudget;
    [SerializeField] private FarmBoxMergeGameController gameController;
    [SerializeField] private FarmBoxMergeLevelRuntime levelRuntime;
    [SerializeField] private CardSpawner cardSpawner;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private bool createRuntimeDragLayer = true;
    [SerializeField] private bool createRuntimeSpawnDropLayer = true;

    [Header("Color Palette")]
    [SerializeField] private float colorDetectionTolerance = 0.1f;
    [SerializeField] private List<ColorPaletteEntry> colorPalette = new List<ColorPaletteEntry>();

    [Header("World Drop")]
    [SerializeField] private bool spawnBoxesOnCenterDrop = true;
    [SerializeField] private Camera worldDropCamera;
    [SerializeField] private Transform spawnSurface;
    [FormerlySerializedAs("spawnedBoxParent")]
    [SerializeField] private Transform spawnSlotRoot;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private Rect centerDropViewportRect = new Rect(0.18f, 0.20f, 0.64f, 0.55f);
    [SerializeField] private float boxSpacing = 1.15f;
    [SerializeField] private float spawnHeightOffset = 0.02f;
    [SerializeField] private bool randomizeFourBlockPatterns = true;
    [SerializeField] private bool createRuntimeSpawnPoints = true;
    [SerializeField] private int runtimeSpawnPointCount = 3;
    [SerializeField] private float runtimeSpawnPointSpacing = 3.4f;

    [Header("Box Slot Previews")]
    [SerializeField] private bool shuffleSlotRequirements = true;
    [SerializeField, Min(1)] private int mergeChallengeStartLevel = 11;
    [SerializeField] private Color slotPreviewBaseColor = new Color(1f, 1f, 1f, 0.28f);

    [Header("Trash Feedback")]
    [SerializeField] private string trashLabel = "TRASH";
    [SerializeField] private Color trashAvailableColor = new Color(0.78f, 0.20f, 0.20f, 0.88f);
    [SerializeField] private Color trashUnavailableColor = new Color(0.30f, 0.30f, 0.30f, 0.65f);

    public RectTransform CardContainer => cardContainer != null ? cardContainer : cardContainer = (RectTransform)transform;
    public Camera EventCamera => ResolveCanvasEventCamera();
    public IReadOnlyList<ColorPaletteEntry> ColorPalette => colorPalette;
    public bool HasColorPalette => colorPalette != null && colorPalette.Count > 0;
    public bool CanAcceptGameplayInput => gameController == null || gameController.GameplayInputEnabled;
    public int CardCount
    {
        get
        {
            _registeredCards.RemoveWhere(card => card == null);
            return _registeredCards.Count;
        }
    }
    public bool HasCardCapacity => CardCount < FarmBoxMergeRules.MaxCardsOnBoard;
    public bool HasActiveBoxGroups => ActiveBoxGroupCount > 0;
    public int ActiveBoxGroupCount
    {
        get
        {
            EnsureSpawnPoints();
            int count = 0;
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Transform spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                for (int childIndex = 0; childIndex < spawnPoint.childCount; childIndex++)
                {
                    if (spawnPoint.GetChild(childIndex).TryGetComponent(out MergeBoxParent group) && group != null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
    public bool AreAllSpawnPointsOccupied
    {
        get
        {
            EnsureSpawnPoints();
            if (spawnPoints.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] == null || !IsSpawnPointOccupied(spawnPoints[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public event Action CardCountChanged;

    private IBoxFactory _boxFactory;
    private IFarmBoxMergeFeedbackService _feedback;
    private MergeItemSpawner _itemSpawner;
    private Material _slotPreviewMaterial;
    private FarmBoxMergeLevelDefinition _activeSlotPlanLevel;
    private int _authoredSlotPlanCursor;
    private bool _slotPlanSuspended = true;
    private bool _initialized;

    [Inject]
    public void Construct(
        IBoxFactory boxFactory,
        IFarmBoxMergeFeedbackService feedback,
        MergeItemSpawner itemSpawner)
    {
        _boxFactory = boxFactory;
        _feedback = feedback;
        _itemSpawner = itemSpawner;
    }

    private void Reset()
    {
        ResolveReferences();
        EnsureDefaultPalette();
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResolveReferences();
        EnsureDefaultPalette();
        EnsureDragLayer();
        EnsureSpawnDropLayer();
        EnsureSpawnPoints();
        EnsureSlotViews();
        RegisterExistingCards();

        if (levelRuntime != null)
        {
            levelRuntime.CurrentLevelSpawned += HandleCurrentLevelSpawned;
        }

        if (actionBudget != null)
        {
            actionBudget.Changed += RefreshTrashState;
        }

        if (gameController != null)
        {
            gameController.GameplayInputChanged += HandleGameplayInputChanged;
        }

        RefreshTrashState();
    }

    private void OnDestroy()
    {
        if (actionBudget != null)
        {
            actionBudget.Changed -= RefreshTrashState;
        }

        if (gameController != null)
        {
            gameController.GameplayInputChanged -= HandleGameplayInputChanged;
        }

        if (levelRuntime != null)
        {
            levelRuntime.CurrentLevelSpawned -= HandleCurrentLevelSpawned;
        }

        for (int i = 0; i < _slotViews.Count; i++)
        {
            if (_slotViews[i] != null)
            {
                _slotViews[i].BecameAvailable -= HandleSlotBecameAvailable;
            }
        }

        if (_slotPreviewMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_slotPreviewMaterial);
            }
            else
            {
                DestroyImmediate(_slotPreviewMaterial);
            }
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public static List<ColorPaletteEntry> CreateDefaultPalette()
    {
        return new List<ColorPaletteEntry>
        {
            new ColorPaletteEntry { colorType = ColorType.Green, color = new Color(0.46f, 0.79f, 0.52f) },
            new ColorPaletteEntry { colorType = ColorType.Orange, color = new Color(0.95f, 0.55f, 0.40f) },
            new ColorPaletteEntry { colorType = ColorType.Purple, color = new Color(0.83f, 0.52f, 0.91f) },
            new ColorPaletteEntry { colorType = ColorType.Red, color = new Color(0.90f, 0.35f, 0.35f) },
            new ColorPaletteEntry { colorType = ColorType.Yellow, color = new Color(0.98f, 0.80f, 0.34f) }
        };
    }

    public void SetColorPalette(IList<ColorPaletteEntry> entries, bool overwriteExisting = true)
    {
        if (entries == null || entries.Count == 0 || (!overwriteExisting && HasColorPalette))
        {
            return;
        }

        colorPalette.Clear();

        foreach (ColorPaletteEntry entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            colorPalette.Add(new ColorPaletteEntry
            {
                colorType = entry.colorType,
                color = entry.color
            });
        }

        RefreshRegisteredCards();
    }

    public bool TryGetColor(ColorType colorType, out Color color)
    {
        foreach (ColorPaletteEntry entry in colorPalette)
        {
            if (entry != null && entry.colorType == colorType)
            {
                color = entry.color;
                return true;
            }
        }

        color = Color.white;
        return false;
    }

    public Color GetColorOrFallback(ColorType colorType, Color fallbackColor)
    {
        return TryGetColor(colorType, out Color resolvedColor) ? resolvedColor : fallbackColor;
    }

    public bool TryResolveColorType(Color color, out ColorType colorType)
    {
        colorType = default;

        if (!HasColorPalette)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        ColorType bestMatch = colorPalette[0].colorType;

        foreach (ColorPaletteEntry entry in colorPalette)
        {
            if (entry == null)
            {
                continue;
            }

            float distance = FarmBoxMergeMath.SqrColorDistance(color, entry.color);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = entry.colorType;
            }
        }

        if (bestDistance > colorDetectionTolerance * colorDetectionTolerance)
        {
            return false;
        }

        colorType = bestMatch;
        return true;
    }

    public void BeginDrag(Card card, PointerEventData eventData)
    {
        if (card == null || !CanAcceptGameplayInput)
        {
            return;
        }

        ResolveReferences();
        EnsureDragLayer();
        EnsureSpawnDropLayer();
        card.PrepareForDrag(dragLayer != null ? dragLayer : CardContainer, eventData);
        RefreshSlotPreviews(card.CounterValue, true);
        UpdateDrag(card, eventData);
    }

    public void UpdateDrag(Card card, PointerEventData eventData)
    {
        if (card == null || !card.IsDragging)
        {
            return;
        }

        if (!CanAcceptGameplayInput)
        {
            card.ReturnToOriginalSlot();
            RefreshSlotPreviews();
            return;
        }

        RectTransform dragSurface = dragLayer != null ? dragLayer : CardContainer;
        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : EventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragSurface, eventData.position, eventCamera, out Vector2 localPoint))
        {
            card.UpdateDragPosition(localPoint);
        }
    }

    public void EndDrag(Card card, PointerEventData eventData)
    {
        if (card == null)
        {
            return;
        }

        if (card.MergeCompleted)
        {
            RefreshSlotPreviews();
            return;
        }

        if (!CanAcceptGameplayInput)
        {
            if (card.IsDragging)
            {
                card.ReturnToOriginalSlot();
            }

            RefreshSlotPreviews();
            return;
        }

        bool handled = TryDiscardCard(card, eventData) || TrySpawnBoxesFromCard(card, eventData);
        RefreshSlotPreviews();
        if (handled)
        {
            return;
        }

        card.ReturnToOriginalSlot();
    }

    public bool TryMerge(Card draggedCard, Card targetCard)
    {
        if (!CanAcceptGameplayInput || draggedCard == null || targetCard == null || draggedCard == targetCard)
        {
            return false;
        }

        RegisterCard(draggedCard);
        RegisterCard(targetCard);

        if (!targetCard.CanMergeWith(draggedCard))
        {
            return false;
        }

        draggedCard.MergeInto(targetCard);
        return true;
    }

    public void RegisterCard(Card card)
    {
        if (card == null)
        {
            return;
        }

        card.AssignBoard(this);
        if (_registeredCards.Add(card))
        {
            card.SyncDataFromView();
            CardCountChanged?.Invoke();
            FillMissingSlotRequirements();
        }
    }

    public void UnregisterCard(Card card)
    {
        if (_registeredCards.Remove(card))
        {
            CardCountChanged?.Invoke();
            FillMissingSlotRequirements();
        }
    }

    public int GetCardCapacity(ColorType colorType)
    {
        int capacity = 0;
        _registeredCards.RemoveWhere(card => card == null);

        foreach (Card card in _registeredCards)
        {
            if (card.CardColorType == colorType)
            {
                capacity += card.CounterValue;
            }
        }

        return capacity;
    }

    public bool HasLevelOneCard(ColorType colorType)
    {
        _registeredCards.RemoveWhere(card => card == null);

        foreach (Card card in _registeredCards)
        {
            if (card.CardColorType == colorType && card.CounterValue == FarmBoxMergeRules.MinCardCounter)
            {
                return true;
            }
        }

        return false;
    }

    public int GetOutstandingBoxDemand(ColorType colorType)
    {
        EnsureSpawnPoints();
        int demand = 0;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            for (int childIndex = 0; childIndex < spawnPoint.childCount; childIndex++)
            {
                if (!spawnPoint.GetChild(childIndex).TryGetComponent(out MergeBoxParent group)
                    || group == null
                    || group.IsCollapsing
                    || group.ColorType != colorType)
                {
                    continue;
                }

                demand += group.EmptyBoxCount;
            }
        }

        return demand;
    }

    public bool HasImpossibleBoxDemand(MergeItemSpawner itemSpawner)
    {
        if (itemSpawner == null)
        {
            return false;
        }

        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            ColorType colorType = AllColorTypes[i];
            if (GetOutstandingBoxDemand(colorType) > itemSpawner.GetRemainingUnplacedCount(colorType))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAnyStandardCardMove()
    {
        if (HasAnyCardMergeMove())
        {
            return true;
        }

        EnsureSlotViews();
        _registeredCards.RemoveWhere(card => card == null);
        foreach (Card card in _registeredCards)
        {
            for (int slotIndex = 0; slotIndex < _slotViews.Count; slotIndex++)
            {
                FarmBoxMergeBoxSlotView slotView = _slotViews[slotIndex];
                if (slotView != null && slotView.CanAccept(card.CounterValue))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void ClearSpawnedBoxGroups()
    {
        _slotPlanSuspended = true;
        _pendingSlotRequirements.Clear();
        EnsureSlotViews();
        for (int i = 0; i < _slotViews.Count; i++)
        {
            _slotViews[i]?.ClearRequirement();
        }

        EnsureSpawnSlotRoot();
        if (spawnSlotRoot == null)
        {
            return;
        }

        for (int i = spawnSlotRoot.childCount - 1; i >= 0; i--)
        {
            Transform spawnPoint = spawnSlotRoot.GetChild(i);
            for (int childIndex = spawnPoint.childCount - 1; childIndex >= 0; childIndex--)
            {
                GameObject child = spawnPoint.GetChild(childIndex).gameObject;
                if (child.TryGetComponent(out MergeBoxParent _))
                {
                    FarmBoxMergeObjectUtility.Destroy(child);
                }
            }
        }
    }

    public void ClearCards()
    {
        DestroyCardsIn(cardContainer);

        if (dragLayer != null && dragLayer != cardContainer)
        {
            DestroyCardsIn(dragLayer);
        }
    }

    private static void DestroyCardsIn(Transform container)
    {
        if (container == null)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (child.TryGetComponent(out Card _))
            {
                FarmBoxMergeObjectUtility.Destroy(child);
            }
        }
    }

    private bool TrySpawnBoxesFromCard(Card card, PointerEventData eventData)
    {
        if (!spawnBoxesOnCenterDrop || card == null || _boxFactory == null)
        {
            return false;
        }

        Camera dropCamera = ResolveWorldDropCamera();
        if (dropCamera == null)
        {
            return false;
        }

        if (!IsPointerInsideSpawnDropLayer(eventData.position))
        {
            return false;
        }

        if (!TryGetAvailableSpawnPoint(card.CounterValue, out Transform availableSpawnPoint))
        {
            return false;
        }

        FarmBoxMergeBoxSlotView slotView = availableSpawnPoint.GetComponent<FarmBoxMergeBoxSlotView>();
        MergeBoxParent spawnedGroup = SpawnBoxGroup(
            card.CounterValue,
            card.CardColorType,
            availableSpawnPoint,
            slotView);
        if (spawnedGroup == null)
        {
            return false;
        }

        card.ConsumeForWorldSpawn();
        return true;
    }

    private bool TryDiscardCard(Card card, PointerEventData eventData)
    {
        if (card == null || trashDropLayer == null || !IsPointerInsideUiLayer(trashDropLayer, eventData.position))
        {
            return false;
        }

        if (actionBudget == null || !actionBudget.TryConsumeTrashUse())
        {
            return false;
        }

        card.DiscardInto(trashDropLayer);
        return true;
    }

    private MergeBoxParent SpawnBoxGroup(
        int boxCount,
        ColorType colorType,
        Transform targetSpawnPoint,
        FarmBoxMergeBoxSlotView slotView)
    {
        EnsureSpawnPoints();

        if (targetSpawnPoint == null)
        {
            return null;
        }

        int clampedBoxCount = FarmBoxMergeRules.ClampCardCounter(boxCount);
        GameObject groupObject = new GameObject($"BoxParent_{colorType}_{clampedBoxCount}", typeof(MergeBoxParent));
        Transform groupTransform = groupObject.transform;
        groupTransform.SetParent(targetSpawnPoint, false);
        groupTransform.localPosition = Vector3.up * spawnHeightOffset;
        groupTransform.localRotation = Quaternion.identity;
        groupTransform.localScale = Vector3.one;

        MergeBoxParent boxParent = groupObject.GetComponent<MergeBoxParent>();
        BoxPatternDefinition pattern = slotView != null
            ? slotView.Pattern
            : ResolvePattern(clampedBoxCount);
        Vector3[] localPositions = GetCenteredLocalPositions(pattern.Cells);
        List<Box> spawnedBoxes = new List<Box>(localPositions.Length);

        for (int i = 0; i < localPositions.Length; i++)
        {
            Box spawnedBox = _boxFactory?.Create(groupTransform);
            if (spawnedBox == null)
            {
                FarmBoxMergeObjectUtility.Destroy(groupObject);
                return null;
            }

            spawnedBox.transform.localPosition = localPositions[i];
            spawnedBox.transform.localRotation = Quaternion.identity;
            spawnedBoxes.Add(spawnedBox);
        }

        boxParent.Initialize(clampedBoxCount, colorType, pattern.PatternType, spawnedBoxes, pattern.Cells, _feedback);
        _feedback?.PlayBoxCreated(groupTransform, colorType);
        _itemSpawner?.TryProcessQueue();
        return boxParent;
    }

    private BoxPatternDefinition ResolvePattern(int boxCount)
    {
        return BoxPatternLibrary.Resolve(boxCount, randomizeFourBlockPatterns);
    }

    private Vector3[] GetCenteredLocalPositions(Vector2Int[] cells)
    {
        return BoxPatternLibrary.GetCenteredPositions(cells, boxSpacing);
    }

    private void RegisterExistingCards()
    {
        for (int i = 0; i < CardContainer.childCount; i++)
        {
            Transform child = CardContainer.GetChild(i);
            if (child.TryGetComponent(out Card card))
            {
                RegisterCard(card);
            }
        }
    }

    private void ResolveReferences()
    {
        if (cardContainer == null)
        {
            cardContainer = (RectTransform)transform;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (trashDropLayer == null && rootCanvas != null)
        {
            trashDropLayer = rootCanvas.transform.Find("TrashDropZone") as RectTransform;
        }

        actionBudget ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeActionBudget>();
        gameController ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeGameController>();
        levelRuntime ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeLevelRuntime>();
        cardSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardSpawner>();

        if (spawnSurface == null)
        {
            GameObject platformObject = GameObject.Find("Platform");
            if (platformObject != null)
            {
                spawnSurface = platformObject.transform;
            }
        }

        spawnPoints.RemoveAll(point => point == null);
    }

    private void HandleGameplayInputChanged(bool inputEnabled)
    {
        RefreshTrashState();

        if (inputEnabled)
        {
            return;
        }

        RefreshSlotPreviews();

        _registeredCards.RemoveWhere(card => card == null);
        foreach (Card card in _registeredCards)
        {
            if (card.IsDragging && !card.MergeCompleted)
            {
                card.ReturnToOriginalSlot();
            }
        }
    }

    private void EnsureDefaultPalette()
    {
        if (HasColorPalette)
        {
            return;
        }

        colorPalette = CreateDefaultPalette();
    }

    private void RefreshRegisteredCards()
    {
        for (int i = 0; i < CardContainer.childCount; i++)
        {
            Transform child = CardContainer.GetChild(i);
            if (child.TryGetComponent(out Card card))
            {
                card.AssignBoard(this);
                card.RefreshVisuals();
            }
        }
    }

    private void EnsureDragLayer()
    {
        RectTransform parentRect = GetDragLayerParent();
        if (parentRect == null)
        {
            dragLayer = CardContainer;
            return;
        }

        if (dragLayer == null)
        {
            Transform existingLayer = parentRect.Find("CardDragLayer");
            if (existingLayer != null)
            {
                dragLayer = existingLayer as RectTransform;
            }
        }

        if (dragLayer == null)
        {
            if (!createRuntimeDragLayer)
            {
                dragLayer = CardContainer;
                return;
            }

            GameObject dragLayerObject = new GameObject("CardDragLayer", typeof(RectTransform));
            dragLayer = dragLayerObject.GetComponent<RectTransform>();
        }

        if (dragLayer.parent != parentRect)
        {
            dragLayer.SetParent(parentRect, false);
        }

        dragLayer.anchorMin = Vector2.zero;
        dragLayer.anchorMax = Vector2.one;
        dragLayer.pivot = new Vector2(0.5f, 0.5f);
        dragLayer.anchoredPosition = Vector2.zero;
        dragLayer.sizeDelta = Vector2.zero;
        dragLayer.SetAsLastSibling();
    }

    private void EnsureSpawnDropLayer()
    {
        RectTransform parentRect = GetDragLayerParent();
        if (parentRect == null)
        {
            spawnDropLayer = null;
            return;
        }

        bool createdRuntimeLayer = false;

        if (spawnDropLayer == null)
        {
            Transform existingLayer = parentRect.Find("CardSpawnDropLayer");
            if (existingLayer != null)
            {
                spawnDropLayer = existingLayer as RectTransform;
            }
        }

        if (spawnDropLayer == null)
        {
            if (!createRuntimeSpawnDropLayer)
            {
                return;
            }

            GameObject spawnDropLayerObject = new GameObject("CardSpawnDropLayer", typeof(RectTransform));
            spawnDropLayer = spawnDropLayerObject.GetComponent<RectTransform>();
            createdRuntimeLayer = true;
        }

        if (spawnDropLayer == null)
        {
            return;
        }

        EnsureSpawnDropLayerGraphic();

        if (!createdRuntimeLayer)
        {
            return;
        }

        spawnDropLayer.SetParent(parentRect, false);
        spawnDropLayer.anchorMin = centerDropViewportRect.min;
        spawnDropLayer.anchorMax = centerDropViewportRect.max;
        spawnDropLayer.pivot = new Vector2(0.5f, 0.5f);
        spawnDropLayer.anchoredPosition = Vector2.zero;
        spawnDropLayer.sizeDelta = Vector2.zero;
        spawnDropLayer.SetSiblingIndex(Mathf.Max(0, dragLayer != null ? dragLayer.GetSiblingIndex() - 1 : parentRect.childCount - 1));
    }

    private RectTransform GetDragLayerParent()
    {
        if (rootCanvas != null && rootCanvas.transform is RectTransform canvasRect)
        {
            return canvasRect;
        }

        return CardContainer.parent as RectTransform;
    }

    private bool IsPointerInsideSpawnDropLayer(Vector2 screenPosition)
    {
        return IsPointerInsideUiLayer(spawnDropLayer, screenPosition);
    }

    private bool IsPointerInsideUiLayer(RectTransform targetLayer, Vector2 screenPosition)
    {
        if (targetLayer == null)
        {
            return false;
        }

        if (EventSystem.current != null)
        {
            _uiRaycastResults.Clear();
            PointerEventData raycastPointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            EventSystem.current.RaycastAll(raycastPointerData, _uiRaycastResults);

            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                Transform hitTransform = _uiRaycastResults[i].gameObject.transform;
                if (hitTransform == targetLayer || hitTransform.IsChildOf(targetLayer))
                {
                    return true;
                }
            }

            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(targetLayer, screenPosition, EventCamera);
    }

    private void EnsureSpawnDropLayerGraphic()
    {
        if (spawnDropLayer == null)
        {
            return;
        }

        if (spawnDropLayer.TryGetComponent(out Graphic existingGraphic))
        {
            existingGraphic.raycastTarget = true;
            return;
        }

        Image image = spawnDropLayer.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
    }

    private void RefreshTrashState()
    {
        if (trashDropLayer == null)
        {
            return;
        }

        bool canUseTrash = CanAcceptGameplayInput && actionBudget != null && actionBudget.CanUseTrash;
        if (trashDropLayer.TryGetComponent(out Image image))
        {
            image.color = canUseTrash ? trashAvailableColor : trashUnavailableColor;
            image.raycastTarget = canUseTrash;
        }

        TextMeshProUGUI label = trashDropLayer.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            int remainingUses = actionBudget != null ? actionBudget.RemainingTrashUses : 0;
            label.text = $"{trashLabel} ({remainingUses})";
        }
    }

    private bool TryGetAvailableSpawnPoint(
        int cardValue,
        out Transform availableSpawnPoint)
    {
        EnsureSlotViews();

        for (int i = 0; i < _slotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _slotViews[i];
            if (slotView == null || !slotView.CanAccept(cardValue))
            {
                continue;
            }

            availableSpawnPoint = slotView.transform;
            return true;
        }

        availableSpawnPoint = null;
        return false;
    }

    private void EnsureSlotViews()
    {
        EnsureSpawnPoints();
        _slotViews.RemoveAll(slotView => slotView == null);

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            FarmBoxMergeBoxSlotView slotView = spawnPoint.GetComponent<FarmBoxMergeBoxSlotView>();
            if (slotView == null)
            {
                slotView = spawnPoint.gameObject.AddComponent<FarmBoxMergeBoxSlotView>();
            }

            if (_slotViews.Contains(slotView))
            {
                continue;
            }

            _slotViews.Add(slotView);
            slotView.BecameAvailable -= HandleSlotBecameAvailable;
            slotView.BecameAvailable += HandleSlotBecameAvailable;
        }
    }

    private void HandleCurrentLevelSpawned(FarmBoxMergeLevelDefinition level)
    {
        EnsureSlotViews();
        _slotPlanSuspended = true;
        _activeSlotPlanLevel = level;
        _authoredSlotPlanCursor = 0;
        _pendingSlotRequirements.Clear();

        for (int i = 0; i < _slotViews.Count; i++)
        {
            _slotViews[i]?.ClearRequirement();
        }

        _slotPlanSuspended = false;
        FillMissingSlotRequirements();
    }

    private void HandleSlotBecameAvailable(FarmBoxMergeBoxSlotView slotView)
    {
        if (!_slotPlanSuspended)
        {
            FillMissingSlotRequirements();
        }
    }

    private void FillMissingSlotRequirements()
    {
        if (!_initialized || _slotPlanSuspended || _itemSpawner == null)
        {
            return;
        }

        _slotPlanSuspended = true;
        EnsureSlotViews();
        if (TryFillAuthoredSlotRequirements())
        {
            _slotPlanSuspended = false;
            return;
        }

        BuildRemainingItemAndCardTotals();
        bool hasValidPlan = FarmBoxMergeSlotPlanBuilder.TryBuildRemainingPlan(
            _remainingItemDemand,
            _remainingCardUnits,
            _pendingSlotRequirements,
            out _);

        if (hasValidPlan)
        {
            ReserveExistingSlotRequirements();
            if (shuffleSlotRequirements)
            {
                ShuffleRequirements(_pendingSlotRequirements);
            }
        }

        CollectMissingSlotViews();
        ShuffleSlotViews(_missingSlotViews);
        for (int i = 0; i < _missingSlotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _missingSlotViews[i];
            bool needsUnlockSingle = NeedsUnlockSingleRequirement();
            if (hasValidPlan)
            {
                AssignNextRequirement(slotView, needsUnlockSingle);
            }

            if (!slotView.HasRequirement)
            {
                AssignFallbackRequirement(slotView, needsUnlockSingle);
            }
        }

        EnsureVisibleMergeRequirement();
        EnsureQueueScheduleSafety();

        _slotPlanSuspended = false;
    }

    private bool TryFillAuthoredSlotRequirements()
    {
        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> authoredPlan =
            _activeSlotPlanLevel != null ? _activeSlotPlanLevel.BoxSlotPlan : null;
        if (authoredPlan == null || authoredPlan.Count == 0)
        {
            return false;
        }

        // Spawn point order is stable: the first entries fill the initial slots
        // from left to right. Afterwards only the slot that became empty consumes
        // the next entry. Replaying the level therefore produces the same flow.
        CollectMissingSlotViews();
        for (int i = 0; i < _missingSlotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _missingSlotViews[i];
            FarmBoxMergeBoxSlotPlanEntry entry =
                authoredPlan[_authoredSlotPlanCursor % authoredPlan.Count];
            _authoredSlotPlanCursor++;

            if (slotView == null || entry == null)
            {
                continue;
            }

            ApplyAuthoredRequirement(slotView, entry);
        }

        return true;
    }

    private void EnsureVisibleMergeRequirement()
    {
        if (_missingSlotViews.Count == 0 || HasVisibleBuildableNonSingleRequirement())
        {
            return;
        }

        int fallbackSize = GetMostBuildableFallbackSize(false);
        if (fallbackSize <= FarmBoxMergeRules.MinCardCounter
            || !CanAnyRemainingColorBuildCard(fallbackSize))
        {
            return;
        }

        List<FarmBoxMergeBoxSlotView> replaceableSlots = new List<FarmBoxMergeBoxSlotView>();
        for (int i = 0; i < _missingSlotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _missingSlotViews[i];
            if (slotView != null
                && !slotView.IsOccupied
                && slotView.HasRequirement
                && slotView.AcceptedCardValue != fallbackSize)
            {
                replaceableSlots.Add(slotView);
            }
        }

        if (replaceableSlots.Count == 0)
        {
            return;
        }

        FarmBoxMergeBoxSlotView buildableSlot = replaceableSlots[
            UnityEngine.Random.Range(0, replaceableSlots.Count)];
        ApplyRequirement(
            buildableSlot,
            new FarmBoxMergeBoxRequirement(GetHighestDemandColor(), fallbackSize));
    }

    private bool CanAnyRemainingColorBuildCard(int boxSize)
    {
        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            ColorType colorType = AllColorTypes[i];
            int remainingDemand = _remainingItemDemand.TryGetValue(colorType, out int demand)
                ? demand
                : 0;
            if (remainingDemand >= boxSize
                && CanBuildCard(GetVisibleCardsByValue(colorType), boxSize))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasVisibleBuildableNonSingleRequirement()
    {
        for (int colorIndex = 0; colorIndex < AllColorTypes.Length; colorIndex++)
        {
            ColorType colorType = AllColorTypes[colorIndex];
            int remainingDemand = _remainingItemDemand.TryGetValue(colorType, out int demand)
                ? demand
                : 0;
            if (remainingDemand <= 0)
            {
                continue;
            }

            int[] visibleCards = GetVisibleCardsByValue(colorType);
            for (int slotIndex = 0; slotIndex < _slotViews.Count; slotIndex++)
            {
                FarmBoxMergeBoxSlotView slotView = _slotViews[slotIndex];
                if (slotView != null
                    && !slotView.IsOccupied
                    && slotView.HasRequirement
                    && slotView.AcceptedCardValue > FarmBoxMergeRules.MinCardCounter
                    && slotView.AcceptedCardValue <= remainingDemand
                    && CanBuildCard(visibleCards, slotView.AcceptedCardValue))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void EnsureQueueScheduleSafety()
    {
        if (_missingSlotViews.Count == 0 || HasSafeVisibleQueueSchedule())
        {
            return;
        }

        List<FarmBoxMergeBoxSlotView> replaceableSlots = new List<FarmBoxMergeBoxSlotView>();
        for (int i = 0; i < _missingSlotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _missingSlotViews[i];
            if (slotView != null
                && !slotView.IsOccupied
                && slotView.HasRequirement
                && slotView.AcceptedCardValue > FarmBoxMergeRules.MinCardCounter)
            {
                replaceableSlots.Add(slotView);
            }
        }

        if (replaceableSlots.Count == 0 || !HasLevelOneResourceForRemainingItems())
        {
            return;
        }

        FarmBoxMergeBoxSlotView safetySlot = replaceableSlots[
            UnityEngine.Random.Range(0, replaceableSlots.Count)];
        ApplyRequirement(
            safetySlot,
            new FarmBoxMergeBoxRequirement(
                GetHighestDemandColor(),
                FarmBoxMergeRules.MinCardCounter));
    }

    private bool HasSafeVisibleQueueSchedule()
    {
        IReadOnlyList<MergeItem> queuedItems = _itemSpawner != null
            ? _itemSpawner.SpawnedItems
            : null;
        if (queuedItems == null || queuedItems.Count == 0)
        {
            return true;
        }

        int slotCapacity = Mathf.Max(1, _slotViews.Count);
        List<ColorType> boxColors = new List<ColorType>(slotCapacity);
        List<int> boxRemainingCapacity = new List<int>(slotCapacity);
        List<int> availableShapes = new List<int>(slotCapacity);

        for (int i = 0; i < _slotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _slotViews[i];
            if (slotView == null)
            {
                continue;
            }

            bool foundActiveGroup = false;
            for (int childIndex = 0; childIndex < slotView.transform.childCount; childIndex++)
            {
                if (!slotView.transform.GetChild(childIndex).TryGetComponent(out MergeBoxParent group)
                    || group == null
                    || group.IsCollapsing)
                {
                    continue;
                }

                boxColors.Add(group.ColorType);
                boxRemainingCapacity.Add(group.EmptyBoxCount);
                foundActiveGroup = true;
                break;
            }

            if (!foundActiveGroup && slotView.HasRequirement)
            {
                availableShapes.Add(slotView.AcceptedCardValue);
            }
        }

        int[] unreservedDemand = new int[AllColorTypes.Length];
        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            unreservedDemand[i] = _remainingItemDemand.TryGetValue(AllColorTypes[i], out int demand)
                ? demand
                : 0;
        }

        return CanReachQueueRelease(
            queuedItems,
            0,
            boxColors,
            boxRemainingCapacity,
            availableShapes,
            unreservedDemand);
    }

    private bool CanReachQueueRelease(
        IReadOnlyList<MergeItem> queuedItems,
        int itemIndex,
        List<ColorType> boxColors,
        List<int> boxRemainingCapacity,
        List<int> availableShapes,
        int[] unreservedDemand)
    {
        if (itemIndex >= queuedItems.Count)
        {
            return true;
        }

        if (queuedItems[itemIndex] == null)
        {
            return CanReachQueueRelease(
                queuedItems,
                itemIndex + 1,
                boxColors,
                boxRemainingCapacity,
                availableShapes,
                unreservedDemand);
        }

        ColorType itemColor = queuedItems[itemIndex].ColorType;
        bool hasMatchingBox = false;
        for (int boxIndex = 0; boxIndex < boxColors.Count; boxIndex++)
        {
            if (boxColors[boxIndex] != itemColor || boxRemainingCapacity[boxIndex] <= 0)
            {
                continue;
            }

            hasMatchingBox = true;
            boxRemainingCapacity[boxIndex]--;
            bool releasesSlot = boxRemainingCapacity[boxIndex] == 0;
            bool succeeds = releasesSlot || CanReachQueueRelease(
                queuedItems,
                itemIndex + 1,
                boxColors,
                boxRemainingCapacity,
                availableShapes,
                unreservedDemand);
            boxRemainingCapacity[boxIndex]++;
            if (succeeds)
            {
                return true;
            }
        }

        if (hasMatchingBox)
        {
            return false;
        }

        int colorIndex = Array.IndexOf(AllColorTypes, itemColor);
        if (colorIndex < 0)
        {
            return false;
        }

        for (int shapeIndex = 0; shapeIndex < availableShapes.Count; shapeIndex++)
        {
            int boxSize = availableShapes[shapeIndex];
            if (boxSize > unreservedDemand[colorIndex])
            {
                continue;
            }

            availableShapes.RemoveAt(shapeIndex);
            unreservedDemand[colorIndex] -= boxSize;
            boxColors.Add(itemColor);
            boxRemainingCapacity.Add(boxSize - 1);

            bool succeeds = boxSize == FarmBoxMergeRules.MinCardCounter || CanReachQueueRelease(
                queuedItems,
                itemIndex + 1,
                boxColors,
                boxRemainingCapacity,
                availableShapes,
                unreservedDemand);

            boxColors.RemoveAt(boxColors.Count - 1);
            boxRemainingCapacity.RemoveAt(boxRemainingCapacity.Count - 1);
            unreservedDemand[colorIndex] += boxSize;
            availableShapes.Insert(shapeIndex, boxSize);
            if (succeeds)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasLevelOneResourceForRemainingItems()
    {
        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            ColorType colorType = AllColorTypes[i];
            if (!_remainingItemDemand.TryGetValue(colorType, out int demand) || demand <= 0)
            {
                continue;
            }

            if (HasLevelOneCard(colorType)
                || (cardSpawner != null && cardSpawner.GetPendingLevelOneCardCount(colorType) > 0))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectMissingSlotViews()
    {
        _missingSlotViews.Clear();
        for (int i = 0; i < _slotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _slotViews[i];
            if (slotView != null && !slotView.IsOccupied && !slotView.HasRequirement)
            {
                _missingSlotViews.Add(slotView);
            }
        }
    }

    private bool NeedsUnlockSingleRequirement()
    {
        if (_itemSpawner == null || !_itemSpawner.IsNextQueuedItemBlocked)
        {
            return false;
        }

        IReadOnlyList<MergeItem> queuedItems = _itemSpawner.SpawnedItems;
        if (queuedItems == null || queuedItems.Count == 0 || queuedItems[0] == null)
        {
            return false;
        }

        ColorType blockedColor = queuedItems[0].ColorType;
        int remainingDemand = _remainingItemDemand.TryGetValue(blockedColor, out int demand)
            ? demand
            : 0;
        if (remainingDemand <= 0)
        {
            return false;
        }

        int[] visibleCards = GetVisibleCardsByValue(blockedColor);
        if (HasBuildableNonSingleRequirement(visibleCards, remainingDemand))
        {
            return false;
        }

        if (UsesMergeChallengeRules()
            && (HasAnyBuildableNonSingleRequirement() || HasAnyCardMergeMove()))
        {
            return false;
        }

        bool hasLevelOneCard = visibleCards[FarmBoxMergeRules.MinCardCounter] > 0;
        bool hasPendingLevelOneCard = cardSpawner != null
            && cardSpawner.GetPendingLevelOneCardCount(blockedColor) > 0;
        return hasLevelOneCard || hasPendingLevelOneCard;
    }

    private bool UsesMergeChallengeRules()
    {
        int currentLevelNumber = levelRuntime != null
            ? levelRuntime.CurrentLevelIndex + 1
            : 1;
        return currentLevelNumber >= Mathf.Max(1, mergeChallengeStartLevel);
    }

    private bool HasAnyBuildableNonSingleRequirement()
    {
        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            ColorType colorType = AllColorTypes[i];
            int remainingDemand = _remainingItemDemand.TryGetValue(colorType, out int demand)
                ? demand
                : 0;
            if (remainingDemand > 0
                && HasBuildableNonSingleRequirement(
                    GetVisibleCardsByValue(colorType),
                    remainingDemand))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyCardMergeMove()
    {
        _registeredCards.RemoveWhere(card => card == null);
        foreach (Card firstCard in _registeredCards)
        {
            if (firstCard == null || firstCard.CounterValue >= FarmBoxMergeRules.MaxCardCounter)
            {
                continue;
            }

            foreach (Card secondCard in _registeredCards)
            {
                if (secondCard != null
                    && !ReferenceEquals(firstCard, secondCard)
                    && firstCard.CardColorType == secondCard.CardColorType
                    && firstCard.CounterValue == secondCard.CounterValue)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasBuildableNonSingleRequirement(int[] visibleCards, int remainingDemand)
    {
        for (int i = 0; i < _slotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _slotViews[i];
            if (slotView != null
                && !slotView.IsOccupied
                && slotView.HasRequirement
                && slotView.AcceptedCardValue > FarmBoxMergeRules.MinCardCounter
                && slotView.AcceptedCardValue <= remainingDemand
                && CanBuildCard(visibleCards, slotView.AcceptedCardValue))
            {
                return true;
            }
        }

        for (int i = 0; i < _pendingSlotRequirements.Count; i++)
        {
            int boxSize = _pendingSlotRequirements[i].BoxSize;
            if (boxSize > FarmBoxMergeRules.MinCardCounter
                && boxSize <= remainingDemand
                && CanBuildCard(visibleCards, boxSize))
            {
                return true;
            }
        }

        return false;
    }

    private void AssignFallbackRequirement(
        FarmBoxMergeBoxSlotView slotView,
        bool needsUnlockSingle)
    {
        int fallbackSize;
        if (needsUnlockSingle)
        {
            fallbackSize = FarmBoxMergeRules.MinCardCounter;
        }
        else if (GetTotalRemainingItemDemand() > 0)
        {
            fallbackSize = GetMostBuildableFallbackSize(false);
        }
        else
        {
            fallbackSize = UnityEngine.Random.Range(
                FarmBoxMergeRules.MinCardCounter + 1,
                FarmBoxMergeRules.MaxCardCounter + 1);
        }

        ApplyRequirement(
            slotView,
            new FarmBoxMergeBoxRequirement(GetHighestDemandColor(), fallbackSize));
    }

    private int GetTotalRemainingItemDemand()
    {
        int total = 0;
        foreach (KeyValuePair<ColorType, int> entry in _remainingItemDemand)
        {
            total += Mathf.Max(0, entry.Value);
        }

        return total;
    }

    private ColorType GetHighestDemandColor()
    {
        ColorType bestColor = AllColorTypes[0];
        int bestDemand = -1;
        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            ColorType colorType = AllColorTypes[i];
            int demand = _remainingItemDemand.TryGetValue(colorType, out int value) ? value : 0;
            if (demand > bestDemand)
            {
                bestDemand = demand;
                bestColor = colorType;
            }
        }

        return bestColor;
    }

    private int GetMostBuildableFallbackSize(bool allowSingle)
    {
        int minimumSize = allowSingle
            ? FarmBoxMergeRules.MinCardCounter
            : FarmBoxMergeRules.MinCardCounter + 1;
        int bestSize = minimumSize;
        int bestScore = int.MinValue;

        for (int colorIndex = 0; colorIndex < AllColorTypes.Length; colorIndex++)
        {
            ColorType colorType = AllColorTypes[colorIndex];
            int demand = _remainingItemDemand.TryGetValue(colorType, out int remainingDemand)
                ? remainingDemand
                : 0;
            if (demand <= 0)
            {
                continue;
            }

            int[] cardsByValue = GetVisibleCardsByValue(colorType);
            int maxSize = Mathf.Min(FarmBoxMergeRules.MaxCardCounter, demand);
            for (int boxSize = minimumSize; boxSize <= maxSize; boxSize++)
            {
                int score;
                if (cardsByValue[boxSize] > 0)
                {
                    score = 200;
                }
                else if (CanBuildCard(cardsByValue, boxSize))
                {
                    score = 100;
                }
                else
                {
                    continue;
                }

                score += boxSize;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSize = boxSize;
                }
            }
        }

        return bestSize;
    }

    private int[] GetVisibleCardsByValue(ColorType colorType)
    {
        int[] cardsByValue = new int[FarmBoxMergeRules.MaxCardCounter + 1];
        foreach (Card card in _registeredCards)
        {
            if (card != null && card.CardColorType == colorType)
            {
                cardsByValue[FarmBoxMergeRules.ClampCardCounter(card.CounterValue)]++;
            }
        }

        return cardsByValue;
    }

    private static bool CanBuildCard(int[] cardsByValue, int targetValue)
    {
        int[] availableCards = (int[])cardsByValue.Clone();
        for (int value = FarmBoxMergeRules.MinCardCounter; value < targetValue; value++)
        {
            availableCards[value + 1] += availableCards[value] / 2;
        }

        return availableCards[targetValue] > 0;
    }

    private void ReserveExistingSlotRequirements()
    {
        for (int i = 0; i < _slotViews.Count; i++)
        {
            FarmBoxMergeBoxSlotView slotView = _slotViews[i];
            if (slotView == null || slotView.IsOccupied || !slotView.HasRequirement)
            {
                continue;
            }

            ReserveRequirementCapacity(slotView.AcceptedCardValue);
        }
    }

    private void ReserveRequirementCapacity(int requiredCapacity)
    {
        int clampedCapacity = FarmBoxMergeRules.ClampCardCounter(requiredCapacity);
        for (int i = 0; i < _pendingSlotRequirements.Count; i++)
        {
            if (_pendingSlotRequirements[i].BoxSize == clampedCapacity)
            {
                _pendingSlotRequirements.RemoveAt(i);
                return;
            }
        }

        List<int> reservedIndices = new List<int>();
        if (!TryFindRequirementSubset(0, clampedCapacity, reservedIndices))
        {
            return;
        }

        reservedIndices.Sort();
        for (int i = reservedIndices.Count - 1; i >= 0; i--)
        {
            _pendingSlotRequirements.RemoveAt(reservedIndices[i]);
        }
    }

    private bool TryFindRequirementSubset(
        int startIndex,
        int remainingCapacity,
        List<int> selectedIndices)
    {
        if (remainingCapacity == 0)
        {
            return true;
        }

        for (int i = startIndex; i < _pendingSlotRequirements.Count; i++)
        {
            int candidateSize = _pendingSlotRequirements[i].BoxSize;
            if (candidateSize > remainingCapacity)
            {
                continue;
            }

            selectedIndices.Add(i);
            if (TryFindRequirementSubset(i + 1, remainingCapacity - candidateSize, selectedIndices))
            {
                return true;
            }

            selectedIndices.RemoveAt(selectedIndices.Count - 1);
        }

        return false;
    }

    private void BuildRemainingItemAndCardTotals()
    {
        _remainingItemDemand.Clear();
        _remainingCardUnits.Clear();
        _registeredCards.RemoveWhere(card => card == null);

        for (int colorIndex = 0; colorIndex < AllColorTypes.Length; colorIndex++)
        {
            ColorType colorType = AllColorTypes[colorIndex];
            int itemDemand = Mathf.Max(
                0,
                _itemSpawner.GetRemainingUnplacedCount(colorType) - GetOutstandingBoxDemand(colorType));
            if (itemDemand > 0)
            {
                _remainingItemDemand[colorType] = itemDemand;
            }

            int cardUnits = cardSpawner != null
                ? cardSpawner.GetPendingLevelOneCardCount(colorType)
                : 0;
            foreach (Card card in _registeredCards)
            {
                if (card.CardColorType == colorType)
                {
                    cardUnits += FarmBoxMergeSlotPlanBuilder.GetRequiredLevelOneCardCount(card.CounterValue);
                }
            }

            if (cardUnits > 0)
            {
                _remainingCardUnits[colorType] = cardUnits;
            }
        }
    }

    private void AssignNextRequirement(
        FarmBoxMergeBoxSlotView slotView,
        bool needsUnlockSingle)
    {
        if (slotView == null || slotView.IsOccupied || slotView.HasRequirement)
        {
            return;
        }

        if (_pendingSlotRequirements.Count == 0)
        {
            slotView.ClearRequirement();
            return;
        }

        int requirementIndex = SelectBestRequirementIndex(needsUnlockSingle);
        if (requirementIndex < 0)
        {
            return;
        }

        FarmBoxMergeBoxRequirement requirement = _pendingSlotRequirements[requirementIndex];
        _pendingSlotRequirements.RemoveAt(requirementIndex);

        ApplyRequirement(slotView, requirement);
    }

    private void ApplyRequirement(
        FarmBoxMergeBoxSlotView slotView,
        FarmBoxMergeBoxRequirement requirement)
    {
        BoxPatternDefinition pattern = ResolvePattern(requirement.BoxSize);
        ApplyRequirement(slotView, requirement, pattern);
    }

    private void ApplyAuthoredRequirement(
        FarmBoxMergeBoxSlotView slotView,
        FarmBoxMergeBoxSlotPlanEntry entry)
    {
        FarmBoxMergeBoxRequirement requirement = new FarmBoxMergeBoxRequirement(
            entry.intendedColor,
            entry.boxSize);
        BoxPatternDefinition pattern = BoxPatternLibrary.ResolveAuthored(
            requirement.BoxSize,
            entry.fourBoxPatternVariant);
        ApplyRequirement(slotView, requirement, pattern);
    }

    private void ApplyRequirement(
        FarmBoxMergeBoxSlotView slotView,
        FarmBoxMergeBoxRequirement requirement,
        BoxPatternDefinition pattern)
    {
        Vector3[] localPositions = GetCenteredLocalPositions(pattern.Cells);
        Color previewColor = Color.white;
        previewColor.a = slotPreviewBaseColor.a;
        slotView.SetRequirement(
            requirement,
            pattern,
            localPositions,
            spawnHeightOffset,
            _boxFactory,
            GetOrCreateSlotPreviewMaterial(),
            previewColor);
    }

    private int SelectBestRequirementIndex(bool needsUnlockSingle)
    {
        int bestScore = int.MinValue;
        List<int> candidates = new List<int>();

        for (int i = 0; i < _pendingSlotRequirements.Count; i++)
        {
            FarmBoxMergeBoxRequirement requirement = _pendingSlotRequirements[i];
            bool isSingle = requirement.BoxSize == FarmBoxMergeRules.MinCardCounter;
            if (needsUnlockSingle != isSingle)
            {
                continue;
            }

            int score = (GetVisibleCardReadinessScore(requirement.BoxSize) * 10)
                + requirement.BoxSize;
            if (score > bestScore)
            {
                bestScore = score;
                candidates.Clear();
                candidates.Add(i);
            }
            else if (score == bestScore)
            {
                candidates.Add(i);
            }
        }

        return candidates.Count == 0
            ? -1
            : candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private int GetVisibleCardReadinessScore(int boxSize)
    {
        _registeredCards.RemoveWhere(card => card == null);
        bool canBuild = false;
        for (int colorIndex = 0; colorIndex < AllColorTypes.Length; colorIndex++)
        {
            int[] cardsByValue = new int[FarmBoxMergeRules.MaxCardCounter + 1];
            foreach (Card card in _registeredCards)
            {
                if (card.CardColorType == AllColorTypes[colorIndex])
                {
                    cardsByValue[FarmBoxMergeRules.ClampCardCounter(card.CounterValue)]++;
                }
            }

            if (cardsByValue[boxSize] > 0)
            {
                return 20;
            }

            for (int value = FarmBoxMergeRules.MinCardCounter; value < boxSize; value++)
            {
                cardsByValue[value + 1] += cardsByValue[value] / 2;
            }

            canBuild |= cardsByValue[boxSize] > 0;
        }

        return canBuild ? 10 : 0;
    }

    private static void ShuffleRequirements(List<FarmBoxMergeBoxRequirement> requirements)
    {
        for (int i = requirements.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (requirements[i], requirements[swapIndex]) = (requirements[swapIndex], requirements[i]);
        }
    }

    private static void ShuffleSlotViews(List<FarmBoxMergeBoxSlotView> slotViews)
    {
        for (int i = slotViews.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (slotViews[i], slotViews[swapIndex]) = (slotViews[swapIndex], slotViews[i]);
        }
    }

    private void RefreshSlotPreviews(
        int draggedCardValue = 0,
        bool isDragging = false)
    {
        EnsureSlotViews();
        for (int i = 0; i < _slotViews.Count; i++)
        {
            _slotViews[i]?.SetDragHighlight(draggedCardValue, isDragging);
        }
    }

    private Material GetOrCreateSlotPreviewMaterial()
    {
        if (_slotPreviewMaterial != null)
        {
            return _slotPreviewMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("FarmBoxMerge: URP Lit shader bulunamadığı için kutu slot önizlemeleri oluşturulamadı.");
            return null;
        }

        _slotPreviewMaterial = new Material(shader)
        {
            name = "FarmBoxMerge Slot Preview (Runtime)",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Transparent
        };
        _slotPreviewMaterial.SetOverrideTag("RenderType", "Transparent");
        _slotPreviewMaterial.SetFloat("_Surface", 1f);
        _slotPreviewMaterial.SetFloat("_Blend", 0f);
        _slotPreviewMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        _slotPreviewMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        _slotPreviewMaterial.SetFloat("_ZWrite", 0f);
        _slotPreviewMaterial.SetFloat("_Metallic", 0f);
        _slotPreviewMaterial.SetFloat("_Smoothness", 0.18f);
        _slotPreviewMaterial.SetColor("_BaseColor", slotPreviewBaseColor);
        _slotPreviewMaterial.SetColor("_Color", slotPreviewBaseColor);
        _slotPreviewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _slotPreviewMaterial.DisableKeyword("_ALPHATEST_ON");
        return _slotPreviewMaterial;
    }

    private bool IsSpawnPointOccupied(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return false;
        }

        for (int i = 0; i < spawnPoint.childCount; i++)
        {
            if (spawnPoint.GetChild(i).GetComponent<MergeBoxParent>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureSpawnPoints()
    {
        EnsureSpawnSlotRoot();

        if (spawnSlotRoot == null)
        {
            return;
        }

        spawnPoints.RemoveAll(point => point == null);

        if (spawnPoints.Count == 0)
        {
            for (int i = 0; i < spawnSlotRoot.childCount; i++)
            {
                spawnPoints.Add(spawnSlotRoot.GetChild(i));
            }
        }

        if (spawnPoints.Count > 0 || !createRuntimeSpawnPoints)
        {
            return;
        }

        int pointCount = Mathf.Max(1, runtimeSpawnPointCount);
        float halfWidth = (pointCount - 1) * runtimeSpawnPointSpacing * 0.5f;

        for (int i = 0; i < pointCount; i++)
        {
            GameObject spawnPointObject = new GameObject($"SpawnPoint_{i + 1:00}");
            Transform pointTransform = spawnPointObject.transform;
            pointTransform.SetParent(spawnSlotRoot, false);
            pointTransform.localPosition = new Vector3((i * runtimeSpawnPointSpacing) - halfWidth, 0f, 0f);
            pointTransform.localRotation = Quaternion.identity;
            pointTransform.localScale = Vector3.one;
            spawnPoints.Add(pointTransform);
        }
    }

    private void EnsureSpawnSlotRoot()
    {
        if (spawnSlotRoot != null)
        {
            return;
        }

        Transform parentTransform = spawnSurface != null ? spawnSurface : transform;
        Transform existingRoot = parentTransform.Find("SpawnedBoxGroups");
        if (existingRoot != null)
        {
            spawnSlotRoot = existingRoot;
            return;
        }

        GameObject spawnRoot = new GameObject("SpawnedBoxGroups");
        spawnSlotRoot = spawnRoot.transform;
        spawnSlotRoot.SetParent(parentTransform, false);

        if (TryGetDefaultSpawnSlotRootPose(out Vector3 worldPosition, out Quaternion worldRotation))
        {
            spawnSlotRoot.SetPositionAndRotation(worldPosition, worldRotation);
        }
        else
        {
            spawnSlotRoot.localPosition = Vector3.zero;
            spawnSlotRoot.localRotation = Quaternion.identity;
        }

        spawnSlotRoot.localScale = Vector3.one;
    }

    private bool TryGetDefaultSpawnSlotRootPose(out Vector3 worldPosition, out Quaternion worldRotation)
    {
        Camera dropCamera = ResolveWorldDropCamera();
        Transform surface = spawnSurface != null ? spawnSurface : transform;

        if (dropCamera == null || surface == null)
        {
            worldPosition = default;
            worldRotation = default;
            return false;
        }

        Vector2 viewportCenter = centerDropViewportRect.center;
        Vector3 screenPoint = dropCamera.ViewportToScreenPoint(new Vector3(viewportCenter.x, viewportCenter.y, 0f));
        Plane dropPlane = new Plane(surface.up, surface.position);
        Ray dropRay = dropCamera.ScreenPointToRay(screenPoint);

        if (!dropPlane.Raycast(dropRay, out float hitDistance))
        {
            worldPosition = default;
            worldRotation = default;
            return false;
        }

        worldPosition = dropRay.GetPoint(hitDistance);
        worldRotation = surface.rotation;
        return true;
    }

    private Camera ResolveCanvasEventCamera()
    {
        if (rootCanvas != null)
        {
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            if (rootCanvas.worldCamera != null)
            {
                return rootCanvas.worldCamera;
            }
        }

        return Camera.main;
    }

    private Camera ResolveWorldDropCamera()
    {
        if (worldDropCamera != null)
        {
            return worldDropCamera;
        }

        if (rootCanvas != null &&
            rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay &&
            rootCanvas.worldCamera != null)
        {
            return rootCanvas.worldCamera;
        }

        return Camera.main;
    }

}
