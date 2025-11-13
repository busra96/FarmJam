using System.Collections.Generic;
using Signals;
using UnityEngine;
using VContainer;

public class EmptyBoxSpawner : MonoBehaviour
{
    [Inject] private LevelManager _levelManager;
    private int index;
    public Transform SpawnPoint;

    [SerializeField] private List<EmptyBox> EmptyBoxList = new List<EmptyBox>();

    private void OnEnable()
    {
        EmptyBoxSignals.OnTheEmptyBoxRemoved.AddListener(RemoveEmptyBoxFromList);
    }

    private void OnDisable()
    {
        EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(RemoveEmptyBoxFromList);
    }

    public void Init()
    {
        index = 0;
        
        for (int i = 0; i < 3; i++)
            SpawnEmptyBox();
    }


    public void SpawnEmptyBox()
    {
        if(index >= _levelManager.LevelSpawnData.EmptyBoxTypes.Count) return;
        
       EmptyBox emptyBox = Instantiate(_levelManager.LevelSpawnData.EmptyBoxTypes[index].EmptyBox, SpawnPoint.position, Quaternion.identity);
       emptyBox.Init(_levelManager.LevelSpawnData.EmptyBoxTypes[index].ColorType);
       emptyBox.transform.position = SpawnPoint.position;
       AddedEmptyBoxToList(emptyBox);
       EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(emptyBox);

       index++;
    }

    public void AddedEmptyBoxToList(EmptyBox emptyBox)
    {
        if(EmptyBoxList.Contains(emptyBox))
            return;
        
        EmptyBoxList.Add(emptyBox);
    }

    public void RemoveEmptyBoxFromList(EmptyBox emptyBox)
    {
        if (!EmptyBoxList.Contains(emptyBox))
            return;
        
        EmptyBoxList.Remove(emptyBox);
        
        if(index >= _levelManager.LevelSpawnData.EmptyBoxTypes.Count)
        {
            Debug.Log(" DAHA SPAWNLANABILCEK KUTU YOK ");
            return;
        }

        SpawnEmptyBox();
    }
}