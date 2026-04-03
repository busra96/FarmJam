using System;
using Signals;
using UnityEngine;

namespace FarmBlast
{
    public class SelectManager
    {
        private FB_EmptyUnitBoxParent SelectedEmptyBox;
        private Camera mainCamera;

        public void Init()
        {
            Debug.Log("SelectManager Init");
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
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                Mathf.Infinity,
                LayerMask.GetMask("EmptyBoxLayer"),
                QueryTriggerInteraction.Collide);

            if (hits.Length == 0)
            {
                return;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                FB_EmptyUnitBoxParent emptyUnitBoxParent = hits[i].collider.GetComponentInParent<FB_EmptyUnitBoxParent>();
                if (emptyUnitBoxParent != null)
                {
                    SelectedEmptyBox = emptyUnitBoxParent;
                    SelectedEmptyBox.Selected(mousePosition);
                    return;
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
}
