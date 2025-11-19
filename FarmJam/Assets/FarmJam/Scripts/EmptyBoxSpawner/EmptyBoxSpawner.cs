using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;
using VContainer;

public class EmptyBoxSpawner : MonoBehaviour
{
    [Inject] private GridTileManager _gridTileManager;
    private int index;
    public Transform SpawnPoint;

    [SerializeField] private List<EmptyBox> EmptyBoxList = new List<EmptyBox>();

    private Level CurrentLevel;

    private void OnEnable()
    {
        EmptyBoxSignals.OnTheEmptyBoxRemoved.AddListener(RemoveEmptyBoxFromList);
        EmptyBoxSignals.OnFailConditionCheck.AddListener(CheckFailCondition);
    }

    private void OnDisable()
    {
        EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(RemoveEmptyBoxFromList);
        EmptyBoxSignals.OnFailConditionCheck.RemoveListener(CheckFailCondition);
    }

    public void Init()
    {
        index = 0;
    }

    public void SetCurrentLevel(Level level)
    {
        CurrentLevel = level;
    }
    

    public void SpawnEmptyBox()
    {
        if(index >= CurrentLevel.LevelSpawnData.EmptyBoxTypes.Count) return;
        
       EmptyBox emptyBox = Instantiate(CurrentLevel.LevelSpawnData.EmptyBoxTypes[index].EmptyBox, SpawnPoint.position, Quaternion.identity);
       emptyBox.transform.SetParent(CurrentLevel.transform);
       emptyBox.Init(CurrentLevel.LevelSpawnData.EmptyBoxTypes[index].ColorType);
       emptyBox.transform.position = SpawnPoint.position;
       AddedEmptyBoxToList(emptyBox);
       EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(emptyBox);

       index++;
    }

    #region Added - Remove EmptyBox 
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
            
            if(CurrentLevel == null)
                return;
            
            EmptyBoxList.Remove(emptyBox);
            
            if(index >= CurrentLevel.LevelSpawnData.EmptyBoxTypes.Count)
            {
                Debug.Log(" DAHA SPAWNLANABILCEK KUTU YOK ");
                return;
            }

            SpawnEmptyBox();
        }
    #endregion
    
    #region Fail Condition Check 

        private void CheckFailCondition()
        {
            UTCheckFailCondition().Forget();
        }

        private async UniTask UTCheckFailCondition()
        {
            await UniTask.DelayFrame(10);
            bool failCondition = true;
            for (int i = 0; i < EmptyBoxList.Count; i++)
            {
                if (_gridTileManager.HasAnyValidPlacement(EmptyBoxList[i].EmptyBoxType))
                {
                    failCondition = false;
                    break;
                }
            }

            if (failCondition)
            {
                Debug.Log(" EMPTY BOX KOYACAK YER KALMADI");
                GameStateSignals.OnGameFail?.Dispatch();
            }
        }
    
    #endregion
}