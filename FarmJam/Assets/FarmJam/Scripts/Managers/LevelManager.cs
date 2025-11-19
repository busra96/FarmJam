using System.Collections.Generic;
using Signals;
using UnityEngine;
using VContainer;

public class LevelManager : MonoBehaviour
{
    [Inject] private EmptyBoxSpawner emptyBoxSpawner;
    [Inject] private CollectableBoxManager collectableBoxManager;
    [Inject] private GridTileManager gridTileManager;
    
    
    public List<Level> Levels;
    
    public int currentLevelCount;
    
    public void Init()
    {
        Debug.Log( " _level maanager ınit");
        LevelManagerSignals.OnLoadCurrentLevel.AddListener(OnLoadCurrentLevel);
        LevelManagerSignals.OnLoadNextLevel.AddListener(OnLoadNextLevel);
    }
    
    public void Disable()
    {
        LevelManagerSignals.OnLoadCurrentLevel.RemoveListener(OnLoadCurrentLevel);
        LevelManagerSignals.OnLoadNextLevel.RemoveListener(OnLoadNextLevel);
    }
    
    private void OnLoadNextLevel()
    {
        currentLevelCount++;
        LoadLevel();
    }

    private void OnLoadCurrentLevel()
    {
        LoadLevel();
    }
    
    
    public void LoadLevel()
    {
        var currentLevel = Instantiate(Levels[currentLevelCount % Levels.Count].gameObject, Vector3.zero, Quaternion.identity).GetComponent<Level>();
        currentLevel.Init(emptyBoxSpawner, collectableBoxManager,gridTileManager);
    }
}
