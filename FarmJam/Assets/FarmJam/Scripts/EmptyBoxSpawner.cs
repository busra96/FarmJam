using System;
using System.Collections.Generic;
using System.Linq;
using Signals;
using UnityEngine;
using Random = UnityEngine.Random;

public class EmptyBoxSpawner : MonoBehaviour
{
    public List<EmptyBox> AllEmptyBoxPrefabs;
    public List<SpawnPoint> SpawnPoints;
    private List<EmptyBox> EmptyBoxes = new List<EmptyBox>();

    public void OnEnable()
    {
        EmptyBoxSignals.OnTheEmptyBoxRemoved.AddListener(RemovedEmptyBox);
    }

    public void OnDisable()
    {
        EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(RemovedEmptyBox);
    }

    public void AddedEmptyBox(EmptyBox emptyBox)
    {
        if(EmptyBoxes.Contains(emptyBox))
            return;

        EmptyBoxes.Add(emptyBox);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnEmptyBox();
        }
    }

    public void RemovedEmptyBox(EmptyBox emptyBox)
    {
        if(!EmptyBoxes.Contains(emptyBox))
            return;
        
        EmptyBoxes.Remove(emptyBox);
        
        foreach (var spawnPoint in SpawnPoints)
        {
            if(spawnPoint.HasEmptyBox && spawnPoint.EmptyBox == emptyBox)
                spawnPoint.RemoveEmptyBox();
        }
    }

    [ContextMenu(" Spawn Empty Boxes ")]
    public void SpawnEmptyBox()
    {
       SpawnPoint spawnPoint = GetEmptySpawnPoint();
       if(spawnPoint == null) return;
       
       int randomIndex = Random.Range(0, AllEmptyBoxPrefabs.Count);
       EmptyBox emptyBox = Instantiate(AllEmptyBoxPrefabs[randomIndex], spawnPoint.transform.position, Quaternion.identity);

       spawnPoint.SetEmptyBox(emptyBox);
       AddedEmptyBox(emptyBox);
    }

    public SpawnPoint GetEmptySpawnPoint()
    {
        return SpawnPoints.FirstOrDefault(sp => !sp.HasEmptyBox);
    }
}