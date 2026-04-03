using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FarmBlast
{
    public class CollectableBox : MonoBehaviour
{
    private const int COLLECTABLE_DELAY_MS = 70;
    private const int POST_PROCESS_DELAY_MS = 30;
    private const float DESTROY_SCALE = 0.15f;
    private const float DESTROY_DURATION = 0.18f;
    private const int MAX_OVERLAP_COLLIDERS = 10;

    public Transform BoxcastStartPoint;
    public bool IsLocked;
    public bool IsDestroying { get; private set; }
    public List<CollectableParameter> CollectableParameters;

    private UnitBoxManager _unitBoxManager;
    private Collider[] _overlapResults;

    public ColorType ColorType;

    public void Init(UnitBoxManager unitBoxManager)
    {
        _unitBoxManager = unitBoxManager;
        _overlapResults = new Collider[MAX_OVERLAP_COLLIDERS];
        SetColor();
    }

    public void SetColor()
    {
        foreach (CollectableParameter collectableParameter in CollectableParameters)
        {
            if (collectableParameter.Collectable != null)
            {
                collectableParameter.Collectable.Init(ColorType);
            }
        }
    }

    public async UniTask<bool> FindUnitBox()
    {
        if (this == null || IsLocked || IsDestroying) return false;

        bool movedAnyCollectable = false;
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            foreach (var collectableParameter in CollectableParameters)
            {
                if (this == null || IsDestroying)
                    return movedAnyCollectable;

                if (collectableParameter.Collectable == null) continue;

                UnitBox unitBox = _unitBoxManager.GetUnitBox(ColorType);
                if (unitBox == null)
                    return movedAnyCollectable;

                UnityBoxPoint unityBoxPoint = unitBox.GetEmptyBoxPoint();
                if (unityBoxPoint == null)
                    return movedAnyCollectable;

                Collectable collectable = collectableParameter.Collectable;
                if (collectable == null)
                    continue;

                unityBoxPoint.SetCollectable(collectable);
                await collectable.JumpToTarget(unityBoxPoint.transform);

                if (this == null || IsDestroying)
                    return movedAnyCollectable;

                collectableParameter.Collectable = null;
                movedAnyCollectable = true;

                await UniTask.Delay(COLLECTABLE_DELAY_MS, cancellationToken: cancellationToken);
            }

            await UniTask.Delay(POST_PROCESS_DELAY_MS, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return movedAnyCollectable;
        }

        if (this == null || IsDestroying)
            return movedAnyCollectable;

        DestroyAnimCheck();
        return movedAnyCollectable;
    }

    private void DestroyAnimCheck()
    {
        if (this == null || IsDestroying)
            return;

        bool isEmpty = true;
        foreach (var collectableParameter in CollectableParameters)
        {
            if (collectableParameter.Collectable != null)
            {
                isEmpty = false;
                break;
            }
        }

        if (isEmpty)
        {
            IsDestroying = true;
            transform.DOKill();
            transform.DOScale(Vector3.one * DESTROY_SCALE, DESTROY_DURATION)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (this != null)
                    {
                        Destroy(gameObject);
                    }
                });
        }
    }
    
    [ContextMenu(" Set Color")]
    public void CheckIsLocked()
    {
        if (_overlapResults == null)
            _overlapResults = new Collider[MAX_OVERLAP_COLLIDERS];

        Vector3 halfExtents = lockBoxScale;
        Vector3 boxCenter = BoxcastStartPoint.position;

        int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, _overlapResults, Quaternion.identity);

        bool hasBoxAbove = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (_overlapResults[i] != null && _overlapResults[i].gameObject != gameObject)
            {
                hasBoxAbove = true;
                break;
            }
        }

        IsLocked = hasBoxAbove;
    }
        
    public Vector3 lockBoxScale = Vector3.one;

    private void OnDrawGizmos()
    {
        if (BoxcastStartPoint == null) return;

        Gizmos.color = Color.blue;
        Vector3 boxCenter = BoxcastStartPoint.position;
        Gizmos.DrawWireCube(boxCenter, lockBoxScale);
    }
}

[Serializable]
public class CollectableParameter
{
    public Transform Point;
    public Collectable Collectable;
}
}

