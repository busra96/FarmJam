using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VContainer;

public class CollectableBox : MonoBehaviour
{
   public UnitBoxManager unitBoxManager;
   public List<CollectableParameter> CollectableParameters;

   [ContextMenu("JumpToTarget")]
   public void JumpToTarget()
   {
       UnitBox unitBox = unitBoxManager.GetUnitBox();
       UnityBoxPoint unityBoxPoint = null;
       for (int i = 0; i < unitBox.Points.Count; i++)
       {
           if (unitBox.Points[i].Collectable == null)
           {
               unityBoxPoint = unitBox.Points[i];
               break;
           }
       }

       if (unityBoxPoint != null)
       {
           unityBoxPoint.Collectable = CollectableParameters[0].Collectable;
           CollectableParameters[0].Collectable.JumpToTarget(unityBoxPoint.transform);
           CollectableParameters.RemoveAt(0);
       }
   }
}

[Serializable]
public class CollectableParameter
{
    public Transform Point;
    public Collectable Collectable;
}
