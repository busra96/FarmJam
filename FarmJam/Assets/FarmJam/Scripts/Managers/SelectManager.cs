using Signals;
using UnityEngine;

public class SelectManager
{
   private EmptyBox SelectedEmptyBox;
   private Camera mainCamera;

   public void Init()
   {
      mainCamera = Camera.main;
      InputSignals.OnInputGetMouseDown.AddListener(OnInputGetMouseDown);
      InputSignals.OnInputGetMouseUp.AddListener(OnInputGetMouseUp);
   }

   public void Disable()
   {
      InputSignals.OnInputGetMouseDown.RemoveListener(OnInputGetMouseDown);
      InputSignals.OnInputGetMouseUp.RemoveListener(OnInputGetMouseUp);
   }
   
   private void OnInputGetMouseDown(Vector3 mousePosition)
   {
      Ray ray = mainCamera.ScreenPointToRay(mousePosition);

      if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("EmptyBoxLayer")))
      {
         if (hitInfo.collider.CompareTag("EmptyBox"))
         {
             EmptyBox emptyBox = hitInfo.collider.GetComponentInParent<EmptyBox>();
             if (emptyBox != null)
             {
                SelectedEmptyBox = emptyBox;
                SelectedEmptyBox.Selected(mousePosition);
             }
         }
      }
   }
   
   private void OnInputGetMouseUp(Vector3 mousePosition)
   {
      if (SelectedEmptyBox != null)
         SelectedEmptyBox.Deselected(mousePosition);
      
      SelectedEmptyBox = null;
      GridTileSignals.OnGridMaterialCheck?.Dispatch();
   }
}