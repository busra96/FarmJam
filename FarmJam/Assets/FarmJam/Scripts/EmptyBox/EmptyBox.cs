using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class EmptyBox : MonoBehaviour
{
    public UnitBox UnitBox;
    private EmptyBoxMovement _emptyBoxMovement;
    public Collider Collider;
    
    public List<GridControlCollider> GridControlColliders = new List<GridControlCollider>();
    private bool _onGridTile;
    
    private bool isActive;
    private CancellationTokenSource _cancellationTokenSource;

    public void Init(ColorType colorType)
    {
        _emptyBoxMovement = GetComponent<EmptyBoxMovement>();
        _emptyBoxMovement.Init();
        
        _cancellationTokenSource = new CancellationTokenSource();

        UnitBox.Init(colorType);
        
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * 0.7f, .2f).SetEase(Ease.InBounce);
        
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.AddListener(OnBoxReturnedToStart);
        EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch();
        
        SetSelectableColliderActive(true);
    }

    public void Disable()
    {
        _emptyBoxMovement.Disable();
        _cancellationTokenSource?.Cancel();
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnBoxReturnedToStart);
    }
    
    public void Selected(Vector3 pos)
    {
        _emptyBoxMovement.HandleMouseDown(pos);
        SetSelectableColliderActive(false);
        TrackInput().Forget();
        
        EmptyBoxSignals.OnRemovedEmptyBox?.Dispatch(this);
    }

    public void Deselected(Vector3 pos)
    {
         isActive = false;
         HandleMouseUpRaycastCheck();
    }

    private void SetSelectableColliderActive(bool isActive)
    {
        foreach (var gridControlCollider in GridControlColliders)
            gridControlCollider.gameObject.SetActive(_onGridTile ? isActive : !isActive);
    }
    
    private async UniTask TrackInput()
    {
        isActive = true;

        while (isActive && !_cancellationTokenSource.IsCancellationRequested)
        {
            CheckRaycast();
            await UniTask.Yield();
        }
    }

    private void CheckRaycast()
    {
        bool isPlacementValid = GridControlColliders.All(collider => collider.ReturnOnGridTileIsAvailable());

        foreach (var gridControlCollider in GridControlColliders)
        {
            gridControlCollider.GridTile?.IsGridOkeyAndNotOkey(isPlacementValid);
        }
    }

    private void HandleMouseUpRaycastCheck()
    {
        bool isPlacementValid = GridControlColliders.All(collider => collider.ReturnOnGridTileIsAvailable());

        if (isPlacementValid)
        {
            PlaceOnGrid();
        }
        else
        {
            ResetToStart();
        }
    }

    private void PlaceOnGrid()
    {
        var mainCollider = GridControlColliders.FirstOrDefault(c => c.IsMain);

        if (mainCollider == null) return;

        UnitBox.JumpToGridTile(mainCollider.GridTile);
        EmptyBoxSignals.OnTheEmptyBoxRemoved?.Dispatch(this);
        DestroySelf().Forget();
        EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch();
    }

    private void ResetToStart()
    {
        foreach (var gridControlCollider in GridControlColliders)
        {
            gridControlCollider.GridTile = null;
        }

        EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(this);
        _emptyBoxMovement.HandleMouseUp();
    }
    
    private void OnBoxReturnedToStart(EmptyBoxMovement emptyBoxMovement)
    {
        if(emptyBoxMovement != _emptyBoxMovement) return;
        SetSelectableColliderActive(true);
    }
    
    private async UniTaskVoid DestroySelf()
    {
        if (this == null || _emptyBoxMovement == null) return;

        Disable();
        isActive = false;
        _cancellationTokenSource?.Cancel();

        _emptyBoxMovement.enabled = false;

        await UniTask.DelayFrame(1);

        if (this != null)
            Destroy(gameObject);
    }
}