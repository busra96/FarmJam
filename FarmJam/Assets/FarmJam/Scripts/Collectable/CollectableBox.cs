using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;
using VContainer;

public class CollectableBox : MonoBehaviour
{ 
   public bool IsLock;
   [Inject] public UnitBoxManager unitBoxManager;
   public List<CollectableParameter> CollectableParameters;
   private bool jumping = false;
   
   [ContextMenu(" Find Unit Box ")]
   public async UniTask FindUnitBox()
   {
       if(IsLock) return;
       
       jumping = true;

       while (jumping)
       {
           await JumpToTarget(); 
           await UniTask.DelayFrame(1);  // Sonsuz döngüyü engellemek için bir gecikme ekleyelim.
       }
   }
   
   public async UniTask JumpToTarget()
   {
       if (CollectableParameters.Count == 0)
       {
           jumping = false;
           return;
       }

       for (int i = 0; i < CollectableParameters.Count; i++)
       {
           var collectableParameter = CollectableParameters[i];
           if (collectableParameter.Collectable == null) continue;

           UnitBox unitBox = unitBoxManager.GetUnitBox();
           if (unitBox == null) break; // Boş kutu yoksa çık

           UnityBoxPoint unityBoxPoint = null;

           foreach (var point in unitBox.Points)
           {
               if (point.Collectable == null)
               {
                   unityBoxPoint = point;
                   break;
               }
           }

           if (unityBoxPoint != null)
           {
               unityBoxPoint.SetCollectable(collectableParameter.Collectable);

               await collectableParameter.Collectable.JumpToTarget(unityBoxPoint.transform);

               // Listeyi güncelle
               CollectableParameters.Remove(collectableParameter);
               i--; // Liste kısaldığı için indexi güncelle
           }
       }

       jumping = false;
       CollectablesControl();
      
   }

   private void CollectablesControl()
   {
       bool isEmpty = true;
       foreach (var collectableParameter in CollectableParameters)
       {
           if (collectableParameter.Collectable != null)
           {
               isEmpty = false;
               break;
           }
       }

       if (isEmpty)
       {
           Destroy(gameObject);
       }
   }
}

[Serializable]
public class CollectableParameter
{
    public Transform Point;
    public Collectable Collectable;
}
