using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class TetrisSpacingLayoutManager : MonoBehaviour
{
    private const float DEFAULT_MOVE_DURATION = 0.1f;
    private const int LAYOUT_UPDATE_DELAY_FRAMES = 5;

    public List<EmptyBox> objects = new List<EmptyBox>();
    public float moveDuration = DEFAULT_MOVE_DURATION;

    private Dictionary<EmptyBox, int> removedIndexes = new Dictionary<EmptyBox, int>();

    private CancellationTokenSource cts;
    private Camera _mainCamera;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        EmptyBoxSignals.OnAddedEmptyBox.AddListener(OnSpawnedEmptyBox);
        EmptyBoxSignals.OnRemovedEmptyBox.AddListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnUpdateTetrisLayout.AddListener(OnUpdateTetrisLayout);
    }

    private void OnDisable()
    {
        EmptyBoxSignals.OnAddedEmptyBox.RemoveListener(OnSpawnedEmptyBox);
        EmptyBoxSignals.OnRemovedEmptyBox.RemoveListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnUpdateTetrisLayout.RemoveListener(OnUpdateTetrisLayout);
        cts?.Cancel();
    }

    public void ClearEmptyBoxList()
    {
        objects.Clear();
        removedIndexes.Clear();
    }

    private void OnUpdateTetrisLayout()
    {
        UpdateTetrisLayout();
    }

    private void OnSpawnedEmptyBox(EmptyBox obj)
    {
        if (objects.Contains(obj)) return;

        if (removedIndexes.TryGetValue(obj, out int previousIndex))
        {
            objects.Insert(Mathf.Min(previousIndex, objects.Count), obj);
            removedIndexes.Remove(obj);
        }
        else
        {
            objects.Add(obj);
        }

        UpdateTetrisLayout();
    }

    public async UniTask UpdateTetrisLayout()
    {
        cts = new CancellationTokenSource();
        try
        {
            await UniTask.DelayFrame(LAYOUT_UPDATE_DELAY_FRAMES, cancellationToken: cts.Token);
            UpdateLayout();
            await UniTask.DelayFrame(LAYOUT_UPDATE_DELAY_FRAMES, cancellationToken: cts.Token);
            await PositionObjectsSmooth(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation
        }
    }

    private void OnRemovedEmptyBox(EmptyBox obj)
    {
        if (!objects.Contains(obj)) return;

        int removedIndex = objects.IndexOf(obj);
        removedIndexes[obj] = removedIndex;

        objects.Remove(obj);
        UpdateTetrisLayout();
    }

    public void UpdateLayout()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("List is empty, no action taken.");
            return;
        }

        foreach (var obj in objects)
        {
            AdjustPivotToBottomCenter(obj);
        }
    }

    private void AdjustPivotToBottomCenter(EmptyBox emptyBox)
    {
        if (emptyBox == null || emptyBox.Collider == null)
        {
            Debug.LogError($"Collider not found or object is null: {emptyBox?.gameObject.name}");
            return;
        }

        Collider col = emptyBox.Collider;
        Bounds bounds = col.bounds;
        Vector3 newPivotPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        Transform parentTransform = emptyBox.transform;
        if (parentTransform == null)
        {
            Debug.LogWarning($"Parent not found: {emptyBox.gameObject.name}, no action taken.");
            return;
        }

        Vector3 offset = parentTransform.position - newPivotPosition;
        parentTransform.position -= offset;

        foreach (Transform child in parentTransform)
        {
            child.position += offset;
        }
    }

    public async UniTask PositionObjectsSmooth(CancellationToken cancellationToken)
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("List is empty, no alignment taken.");
            return;
        }

        if (_mainCamera == null) _mainCamera = Camera.main;
        float screenWorldWidth = _mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, 10)).x * 2;
        float[] widths = new float[objects.Count];
        float totalWidth = 0;

        for (int i = 0; i < objects.Count; i++)
        {
            Collider col = objects[i].Collider;
            if (col != null)
            {
                widths[i] = col.bounds.size.x;
                totalWidth += widths[i];
            }
        }

        float totalSpacing = screenWorldWidth - totalWidth;
        float spacing = totalSpacing / (objects.Count + 1);
        float currentX = -screenWorldWidth / 2 + spacing;

        List<UniTask> moveTasks = new List<UniTask>();

        for (int i = 0; i < objects.Count; i++)
        {
            float halfWidth = widths[i] / 2;
            Vector3 targetPosition = new Vector3(currentX + halfWidth, 0, -10);

            moveTasks.Add(objects[i].transform
                .DOMove(targetPosition, moveDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(UpdateType.Normal, true)
                .AsyncWaitForCompletion().AsUniTask());

            currentX += widths[i] + spacing;
        }

        await UniTask.WhenAll(moveTasks).AttachExternalCancellation(cancellationToken);
    }

    private void OnValidate()
    {
        UpdateLayout();
    }
}