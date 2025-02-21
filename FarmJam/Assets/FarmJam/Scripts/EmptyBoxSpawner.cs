using Signals;
using UnityEngine;
using VContainer;

public class EmptyBoxSpawner : MonoBehaviour
{
    [Inject] private LevelManager _levelManager;
    private int index;
    public Transform SpawnPoint;

    public void Init()
    {
        index = 0;
        SpawnEmptyBox();
    }

    public void SpawnEmptyBox()
    {
        if(index >= _levelManager.LevelSpawnData.EmptyBoxTypes.Count) return;
        
       EmptyBox emptyBox = Instantiate(_levelManager.LevelSpawnData.EmptyBoxTypes[index].EmptyBox, SpawnPoint.position, Quaternion.identity);
       emptyBox.Init(_levelManager.LevelSpawnData.EmptyBoxTypes[index].ColorType);
       emptyBox.transform.position = SpawnPoint.position;
       
       EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(emptyBox);

       index++;
    }
}