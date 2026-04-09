namespace FarmTetris
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class UnitBoxColorTypeAndMat : MonoBehaviour
    {
       public List<MeshRenderer> meshRendererList;
       public List<ColorTypeAndUnitBoxMaterial> ColorTypeAndUnitBoxMaterialList;
       
       public ColorType ColorType;

       public void ActiveColor()
       {
          foreach (var parameter in ColorTypeAndUnitBoxMaterialList)
          {
             if (parameter.ColorType == ColorType)
                foreach (var meshRenderer in meshRendererList)
                {
                   meshRenderer.material = parameter.ColorMaterial;
                }
          }
       }
    }

    [Serializable]
    public class ColorTypeAndUnitBoxMaterial
    {
       public ColorType ColorType;
       public Material ColorMaterial;
    }
}

