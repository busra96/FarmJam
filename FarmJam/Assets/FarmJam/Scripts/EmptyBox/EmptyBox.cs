using System.Collections.Generic;
using UnityEngine;

public class EmptyBox : MonoBehaviour
{
    private EmptyBoxMovement _emptyBoxMovement;
    
    public List<RaycastControl> RaycastControls = new List<RaycastControl>();
    public Collider SelectableCollider;
    private bool _onGridTile;

    private void Start()
    {
        Init();
        _emptyBoxMovement.Init();
    }

    public void Init()
    {
        _emptyBoxMovement = GetComponent<EmptyBoxMovement>();
        SelectableColliderIsActive(true);
    }
    
    public void Selected(Vector3 pos)
    {
        _emptyBoxMovement.HandleMouseDown(pos);
        SelectableColliderIsActive(false);
    }

    public void Deselected(Vector3 pos)
    {
        _emptyBoxMovement.HandleMouseUp();
        SelectableColliderIsActive(true);
    }

    private void SelectableColliderIsActive(bool isActive)
    {
        SelectableCollider.enabled = isActive;
        if (!_onGridTile)
        {
            foreach (var raycastControllerObj in RaycastControls)
                raycastControllerObj.gameObject.SetActive(!isActive);
        }
        else
        {
            foreach (var raycastControllerObj in RaycastControls)
                raycastControllerObj.gameObject.SetActive(isActive);
        }
    }
}
