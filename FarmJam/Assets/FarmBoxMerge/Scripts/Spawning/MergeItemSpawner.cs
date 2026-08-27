using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
public class MergeItemSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform itemsRoot;
    [SerializeField] private Transform queuePointRoot;

    [Header("Queue Points")]
    [SerializeField] private List<Transform> queuePoints = new List<Transform>();
    [SerializeField] private bool createRuntimeQueuePoints = true;
    [SerializeField] private int runtimeQueuePointCount = 20;
    [SerializeField] private float runtimeQueuePointSpacing = 1.1f;

    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart;
    [SerializeField] private int initialSpawnCount = 8;

    [Header("Animation")]
    [SerializeField] private float queueMoveDuration = 0.1f;
    [SerializeField] private float jumpDuration = 0.22f;
    [SerializeField] private float jumpHeight = 0.95f;
    [SerializeField] private Vector3 queueItemEulerAngles = new Vector3(30f, 30f, 0f);
    [SerializeField] private Vector3 boxItemEulerAngles = Vector3.zero;
    [SerializeField] private float boxItemFloorHeight = 0.1f;

    [Header("Runtime")]
    [SerializeField] private List<MergeItem> spawnedItems = new List<MergeItem>();

    public IReadOnlyList<MergeItem> SpawnedItems => spawnedItems;
    public int QueueCapacity => queuePoints.Count;
    public int ActiveItemCount
    {
        get
        {
            CleanupActiveItems();
            return _activeItems.Count;
        }
    }
    public int RemainingItemCount => ActiveItemCount + _pendingColors.Count;
    public bool HasRemainingItems => RemainingItemCount > 0;
    public int RemainingUnplacedItemCount
    {
        get
        {
            int total = 0;
            foreach (KeyValuePair<ColorType, int> entry in _remainingUnplacedByColor)
            {
                total += Mathf.Max(0, entry.Value);
            }

            return total;
        }
    }
    public bool HasUnplacedItems => RemainingUnplacedItemCount > 0;
    public bool IsNextQueuedItemBlocked
    {
        get
        {
            CleanupNullItems();
            if (spawnedItems.Count == 0 || spawnedItems[0] == null)
            {
                return false;
            }

            return _boxRegistry == null || !_boxRegistry.TryFindAvailable(spawnedItems[0].ColorType, out _);
        }
    }

    public event Action ItemCountChanged;
    public event Action RemainingColorCountsChanged;

    private Coroutine _processRoutine;
    private readonly List<ColorType> _lastInitialColors = new List<ColorType>();
    private readonly HashSet<MergeItem> _activeItems = new HashSet<MergeItem>();
    private readonly Queue<ColorType> _pendingColors = new Queue<ColorType>();
    private readonly Dictionary<ColorType, int> _remainingUnplacedByColor = new Dictionary<ColorType, int>();
    private readonly List<MergeItem> _queueMoveItems = new List<MergeItem>(20);
    private readonly List<Vector3> _queueMoveStartPositions = new List<Vector3>(20);
    private readonly List<Quaternion> _queueMoveStartRotations = new List<Quaternion>(20);
    private readonly List<Vector3> _queueMoveTargetPositions = new List<Vector3>(20);
    private readonly List<Quaternion> _queueMoveTargetRotations = new List<Quaternion>(20);
    private IMergeItemFactory _itemFactory;
    private IFarmBoxMergeBoxRegistry _boxRegistry;
    private IFarmBoxMergeFeedbackService _feedback;
    private bool _initialized;

    [Inject]
    public void Construct(
        IMergeItemFactory itemFactory,
        IFarmBoxMergeBoxRegistry boxRegistry,
        IFarmBoxMergeFeedbackService feedback)
    {
        _itemFactory = itemFactory;
        _boxRegistry = boxRegistry;
        _feedback = feedback;
    }

    private void Reset()
    {
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
        RegisterExistingItems();

        if (spawnOnStart)
        {
            SpawnInitialItems();
        }

        TryProcessQueue();
    }

    public void SetSpawnOnStart(bool shouldSpawnOnStart)
    {
        spawnOnStart = shouldSpawnOnStart;
    }

    public void SpawnLevelItems(IReadOnlyList<FarmBoxMergeItemRun> itemSequence)
    {
        _lastInitialColors.Clear();
        _pendingColors.Clear();
        _remainingUnplacedByColor.Clear();

        if (itemSequence != null)
        {
            for (int i = 0; i < itemSequence.Count; i++)
            {
                FarmBoxMergeItemRun itemRun = itemSequence[i];
                if (itemRun == null)
                {
                    continue;
                }

                int count = Mathf.Max(1, itemRun.count);
                for (int j = 0; j < count; j++)
                {
                    _lastInitialColors.Add(itemRun.colorType);
                    _pendingColors.Enqueue(itemRun.colorType);
                    AddRemainingColor(itemRun.colorType);
                }
            }
        }

        FillQueueToCapacity();
        ItemCountChanged?.Invoke();
        RemainingColorCountsChanged?.Invoke();
        TryProcessQueue();
    }

    [ContextMenu("Spawn Random Item")]
    public void SpawnRandomItem()
    {
        SpawnItem(GetRandomColorType());
    }

    [ContextMenu("Spawn Initial Items")]
    public void SpawnRandomItems()
    {
        SpawnRandomItems(initialSpawnCount);
    }

    public void SpawnInitialItems()
    {
        _lastInitialColors.Clear();
        _pendingColors.Clear();
        _remainingUnplacedByColor.Clear();
        if (!spawnOnStart)
        {
            RemainingColorCountsChanged?.Invoke();
            return;
        }

        int count = Mathf.Max(0, initialSpawnCount);
        for (int i = 0; i < count; i++)
        {
            ColorType colorType = GetRandomColorType();
            _lastInitialColors.Add(colorType);
            _pendingColors.Enqueue(colorType);
            AddRemainingColor(colorType);
        }

        FillQueueToCapacity();
        RemainingColorCountsChanged?.Invoke();
        TryProcessQueue();
    }

    public void ReplayInitialItems()
    {
        if (_lastInitialColors.Count == 0)
        {
            SpawnInitialItems();
            return;
        }

        _pendingColors.Clear();
        _remainingUnplacedByColor.Clear();
        for (int i = 0; i < _lastInitialColors.Count; i++)
        {
            _pendingColors.Enqueue(_lastInitialColors[i]);
            AddRemainingColor(_lastInitialColors[i]);
        }

        FillQueueToCapacity();
        RemainingColorCountsChanged?.Invoke();
        TryProcessQueue();
    }

    [ContextMenu("Clear Spawned Items")]
    public void ClearSpawnedItems()
    {
        StopProcessing();

        MergeItem[] itemsToDestroy = itemsRoot != null
            ? itemsRoot.GetComponentsInChildren<MergeItem>(true)
            : spawnedItems.ToArray();

        for (int i = itemsToDestroy.Length - 1; i >= 0; i--)
        {
            MergeItem item = itemsToDestroy[i];
            if (item == null)
            {
                continue;
            }

            FarmBoxMergeObjectUtility.Destroy(item.gameObject);
        }

        spawnedItems.Clear();
        _activeItems.Clear();
        _pendingColors.Clear();
        _remainingUnplacedByColor.Clear();
        ItemCountChanged?.Invoke();
        RemainingColorCountsChanged?.Invoke();
    }

    public void SpawnRandomItems(int count)
    {
        int itemCount = Mathf.Max(0, count);
        for (int i = 0; i < itemCount; i++)
        {
            ColorType colorType = GetRandomColorType();
            _pendingColors.Enqueue(colorType);
            AddRemainingColor(colorType);
        }

        FillQueueToCapacity();
        ItemCountChanged?.Invoke();
        RemainingColorCountsChanged?.Invoke();
        TryProcessQueue();
    }

    public MergeItem SpawnItem(ColorType colorType)
    {
        if (_pendingColors.Count > 0 || !CanSpawnQueuedItem())
        {
            _pendingColors.Enqueue(colorType);
            AddRemainingColor(colorType);
            ItemCountChanged?.Invoke();
            RemainingColorCountsChanged?.Invoke();
            TryProcessQueue();
            return null;
        }

        MergeItem spawnedItem = SpawnQueuedItem(colorType);
        if (spawnedItem != null)
        {
            AddRemainingColor(colorType);
            RemainingColorCountsChanged?.Invoke();
        }
        TryProcessQueue();
        return spawnedItem;
    }

    private MergeItem SpawnQueuedItem(ColorType colorType)
    {
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
        CleanupNullItems();

        if (_itemFactory == null)
        {
            Debug.LogWarning("Merge item factory is not available. Check FarmBoxMergeLifetimeScope.", this);
            return null;
        }

        if (queuePoints.Count == 0)
        {
            Debug.LogWarning("MergeItemSpawner icin queue point bulunamadi.", this);
            return null;
        }

        if (spawnedItems.Count >= QueueCapacity)
        {
            Debug.LogWarning("Queue dolu. Yeni item spawnlanamadi.", this);
            return null;
        }

        MergeItem spawnedItem = _itemFactory.Create(itemsRoot != null ? itemsRoot : transform);
        if (spawnedItem == null)
        {
            return null;
        }

        spawnedItem.name = $"MergeItem_{spawnedItems.Count + 1:00}";
        RegisterActiveItem(spawnedItem);
        spawnedItem.Initialize(colorType);

        // The queue is filled from front to back. Once it is full, removing the
        // front item leaves exactly one free slot at the final (off-camera) point,
        // so replenishment is never visibly instantiated in the play area.
        Transform targetPoint = GetQueuePoint(spawnedItems.Count);
        if (targetPoint != null)
        {
            spawnedItem.transform.SetPositionAndRotation(targetPoint.position, GetQueueItemRotation(targetPoint));
        }

        spawnedItems.Add(spawnedItem);
        _feedback?.PlayItemSpawn(spawnedItem.transform, colorType);
        return spawnedItem;
    }

    public void TryProcessQueue()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _processRoutine != null)
        {
            return;
        }

        _processRoutine = StartCoroutine(ProcessQueueRoutine());
    }

    private IEnumerator ProcessQueueRoutine()
    {
        yield return RepositionQueueItems();

        while (true)
        {
            CleanupNullItems();
            if (spawnedItems.Count == 0)
            {
                break;
            }

            MergeItem firstItem = spawnedItems[0];
            if (firstItem == null)
            {
                spawnedItems.RemoveAt(0);
                FillQueueToCapacity();
                continue;
            }

            Box targetBox = FindMatchingBox(firstItem.ColorType);
            if (targetBox == null || !targetBox.TryAssignItem(firstItem))
            {
                break;
            }

            spawnedItems.RemoveAt(0);
            FillQueueToCapacity();
            yield return AnimateItemIntoBox(firstItem, targetBox);
            yield return RepositionQueueItems();
        }

        _processRoutine = null;
    }

    private IEnumerator AnimateItemIntoBox(MergeItem item, Box targetBox)
    {
        if (item == null || targetBox == null)
        {
            yield break;
        }

        Transform targetAnchor = targetBox.CollectableRoot;
        if (targetAnchor == null)
        {
            targetBox.ClearAssignedItem(item);
            yield break;
        }

        if (itemsRoot != null)
        {
            item.transform.SetParent(itemsRoot, true);
        }

        Vector3 startPosition = item.transform.position;
        Quaternion startRotation = item.transform.rotation;
        Quaternion targetLocalRotation = Quaternion.Euler(boxItemEulerAngles);
        Vector3 targetLocalPosition = Vector3.up * (boxItemFloorHeight - item.GetVisualBottomLocalY());
        Vector3 targetPosition = targetAnchor.TransformPoint(targetLocalPosition);
        Quaternion targetRotation = targetAnchor.rotation * targetLocalRotation;

        float duration = Mathf.Max(0.01f, jumpDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (item == null || targetBox == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = FarmBoxMergeMath.EaseOutCubic(progress);
            Vector3 position = Vector3.LerpUnclamped(startPosition, targetPosition, easedProgress);
            position += Vector3.up * (4f * jumpHeight * progress * (1f - progress));

            item.transform.position = position;
            item.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedProgress);
            yield return null;
        }

        if (item == null || targetBox == null)
        {
            yield break;
        }

        item.transform.SetParent(targetAnchor, false);
        item.transform.localPosition = targetLocalPosition;
        item.transform.localRotation = targetLocalRotation;
        NotifyItemPlaced(item.ColorType);
        _feedback?.PlayItemLanded(item.transform, item.ColorType);
        targetBox.NotifyItemSettled();
    }

    private IEnumerator RepositionQueueItems()
    {
        CleanupNullItems();
        if (spawnedItems.Count == 0 || queuePoints.Count == 0)
        {
            yield break;
        }

        _queueMoveItems.Clear();
        _queueMoveStartPositions.Clear();
        _queueMoveStartRotations.Clear();
        _queueMoveTargetPositions.Clear();
        _queueMoveTargetRotations.Clear();

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            MergeItem item = spawnedItems[i];
            if (item == null)
            {
                continue;
            }

            Transform targetPoint = GetQueuePoint(i);
            if (targetPoint == null)
            {
                continue;
            }

            if (itemsRoot != null)
            {
                item.transform.SetParent(itemsRoot, true);
            }

            _queueMoveItems.Add(item);
            _queueMoveStartPositions.Add(item.transform.position);
            _queueMoveStartRotations.Add(item.transform.rotation);
            _queueMoveTargetPositions.Add(targetPoint.position);
            _queueMoveTargetRotations.Add(GetQueueItemRotation(targetPoint));
        }

        if (_queueMoveItems.Count == 0)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, queueMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = FarmBoxMergeMath.SmoothStep(progress);

            for (int i = 0; i < _queueMoveItems.Count; i++)
            {
                MergeItem item = _queueMoveItems[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.position = Vector3.LerpUnclamped(
                    _queueMoveStartPositions[i],
                    _queueMoveTargetPositions[i],
                    easedProgress);
                item.transform.rotation = Quaternion.Slerp(
                    _queueMoveStartRotations[i],
                    _queueMoveTargetRotations[i],
                    easedProgress);
            }

            yield return null;
        }

        for (int i = 0; i < _queueMoveItems.Count; i++)
        {
            MergeItem item = _queueMoveItems[i];
            if (item == null)
            {
                continue;
            }

            item.transform.position = _queueMoveTargetPositions[i];
            item.transform.rotation = _queueMoveTargetRotations[i];
        }
    }

    private Box FindMatchingBox(ColorType colorType)
    {
        return _boxRegistry != null && _boxRegistry.TryFindAvailable(colorType, out Box matchingBox)
            ? matchingBox
            : null;
    }

    private void RegisterExistingItems()
    {
        CleanupNullItems();

        if (itemsRoot == null)
        {
            return;
        }

        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            Transform child = itemsRoot.GetChild(i);
            if (!child.TryGetComponent(out MergeItem mergeItem))
            {
                continue;
            }

            if (!spawnedItems.Contains(mergeItem))
            {
                spawnedItems.Add(mergeItem);
            }

            RegisterActiveItem(mergeItem);
        }
    }

    internal void UnregisterActiveItem(MergeItem item)
    {
        if (_activeItems.Remove(item))
        {
            ItemCountChanged?.Invoke();
        }
    }

    private void RegisterActiveItem(MergeItem item)
    {
        if (item == null)
        {
            return;
        }

        item.AssignSpawner(this);
        if (_activeItems.Add(item))
        {
            ItemCountChanged?.Invoke();
        }
    }

    private void ResolveReferences()
    {
        if (itemsRoot == null)
        {
            Transform existingItemsRoot = transform.Find("SpawnedItems");
            if (existingItemsRoot != null)
            {
                itemsRoot = existingItemsRoot;
            }
        }

        if (queuePointRoot == null)
        {
            Transform existingQueueRoot = transform.Find("ItemQueuePoints");
            if (existingQueueRoot != null)
            {
                queuePointRoot = existingQueueRoot;
            }
        }

        queuePoints.RemoveAll(point => point == null);
    }

    private void EnsureRoots()
    {
        if (itemsRoot == null)
        {
            GameObject itemsRootObject = new GameObject("SpawnedItems");
            itemsRoot = itemsRootObject.transform;
            itemsRoot.SetParent(transform, false);
            itemsRoot.localPosition = Vector3.zero;
            itemsRoot.localRotation = Quaternion.identity;
            itemsRoot.localScale = Vector3.one;
        }

        if (queuePointRoot == null)
        {
            GameObject queueRootObject = new GameObject("ItemQueuePoints");
            queuePointRoot = queueRootObject.transform;
            queuePointRoot.SetParent(transform, false);
            queuePointRoot.localPosition = Vector3.zero;
            queuePointRoot.localRotation = Quaternion.identity;
            queuePointRoot.localScale = Vector3.one;
        }
    }

    private void EnsureQueuePoints()
    {
        if (queuePointRoot == null)
        {
            return;
        }

        queuePoints.RemoveAll(point => point == null);

        if (queuePoints.Count == 0)
        {
            for (int i = 0; i < queuePointRoot.childCount; i++)
            {
                queuePoints.Add(queuePointRoot.GetChild(i));
            }
        }

        if (queuePoints.Count > 0 || !createRuntimeQueuePoints)
        {
            return;
        }

        int pointCount = Mathf.Max(1, runtimeQueuePointCount);
        float halfWidth = (pointCount - 1) * runtimeQueuePointSpacing * 0.5f;

        for (int i = 0; i < pointCount; i++)
        {
            GameObject pointObject = new GameObject($"QueuePoint_{i + 1:00}");
            Transform pointTransform = pointObject.transform;
            pointTransform.SetParent(queuePointRoot, false);
            pointTransform.localPosition = new Vector3((i * runtimeQueuePointSpacing) - halfWidth, 0f, 0f);
            pointTransform.localRotation = Quaternion.identity;
            pointTransform.localScale = Vector3.one;
            queuePoints.Add(pointTransform);
        }
    }

    private Transform GetQueuePoint(int index)
    {
        if (queuePoints.Count == 0)
        {
            return null;
        }

        int clampedIndex = Mathf.Clamp(index, 0, queuePoints.Count - 1);
        return queuePoints[clampedIndex];
    }

    private Quaternion GetQueueItemRotation(Transform queuePoint)
    {
        return queuePoint.rotation * Quaternion.Euler(queueItemEulerAngles);
    }

    private ColorType GetRandomColorType()
    {
        return FarmBoxMergeRandom.ColorType();
    }

    public int GetRemainingUnplacedCount(ColorType colorType)
    {
        return _remainingUnplacedByColor.TryGetValue(colorType, out int count)
            ? Mathf.Max(0, count)
            : 0;
    }

    public bool IsColorUsedInCurrentSequence(ColorType colorType)
    {
        return _remainingUnplacedByColor.ContainsKey(colorType);
    }

    private void AddRemainingColor(ColorType colorType)
    {
        _remainingUnplacedByColor.TryGetValue(colorType, out int currentCount);
        _remainingUnplacedByColor[colorType] = currentCount + 1;
    }

    public void NotifyItemPlaced(ColorType colorType)
    {
        if (!_remainingUnplacedByColor.TryGetValue(colorType, out int currentCount) || currentCount <= 0)
        {
            return;
        }

        _remainingUnplacedByColor[colorType] = currentCount - 1;
        RemainingColorCountsChanged?.Invoke();
    }

    private bool CanSpawnQueuedItem()
    {
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
        CleanupNullItems();
        return _itemFactory != null && QueueCapacity > 0 && spawnedItems.Count < QueueCapacity;
    }

    private void FillQueueToCapacity()
    {
        while (_pendingColors.Count > 0 && CanSpawnQueuedItem())
        {
            SpawnQueuedItem(_pendingColors.Dequeue());
        }
    }

    private void CleanupNullItems()
    {
        spawnedItems.RemoveAll(item => item == null);
    }

    private void CleanupActiveItems()
    {
        _activeItems.RemoveWhere(item => item == null);
    }

    private void StopProcessing()
    {
        if (_processRoutine == null)
        {
            return;
        }

        StopCoroutine(_processRoutine);
        _processRoutine = null;
    }
}
