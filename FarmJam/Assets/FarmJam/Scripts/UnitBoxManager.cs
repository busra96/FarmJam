using System.Collections.Generic;
using Signals;
using UnityEngine;

public class UnitBoxManager : MonoBehaviour
{
   public List<UnitBox> UnitBoxes  = new List<UnitBox>();

   private void OnEnable()
   {
      GridTileSignals.OnAddedUnitBox.AddListener(AddedUnitBox);
      GridTileSignals.OnRemovedUnitBox.AddListener(RemovedUnitBox);
      UnitBoxSignals.OnThisUnitBoxDestroyed.AddListener(RemovedUnitBox);
   }

   private void OnDisable()
   {
      GridTileSignals.OnAddedUnitBox.RemoveListener(AddedUnitBox);
      GridTileSignals.OnRemovedUnitBox.RemoveListener(RemovedUnitBox);
      UnitBoxSignals.OnThisUnitBoxDestroyed.RemoveListener(RemovedUnitBox);
   }

   public void AddedUnitBox(UnitBox unitBox)
   {
      if(UnitBoxes.Contains(unitBox))
         return;
      
      UnitBoxes.Add(unitBox);
   }

   public void RemovedUnitBox(UnitBox unitBox)
   {
      if(!UnitBoxes.Contains(unitBox))
         return;
      
      UnitBoxes.Remove(unitBox);
   }

   public UnitBox GetUnitBox(ColorType colorType)
   {
      foreach (UnitBox box in UnitBoxes)
      {
         if(box.UnitBoxColorTypeAndMat.ColorType != colorType) continue;
         if(box.IsFull) continue;
         
         return box;
      }

      return null;
   } 
   
   public void ClearUnitBoxList()
   {
      UnitBoxes.Clear();
   }
}