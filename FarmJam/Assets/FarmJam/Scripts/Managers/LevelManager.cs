using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;
using VContainer;

public class LevelManager : MonoBehaviour
{
    private const int LEVEL_TRANSITION_DELAY_FRAMES = 5;

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
        LoadLevel().Forget();
    }

    private void OnLoadCurrentLevel()
    {
        LoadLevel().Forget();
    }

    private async UniTask LoadLevel()
    {
        await DestroyLevel();
        await UniTask.DelayFrame(LEVEL_TRANSITION_DELAY_FRAMES);
        await SpawnLevel();
    }

    private async UniTask SpawnLevel()
    {
        int levelIndex = currentLevelCount % Levels.Count;
        CurrentLevel = Instantiate(Levels[levelIndex].gameObject, Vector3.zero, Quaternion.identity).GetComponent<Level>();
        CurrentLevel.Init(emptyBoxSpawner, collectableBoxManager, gridTileManager);
    }

    private async UniTask DestroyLevel()
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
