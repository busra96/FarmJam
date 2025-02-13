using System;
using System.Collections.Generic;
using UnityEngine;

public class CollectableColorAndMesh : MonoBehaviour
{
    public List<ColorTypeAndCollectableObj> ColorTypeAndCollectableObjList;
    public ColorType ColorType;

    public void ActiveColorObject()
    {
        foreach (var parameter in ColorTypeAndCollectableObjList)
        {
            if (parameter.ColorType == ColorType)
                parameter.CollectableMeshRenderer.enabled = true;
            else 
                parameter.CollectableMeshRenderer.enabled = false;
        }
    }
}


[Serializable]
public class ColorTypeAndCollectableObj
{
    public ColorType ColorType;
    public MeshRenderer CollectableMeshRenderer;
}
