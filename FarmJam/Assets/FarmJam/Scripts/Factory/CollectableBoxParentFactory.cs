using UnityEngine;

public class CollectableBoxParentFactory
{
    private readonly Transform _parentTransform;
    
    public CollectableBoxParent Create(CollectableBoxParent prefab)
    {
        var instance = Object.Instantiate(prefab);
        return instance;
    }
}