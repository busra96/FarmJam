using UnityEngine;
using VContainer;

namespace FarmBlast
{
    public class CollectableSpawnManager : MonoBehaviour
    {
        [Inject] private CollectableBoxManager _collectableBoxManager;
        public CollectableBoxParent ExampleCollectableBoxParent;
        public GameObject SpawnPoint;
        
        public void Init()
        {
            if (ExampleCollectableBoxParent == null || SpawnPoint == null)
            {
                return;
            }

            _collectableBoxManager.SpawnCollectableBoxParent(ExampleCollectableBoxParent,SpawnPoint);
        }

        public void Disable()
        {
            
        }
    }

}
