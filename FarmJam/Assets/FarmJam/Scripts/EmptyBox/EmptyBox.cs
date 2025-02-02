using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;

public class EmptyBox : MonoBehaviour
{
    private EmptyBoxMovement _emptyBoxMovement;
    
    public List<GridControlCollider> GridControlColliders = new List<GridControlCollider>();
    public Collider SelectableCollider;
    private bool _onGridTile;
    
    private bool isActive;

    private void Start()
    {
        Init();
        _emptyBoxMovement.Init();
    }

    public void Init()
    {
        _emptyBoxMovement = GetComponent<EmptyBoxMovement>();
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.AddListener(OnTheBoxHasCompletedTheMovementToTheStartingPosition);
        SelectableColliderIsActive(true);
    }

    public void Disable()
    {
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnTheBoxHasCompletedTheMovementToTheStartingPosition);
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
        SelectableCollider.enabled = isActive;
        if (!_onGridTile)
        {
            foreach (var gridControlCollider in GridControlColliders)
            {
                gridControlCollider.gameObject.SetActive(!isActive);
            }
        }
        else
        {
            foreach (var gridControlCollider in GridControlColliders)
            {
                gridControlCollider.gameObject.SetActive(isActive);
            }
        }
    }
    
    private async UniTask CheckInput()
    {
        isActive = true;

        while (isActive)
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
            if (!gridControlCollider.ReturnOnGridTileIsAvailable())
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
               GridControlCollider gridControlCollider = GridControlColliders[i];
             
            } 
        }
        else
        {
            _emptyBoxMovement.HandleMouseUp();
        }
    }
    
    private void OnTheBoxHasCompletedTheMovementToTheStartingPosition(EmptyBoxMovement emptyBoxMovement)
    {
        if(emptyBoxMovement != _emptyBoxMovement) return;
        
        SelectableColliderIsActive(true);
    }
}