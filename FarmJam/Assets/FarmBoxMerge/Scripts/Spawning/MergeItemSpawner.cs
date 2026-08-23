using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MergeItemSpawner : MonoBehaviour
{
    public static MergeItemSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MergeItem itemPrefab;
    [SerializeField] private Transform itemsRoot;
    [SerializeField] private Transform queuePointRoot;

    [Header("Queue Points")]
    [SerializeField] private List<Transform> queuePoints = new List<Transform>();
    [SerializeField, Min(1)] private int maxVisibleItems = 6;
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

    [Header("Runtime")]
    [SerializeField] private List<MergeItem> spawnedItems = new List<MergeItem>();

    public IReadOnlyList<MergeItem> SpawnedItems => spawnedItems;
    public int VisibleQueueCapacity => Mathf.Min(queuePoints.Count, Mathf.Max(1, maxVisibleItems));
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
    public bool IsNextQueuedItemBlocked
    {
        get
        {
            CleanupNullItems();
            if (spawnedItems.Count == 0 || spawnedItems[0] == null)
            {
                return false;
            }

            return !BoxRegistry.TryFindAvailable(spawnedItems[0].ColorType, out _);
        }
    }

    public event Action ItemCountChanged;

    private Coroutine _processRoutine;
    private readonly List<ColorType> _lastInitialColors = new List<ColorType>();
    private readonly HashSet<MergeItem> _activeItems = new HashSet<MergeItem>();
    private readonly Queue<ColorType> _pendingColors = new Queue<ColorType>();

    private void Reset()
    {
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Birden fazla MergeItemSpawner bulundu. Son bulunan instance kullanilacak.", this);
        }

        Instance = this;
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
        RegisterExistingItems();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
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
                }
            }
        }

        FillVisibleQueue();
        ItemCountChanged?.Invoke();
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
        if (!spawnOnStart)
        {
            return;
        }

        int count = Mathf.Max(0, initialSpawnCount);
        for (int i = 0; i < count; i++)
        {
            ColorType colorType = GetRandomColorType();
            _lastInitialColors.Add(colorType);
            _pendingColors.Enqueue(colorType);
        }

        FillVisibleQueue();
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
        for (int i = 0; i < _lastInitialColors.Count; i++)
        {
            _pendingColors.Enqueue(_lastInitialColors[i]);
        }

        FillVisibleQueue();
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
        ItemCountChanged?.Invoke();
    }

    public void SpawnRandomItems(int count)
    {
        int itemCount = Mathf.Max(0, count);
        for (int i = 0; i < itemCount; i++)
        {
            _pendingColors.Enqueue(GetRandomColorType());
        }

        FillVisibleQueue();
        ItemCountChanged?.Invoke();
        TryProcessQueue();
    }

    public MergeItem SpawnItem(ColorType colorType)
    {
        if (_pendingColors.Count > 0 || !CanSpawnVisibleItem())
        {
            _pendingColors.Enqueue(colorType);
            ItemCountChanged?.Invoke();
            TryProcessQueue();
            return null;
        }

        MergeItem spawnedItem = SpawnVisibleItem(colorType);
        TryProcessQueue();
        return spawnedItem;
    }

    private MergeItem SpawnVisibleItem(ColorType colorType)
    {
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
        CleanupNullItems();

        if (itemPrefab == null)
        {
            Debug.LogWarning("MergeItemSpawner icin item prefab referansi eksik.", this);
            return null;
        }

        if (queuePoints.Count == 0)
        {
            Debug.LogWarning("MergeItemSpawner icin queue point bulunamadi.", this);
            return null;
        }

        if (spawnedItems.Count >= VisibleQueueCapacity)
        {
            Debug.LogWarning("Queue dolu. Yeni item spawnlanamadi.", this);
            return null;
        }

        MergeItem spawnedItem = Instantiate(itemPrefab, itemsRoot != null ? itemsRoot : transform);
        spawnedItem.name = $"{itemPrefab.name}_{spawnedItems.Count + 1:00}";
        RegisterActiveItem(spawnedItem);
        spawnedItem.Initialize(colorType);

        Transform targetPoint = GetQueuePoint(spawnedItems.Count);
        if (targetPoint != null)
        {
            spawnedItem.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
        }

        spawnedItems.Add(spawnedItem);
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
                FillVisibleQueue();
                continue;
            }

            Box targetBox = FindMatchingBox(firstItem.ColorType);
            if (targetBox == null || !targetBox.TryAssignItem(firstItem))
            {
                break;
            }

            spawnedItems.RemoveAt(0);
            FillVisibleQueue();
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
        Vector3 targetPosition = targetAnchor.position;
        Quaternion targetRotation = targetAnchor.rotation;

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
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        targetBox.NotifyItemSettled();
    }

    private IEnumerator RepositionQueueItems()
    {
        CleanupNullItems();
        if (spawnedItems.Count == 0 || queuePoints.Count == 0)
        {
            yield break;
        }

        List<MergeItem> itemsToMove = new List<MergeItem>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Quaternion> startRotations = new List<Quaternion>();
        List<Vector3> targetPositions = new List<Vector3>();
        List<Quaternion> targetRotations = new List<Quaternion>();

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

            itemsToMove.Add(item);
            startPositions.Add(item.transform.position);
            startRotations.Add(item.transform.rotation);
            targetPositions.Add(targetPoint.position);
            targetRotations.Add(targetPoint.rotation);
        }

        if (itemsToMove.Count == 0)
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

            for (int i = 0; i < itemsToMove.Count; i++)
            {
                MergeItem item = itemsToMove[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.position = Vector3.LerpUnclamped(startPositions[i], targetPositions[i], easedProgress);
                item.transform.rotation = Quaternion.Slerp(startRotations[i], targetRotations[i], easedProgress);
            }

            yield return null;
        }

        for (int i = 0; i < itemsToMove.Count; i++)
        {
            MergeItem item = itemsToMove[i];
            if (item == null)
            {
                continue;
            }

            item.transform.position = targetPositions[i];
            item.transform.rotation = targetRotations[i];
        }
    }

    private Box FindMatchingBox(ColorType colorType)
    {
        return BoxRegistry.TryFindAvailable(colorType, out Box matchingBox) ? matchingBox : null;
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

    private ColorType GetRandomColorType()
    {
        return FarmBoxMergeRandom.ColorType();
    }

    private bool CanSpawnVisibleItem()
    {
        ResolveReferences();
        EnsureRoots();
        EnsureQueuePoints();
        CleanupNullItems();
        return itemPrefab != null && VisibleQueueCapacity > 0 && spawnedItems.Count < VisibleQueueCapacity;
    }

    private void FillVisibleQueue()
    {
        while (_pendingColors.Count > 0 && CanSpawnVisibleItem())
        {
            SpawnVisibleItem(_pendingColors.Dequeue());
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
