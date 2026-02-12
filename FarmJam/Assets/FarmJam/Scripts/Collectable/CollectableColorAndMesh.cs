using System;
using System.Collections.Generic;
using UnityEngine;

public class CollectableColorAndMesh : MonoBehaviour
{
    public List<ColorTypeAndCollectableObj> ColorTypeAndCollectableObjList;
    public ColorType ColorType;
    public CollectableMesh CollectableMesh;

    public void ActiveColorObject()
    {
        foreach (var parameter in ColorTypeAndCollectableObjList)
        {
            if (parameter.ColorType == ColorType)
            {
                CollectableMesh = parameter.CollectableMesh;
                CollectableMesh.Mesh.enabled = true;
            }
            else 
                parameter.CollectableMesh.Mesh.enabled = false;
        }
    }
}


[Serializable]
public class ColorTypeAndCollectableObj
{
    public ColorType ColorType;
    public CollectableMesh CollectableMesh;
}
