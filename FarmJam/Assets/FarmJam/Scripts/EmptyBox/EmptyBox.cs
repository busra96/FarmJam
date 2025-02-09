using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class EmptyBox : MonoBehaviour
{
    public UnitBox UnitBox;
    private EmptyBoxMovement _emptyBoxMovement;
    
    public List<GridControlCollider> GridControlColliders = new List<GridControlCollider>();
    private bool _onGridTile;
    
    private bool isActive;
    private CancellationTokenSource _cancellationTokenSource;

    private void Start()
    {
        Init();
        _emptyBoxMovement.Init();
    }

    public void Init()
    {
        _emptyBoxMovement = GetComponent<EmptyBoxMovement>();
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.AddListener(OnTheBoxHasCompletedTheMovementToTheStartingPosition);
        _cancellationTokenSource = new CancellationTokenSource();
        
        transform.localScale = Vector3.one * 0.7f;
        transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.5f).SetEase(Ease.OutBounce)
            .OnComplete(() => transform.localScale = Vector3.one * .7f);
        
        
        SelectableColliderIsActive(true);
    }

    public void Disable()
    {
        _emptyBoxMovement.Disable();
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnTheBoxHasCompletedTheMovementToTheStartingPosition);
        _cancellationTokenSource?.Cancel();
    }
    
    public void Selected(Vector3 pos)
    {
        _emptyBoxMovement.HandleMouseDown(pos);
        SelectableColliderIsActive(false);
        CheckInput().Forget();
    }

    public void Deselected(Vector3 pos)
    {
         isActive = false;
         MouseUpRaycastCheck();
    }

    private void SelectableColliderIsActive(bool isActive)
    {
        foreach (var gridControlCollider in GridControlColliders)
            gridControlCollider.gameObject.SetActive(_onGridTile ? isActive : !isActive);
    }
    
    private async UniTask CheckInput()
    {
        isActive = true;

        while (isActive && !_cancellationTokenSource.IsCancellationRequested)
        {
            RaycastCheck();
            await UniTask.Yield();
        }
    }

    public void RaycastCheck()
    {
        bool isOkey = true;
        for (int i = 0; i < GridControlColliders.Count; i++)
        {
            GridControlCollider gridControlCollider = GridControlColliders[i];
            if (gridControlCollider.ReturnOnGridTileIsAvailable() == false)
            {
                isOkey = false;
                break;
            }
        }

        for (int i = 0; i < GridControlColliders.Count; i++)
        {
            GridControlCollider gridControlCollider = GridControlColliders[i];
            if (gridControlCollider.GridTile == null)
                continue;
            
            gridControlCollider.GridTile.IsGridOkeyAndNotOkey(isOkey);
        }
     
    }

    public void MouseUpRaycastCheck()
    {
        bool isOkey = true;
        for (int i = 0; i < GridControlColliders.Count; i++)
        {
            GridControlCollider gridControlCollider = GridControlColliders[i];
            if (!gridControlCollider.ReturnOnGridTileIsAvailable())
            {
                isOkey = false;
                break;
            }
        }

        if (isOkey)
        {
            for (int i = 0; i < GridControlColliders.Count; i++)
            {
                if (GridControlColliders[i].IsMain)
                {
                   UnitBox.JumpToGridTile(GridControlColliders[i].GridTile);
                   EmptyBoxSignals.OnTheEmptyBoxRemoved?.Dispatch(this);
                   DestroySelf().Forget();
                   break;
                }
            } 
        }
        else
        {
            for (int i = 0; i < GridControlColliders.Count; i++)
            {
                GridControlCollider gridControlCollider = GridControlColliders[i];
                gridControlCollider.GridTile = null;
            }

            _emptyBoxMovement.HandleMouseUp();
        }
    }
    
    private void OnTheBoxHasCompletedTheMovementToTheStartingPosition(EmptyBoxMovement emptyBoxMovement)
    {
        if(emptyBoxMovement != _emptyBoxMovement) return;
        
        SelectableColliderIsActive(true);
    }
    
    private async UniTaskVoid DestroySelf()
    {
        if (this == null || _emptyBoxMovement == null) return;

        Disable();
        // UniTask işlemlerini durdur
        isActive = false;
        _cancellationTokenSource?.Cancel();

        // _emptyBoxMovement bileşenini devre dışı bırak
        if (_emptyBoxMovement != null)
        {
            _emptyBoxMovement.enabled = false;
        }

        // Bir frame bekleyerek UniTask süreçlerinin kapanmasını sağla
        await UniTask.DelayFrame(1);

        // Eğer nesne hala varsa yok et
        if (this != null)
        {
            Destroy(gameObject);
        }
    }
}