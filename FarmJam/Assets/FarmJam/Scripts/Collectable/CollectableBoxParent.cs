using System.Collections.Generic;
using UnityEngine;

public class CollectableBoxParent : MonoBehaviour
{
    public List<CollectableBox> CollectableBoxList;

    public ColorType ColorType;

    public void Init()
    {
        foreach (var collectableBox in CollectableBoxList)
        {
            collectableBox.ColorType = ColorType;
            collectableBox.SetColor();
        }
    }
}
