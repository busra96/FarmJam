namespace FarmTetris
{
    using UnityEngine;

    public class Level : MonoBehaviour
    {
        public int GridXCount;
        public int GridYCount;

        public Transform Grids;
        
        public FarmTetrisLevelSpawnData LevelSpawnData;

        public void Init(EmptyBoxSpawner emptyBoxSpawner,CollectableBoxManager collectableBoxManager, GridTileManager gridTileManager)
        {
            emptyBoxSpawner.SetCurrentLevel(this);
          
            for (int i = 0; i < 3; i++)
                emptyBoxSpawner.SpawnEmptyBox();

            collectableBoxManager.SpawnCollectableBoxParent(this);
            
            gridTileManager.SpawnGrids(GridXCount, GridYCount, Grids);
        }
    }
}

