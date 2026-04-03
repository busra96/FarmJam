using UnityEngine;

namespace FarmBlast
{
    public class CollectableBoxParentFactory
    {
        public CollectableBoxParent Create(CollectableBoxParent prefab)
        {
            var instance = Object.Instantiate(prefab);
            return instance;
        }
    }
}
