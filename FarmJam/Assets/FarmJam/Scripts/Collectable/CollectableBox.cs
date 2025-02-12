using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using VContainer;

public class CollectableBox : MonoBehaviour
{
    public bool IsLock;
    [Inject] public UnitBoxManager unitBoxManager;
    public List<CollectableParameter> CollectableParameters;
    private bool jumping = false;
    private bool onDestroyed = false;

    public async UniTask FindUnitBox()
    {
        if(IsLock) return;
        
        foreach (var collectableParameter in CollectableParameters)
        {
            if(collectableParameter.Collectable == null) continue;
            
            UnitBox unitBox = unitBoxManager.GetUnitBox();
            if (unitBox == null)
                return;
            
            UnityBoxPoint unityBoxPoint = unitBox.GetEmptyBoxPoint();
            if(unityBoxPoint == null)
                return;

            unityBoxPoint.SetCollectable(collectableParameter.Collectable);
            collectableParameter.Collectable.JumpToTarget(unityBoxPoint.transform);
            collectableParameter.Collectable = null;

            await UniTask.Delay(250);
        }

        await UniTask.Delay(100);
        DestroyAnimCheck();
    }

    private bool isEmpty = true;
    private void DestroyAnimCheck()
    {
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
            transform.DOScale(Vector3.one * 0.15f, 0.25f)
                .SetEase(Ease.Linear)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}

[Serializable]
public class CollectableParameter
{
    public Transform Point;
    public Collectable Collectable;
}
