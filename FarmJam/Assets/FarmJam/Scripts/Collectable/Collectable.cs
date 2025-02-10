using DG.Tweening;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    public void JumpToTarget(Transform target)
    {
        /*transform.parent = target;
        transform.DOLocalJump(Vector3.zero, 1, 1, .5f).SetEase(Ease.Linear);*/
        
        // İlk önce objeyi hedefin ters yönüne çekiyoruz
        Vector3 backwardPos = transform.position + (transform.position - target.position).normalized * 0.5f;

        // Önce geri git, sonra hedefe atla
        transform.DOMove(backwardPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.parent = target;
                transform.DOLocalJump(Vector3.zero, 1, 1, 0.5f)
                    .SetEase(Ease.OutBack);
            });
        
    }
}