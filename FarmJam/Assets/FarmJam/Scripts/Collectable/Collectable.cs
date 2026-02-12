using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;
using UnityEngine.Serialization;

public class Collectable : MonoBehaviour
{
    private const float BACKWARD_DISTANCE = 1f;
    private const float BACKWARD_DURATION = 0.1f;
    private const float JUMP_HEIGHT = 1f;
    private const float JUMP_DURATION = 0.2f;
    private const float TARGET_Y_OFFSET = 0.6f;

    public CollectableAudio CollectableAudio;
    [SerializeField] private CollectableColorAndMesh _collectableColorAndMesh;
    
    public bool isJumping;

    [ContextMenu(" Init ")]
    public void Init(ColorType colorType)
    {
        _collectableColorAndMesh.ColorType = colorType;
        _collectableColorAndMesh.ActiveColorObject();
        _collectableColorAndMesh.CollectableMesh.Rotate();
    }
    public async UniTask JumpToTarget(Transform target)
    {
        isJumping = true;
        Vector3 backwardPos = transform.position + (transform.position - target.position).normalized * BACKWARD_DISTANCE;
        
        await transform.DOMove(backwardPos, BACKWARD_DURATION)
            .SetEase(Ease.OutQuad)
            .AsyncWaitForCompletion();
        CollectableAudio.PlayJumpClip();
        transform.parent = target;
        await transform.DOLocalJump(new Vector3(0, TARGET_Y_OFFSET, 0), JUMP_HEIGHT, 1, JUMP_DURATION)
            .SetEase(Ease.OutBack).OnComplete(()=>
            {
                isJumping = false;
                _collectableColorAndMesh.CollectableMesh.Rotate();
                UnitBoxSignals.OnThisUnitBoxIsFullCheck?.Dispatch();
                LevelManagerSignals.OnLevelWinFailCheckTimerRestart?.Dispatch();
            })
            .AsyncWaitForCompletion();
    }
}
