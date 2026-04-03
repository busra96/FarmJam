using System.Collections.Generic;
using System.Linq;
using Signals;
using UnityEngine;

namespace FarmBlast
{
    public class UnitBoxManager : MonoBehaviour
   {
      public List<UnitBox> UnitBoxes = new List<UnitBox>();
      private Dictionary<ColorType, List<UnitBox>> _unitBoxesByColor = new Dictionary<ColorType, List<UnitBox>>();

      private void OnEnable()
      {
         FB_GridTileSignals.OnAddedUnitBox.AddListener(AddedUnitBox);
         FB_GridTileSignals.OnRemovedUnitBox.AddListener(RemovedUnitBox);
         FB_UnitBoxSignals.OnThisUnitBoxDestroyed.AddListener(RemovedUnitBox);
      }

      private void OnDisable()
      {
         FB_GridTileSignals.OnAddedUnitBox.RemoveListener(AddedUnitBox);
         FB_GridTileSignals.OnRemovedUnitBox.RemoveListener(RemovedUnitBox);
         FB_UnitBoxSignals.OnThisUnitBoxDestroyed.RemoveListener(RemovedUnitBox);
      }

      public void AddedUnitBox(UnitBox unitBox)
      {
         if (UnitBoxes.Contains(unitBox))
            return;

         UnitBoxes.Add(unitBox);

         ColorType colorType = unitBox.UnitBoxColorTypeAndMat.ColorType;
         if (!_unitBoxesByColor.ContainsKey(colorType))
         {
            _unitBoxesByColor[colorType] = new List<UnitBox>();
         }

         _unitBoxesByColor[colorType].Add(unitBox);
      }

      public void RemovedUnitBox(UnitBox unitBox)
      {
         if (!UnitBoxes.Contains(unitBox))
            return;

         UnitBoxes.Remove(unitBox);

         ColorType colorType = unitBox.UnitBoxColorTypeAndMat.ColorType;
         if (_unitBoxesByColor.ContainsKey(colorType))
         {
            _unitBoxesByColor[colorType].Remove(unitBox);
            if (_unitBoxesByColor[colorType].Count == 0)
            {
               _unitBoxesByColor.Remove(colorType);
            }
         }
      }

      public UnitBox GetUnitBox(ColorType colorType)
      {
         if (!_unitBoxesByColor.TryGetValue(colorType, out List<UnitBox> boxes))
            return null;

         return boxes.FirstOrDefault(box => box != null && !box.IsFull && box.GetEmptyBoxPoint() != null);
      }

      public void ClearUnitBoxList()
      {
         UnitBoxes.Clear();
         _unitBoxesByColor.Clear();
      }
   }
}
