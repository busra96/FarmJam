using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;
using VContainer;

public class LevelManager : MonoBehaviour
{
    [Inject] private EmptyBoxSpawner emptyBoxSpawner;
    [Inject] private CollectableBoxManager collectableBoxManager;
    [Inject] private GridTileManager gridTileManager;
    [Inject] private TetrisSpacingLayoutManager tetrisSpacingLayoutManager;
    [Inject] private UnitBoxManager unitBoxManager;
    
    
    public List<Level> Levels;

    private Level CurrentLevel;
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

    public async UniTask LoadLevel()
    {
        await DestroyLevel();
        await UniTask.DelayFrame(5);
        await SpawnLevel();
    }

    
    
    public async UniTask SpawnLevel()
    {
        CurrentLevel = Instantiate(Levels[currentLevelCount % Levels.Count].gameObject, Vector3.zero, Quaternion.identity).GetComponent<Level>();
        CurrentLevel.Init(emptyBoxSpawner, collectableBoxManager,gridTileManager);
    }

    public async UniTask DestroyLevel()
    {
        if (CurrentLevel == null)
            return;
        
        tetrisSpacingLayoutManager.ClearEmptyBoxList();
        emptyBoxSpawner.ClearEmptyBoxList();
        gridTileManager.DestroyAllGrids();
        unitBoxManager.ClearUnitBoxList();
        Destroy(CurrentLevel.gameObject);
        CurrentLevel = null;

    }
}
