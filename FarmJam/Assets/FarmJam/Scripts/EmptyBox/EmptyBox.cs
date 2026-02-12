using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class EmptyBox : MonoBehaviour
{
    private const float SPAWN_SCALE = 0.7f;
    private const float SPAWN_DURATION = 0.2f;

    public EmptyBoxType EmptyBoxType;
    public UnitBox UnitBox;
    private EmptyBoxMovement _emptyBoxMovement;
    private EmptyBoxAudio _emptyBoxAudio;
    public Collider Collider;
    
    public List<GridControlCollider> GridControlColliders = new List<GridControlCollider>();
    private bool _onGridTile;
    
    private bool isActive;
    private CancellationTokenSource _cancellationTokenSource;

    public void Init(ColorType colorType)
    {
        _emptyBoxMovement = GetComponent<EmptyBoxMovement>();
        _emptyBoxAudio = GetComponent<EmptyBoxAudio>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _emptyBoxAudio.PlayAudioClip(EmptyBoxAudioClipType.Spawn);
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * SPAWN_SCALE, SPAWN_DURATION).SetEase(Ease.InBounce);
        
        UnitBox.Init(colorType);
        _emptyBoxMovement.Init();
        
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
        _emptyBoxAudio.PlayAudioClip(EmptyBoxAudioClipType.Select);
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
        bool isPlacementValid = IsPlacementValid();

        foreach (var gridControlCollider in GridControlColliders)
        {
            gridControlCollider.GridTile?.IsGridOkeyAndNotOkey(isPlacementValid);
        }
    }

    private void HandleMouseUpRaycastCheck()
    {
        bool isPlacementValid = IsPlacementValid();

        if (isPlacementValid)
        {
            PlaceOnGrid();
        }
        else
        {
            ResetToStart();
        }
    }

    private bool IsPlacementValid()
    {
        for (int i = 0; i < GridControlColliders.Count; i++)
        {
            if (!GridControlColliders[i].ReturnOnGridTileIsAvailable())
                return false;
        }
        return true;
    }

    private void PlaceOnGrid()
    {
        GridControlCollider mainCollider = null;
        for (int i = 0; i < GridControlColliders.Count; i++)
        {
            if (GridControlColliders[i].IsMain)
            {
                mainCollider = GridControlColliders[i];
                break;
            }
        }

        if (mainCollider == null) return;

        UnitBox.JumpToGridTile(mainCollider.GridTile);
        EmptyBoxSignals.OnTheEmptyBoxRemoved?.Dispatch(this);
        DestroySelf().Forget();
        EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch();
        EmptyBoxSignals.OnFailConditionCheck?.Dispatch();
    }

    private void ResetToStart()
    {
        _emptyBoxAudio.PlayAudioClip(EmptyBoxAudioClipType.Back);
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