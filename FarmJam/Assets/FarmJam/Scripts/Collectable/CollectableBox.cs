using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CollectableBox : MonoBehaviour
{
    private const int COLLECTABLE_DELAY_MS = 250;
    private const int POST_PROCESS_DELAY_MS = 100;
    private const float DESTROY_SCALE = 0.15f;
    private const float DESTROY_DURATION = 0.25f;
    private const int MAX_OVERLAP_COLLIDERS = 10;

    public Transform BoxcastStartPoint;
    public bool IsLocked;
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
            collectableParameter.Collectable.Init(ColorType);
    }

    public async UniTask FindUnitBox()
    {
        if (IsLocked) return;

        foreach (var collectableParameter in CollectableParameters)
        {
            if (collectableParameter.Collectable == null) continue;

            UnitBox unitBox = _unitBoxManager.GetUnitBox(ColorType);
            if (unitBox == null)
                return;

            UnityBoxPoint unityBoxPoint = unitBox.GetEmptyBoxPoint();
            if (unityBoxPoint == null)
                return;

            unityBoxPoint.SetCollectable(collectableParameter.Collectable);
            collectableParameter.Collectable.JumpToTarget(unityBoxPoint.transform);
            collectableParameter.Collectable = null;

            await UniTask.Delay(COLLECTABLE_DELAY_MS);
        }

        await UniTask.Delay(POST_PROCESS_DELAY_MS);
        DestroyAnimCheck();
    }

    private void DestroyAnimCheck()
    {
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
            transform.DOScale(Vector3.one * DESTROY_SCALE, DESTROY_DURATION)
                .SetEase(Ease.Linear)
                .OnComplete(() => Destroy(gameObject));
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
