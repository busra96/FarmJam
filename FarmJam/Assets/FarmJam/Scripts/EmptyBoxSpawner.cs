using System.Collections.Generic;
using Signals;
using UnityEngine;
using Random = UnityEngine.Random;

public class EmptyBoxSpawner : MonoBehaviour
{
    public List<EmptyBox> AllEmptyBoxPrefabs;
    public Transform SpawnPoint;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SpawnEmptyBox();
    }

    public void SpawnEmptyBox()
    {
       int randomIndex = Random.Range(0, AllEmptyBoxPrefabs.Count);
       EmptyBox emptyBox = Instantiate(AllEmptyBoxPrefabs[randomIndex], SpawnPoint.position, Quaternion.identity);
       emptyBox.Init();
       emptyBox.transform.position = SpawnPoint.position;
       
       EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(emptyBox);
    }
}