using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using VContainer;

public class CollectableBox : MonoBehaviour
{
    public Transform BoxcastStartPoint;
    public bool IsLocked;
    [Inject] public UnitBoxManager unitBoxManager;
    public List<CollectableParameter> CollectableParameters;
    private bool jumping = false;
    private bool onDestroyed = false;

    public ColorType ColorType;

    private void Start()
    {
        SetColor(); 
    }

    [ContextMenu(" Set Color ")]
    public void SetColor()
    {
        foreach (CollectableParameter collectableParameter in CollectableParameters)
            collectableParameter.Collectable.Init(ColorType);
    }

    public async UniTask FindUnitBox()
    {
        if(IsLocked) return;
        
        foreach (var collectableParameter in CollectableParameters)
        {
            if(collectableParameter.Collectable == null) continue;
            
            UnitBox unitBox = unitBoxManager.GetUnitBox(ColorType);
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
    
    [ContextMenu(" Set Color")]
    public void CheckIsLocked()
    {
        Vector3 halfExtents = lockBoxScale; 
        Vector3 boxCenter = BoxcastStartPoint.position;

        Collider[] colliders = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.identity);

        bool hasBoxAbove = false;
            
        foreach (var collider in colliders)
        {
            if (collider != null && collider.gameObject != gameObject)
            {
                hasBoxAbove = true;
                break; //
            }
        }

        if (!hasBoxAbove)
        {
            //Debug.Log("Üzerimde başka kutu yok, kilit açıldı: " + gameObject.name);
            IsLocked = false; 
        }
        else
        {
            //Debug.Log("Üzerimde başka bir kutu var: " + gameObject.name);
            IsLocked = true; 
        }
    }
        
    public Vector3 lockBoxScale = Vector3.one;
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 halfExtents = lockBoxScale; // Kutunun yarı boyutları
        Vector3 boxCenter = BoxcastStartPoint.position;    // Kutunun merkez noktası
        Gizmos.DrawWireCube(boxCenter, halfExtents );   // Yarı boyutların tam kutu boyutuna dönüşümü
    }
    
}

[Serializable]
public class CollectableParameter
{
    public Transform Point;
    public Collectable Collectable;
}
