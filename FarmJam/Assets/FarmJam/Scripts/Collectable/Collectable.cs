using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    [SerializeField] private CollectableColorAndMesh _collectableColorAndMesh;
    
    public bool isJumping;

    [ContextMenu(" Init ")]
    public void Init(ColorType colorType)
    {
        _collectableColorAndMesh.ColorType = colorType;
        _collectableColorAndMesh.ActiveColorObject();
    }
    public async UniTask JumpToTarget(Transform target)
    {
        isJumping = true;
        Vector3 backwardPos = transform.position + (transform.position - target.position).normalized * 1f;
        
        await transform.DOMove(backwardPos, 0.1f)
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion();
        
        transform.parent = target;
        await transform.DOLocalJump(Vector3.zero, 1, 1, 0.2f)
            .SetEase(Ease.OutBack).OnComplete(()=>
            {
                isJumping = false;
                UnitBoxSignals.OnThisUnitBoxIsFullCheck?.Dispatch();
            })
            .AsyncWaitForCompletion();
    }
}
