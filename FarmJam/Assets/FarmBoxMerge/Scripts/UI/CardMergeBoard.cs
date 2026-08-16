using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CardMergeBoard : MonoBehaviour
{
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
    private readonly HashSet<Card> _registeredCards = new HashSet<Card>();

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
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private bool createRuntimeDragLayer = true;
    [SerializeField] private bool createRuntimeSpawnDropLayer = true;

    [Header("Color Palette")]
    [SerializeField] private float colorDetectionTolerance = 0.1f;
    [SerializeField] private List<ColorPaletteEntry> colorPalette = new List<ColorPaletteEntry>();

    [Header("World Drop")]
    [SerializeField] private bool spawnBoxesOnCenterDrop = true;
    [SerializeField] private Box boxPrefab;
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

    public RectTransform CardContainer => cardContainer != null ? cardContainer : cardContainer = (RectTransform)transform;
    public Camera EventCamera => ResolveCanvasEventCamera();
    public IReadOnlyList<ColorPaletteEntry> ColorPalette => colorPalette;
    public bool HasColorPalette => colorPalette != null && colorPalette.Count > 0;
    public int CardCount
    {
        get
        {
            _registeredCards.RemoveWhere(card => card == null);
            return _registeredCards.Count;
        }
    }
    public bool HasCardCapacity => CardCount < FarmBoxMergeRules.MaxCardsOnBoard;

    public event Action CardCountChanged;

    private void Reset()
    {
        ResolveReferences();
        EnsureDefaultPalette();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureDefaultPalette();
        EnsureDragLayer();
        EnsureSpawnDropLayer();
        EnsureSpawnPoints();
        RegisterExistingCards();
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
        if (card == null)
        {
            return;
        }

        ResolveReferences();
        EnsureDragLayer();
        EnsureSpawnDropLayer();
        card.PrepareForDrag(dragLayer != null ? dragLayer : CardContainer, eventData);
        UpdateDrag(card, eventData);
    }

    public void UpdateDrag(Card card, PointerEventData eventData)
    {
        if (card == null || !card.IsDragging)
        {
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
        if (card == null || card.MergeCompleted)
        {
            return;
        }

        if (TryDiscardCard(card, eventData) || TrySpawnBoxesFromCard(card, eventData))
        {
            return;
        }

        card.ReturnToOriginalSlot();
    }

    public bool TryMerge(Card draggedCard, Card targetCard)
    {
        if (draggedCard == null || targetCard == null || draggedCard == targetCard)
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
        }
    }

    public void UnregisterCard(Card card)
    {
        if (_registeredCards.Remove(card))
        {
            CardCountChanged?.Invoke();
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

    public void ClearSpawnedBoxGroups()
    {
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
        if (!spawnBoxesOnCenterDrop || card == null || boxPrefab == null)
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

        if (!TryGetAvailableSpawnPoint(out Transform availableSpawnPoint))
        {
            return false;
        }

        SpawnBoxGroup(card.CounterValue, card.CardColorType, availableSpawnPoint);
        card.ConsumeForWorldSpawn();
        return true;
    }

    private bool TryDiscardCard(Card card, PointerEventData eventData)
    {
        if (card == null || trashDropLayer == null || !IsPointerInsideUiLayer(trashDropLayer, eventData.position))
        {
            return false;
        }

        card.DiscardInto(trashDropLayer);
        return true;
    }

    private MergeBoxParent SpawnBoxGroup(int boxCount, ColorType colorType, Transform targetSpawnPoint)
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
        BoxPatternDefinition pattern = ResolvePattern(clampedBoxCount);
        Vector3[] localPositions = GetCenteredLocalPositions(pattern.Cells);
        List<Box> spawnedBoxes = new List<Box>(localPositions.Length);

        for (int i = 0; i < localPositions.Length; i++)
        {
            Box spawnedBox = Instantiate(boxPrefab, groupTransform);
            spawnedBox.transform.localPosition = localPositions[i];
            spawnedBox.transform.localRotation = Quaternion.identity;
            spawnedBoxes.Add(spawnedBox);
        }

        boxParent.Initialize(clampedBoxCount, colorType, pattern.PatternType, spawnedBoxes, pattern.Cells);
        MergeItemSpawner.Instance?.TryProcessQueue();
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

    private bool TryGetAvailableSpawnPoint(out Transform availableSpawnPoint)
    {
        EnsureSpawnPoints();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform candidate = spawnPoints[i];
            if (candidate == null || IsSpawnPointOccupied(candidate))
            {
                continue;
            }

            availableSpawnPoint = candidate;
            return true;
        }

        availableSpawnPoint = null;
        return false;
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
