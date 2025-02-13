using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitBoxColorTypeAndMat : MonoBehaviour
{
   public MeshRenderer meshRenderer;
   public List<ColorTypeAndUnitBoxMaterial> ColorTypeAndUnitBoxMaterialList;
   
   public ColorType ColorType;

   [ContextMenu("Get ColorTypeAndUnitBoxMaterialList")]
   public void ActiveColor()
   {
      foreach (var parameter in ColorTypeAndUnitBoxMaterialList)
      {
         if (parameter.ColorType == ColorType)
            meshRenderer.material = parameter.ColorMaterial;
      }
   }
}

[Serializable]
public class ColorTypeAndUnitBoxMaterial
{
   public ColorType ColorType;
   public Material ColorMaterial;
}