using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;
using VContainer;

public class EmptyBoxSpawner : MonoBehaviour
{
    private const float FAIL_CHECK_DELAY_SECONDS = 1.5f;

    [Inject] private readonly GridTileManager _gridTileManager;
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
        if (index >= CurrentLevel.LevelSpawnData.EmptyBoxTypes.Count) return;

        var emptyBoxData = CurrentLevel.LevelSpawnData.EmptyBoxTypes[index];
        EmptyBox emptyBox = Instantiate(emptyBoxData.EmptyBox, SpawnPoint.position, Quaternion.identity);
        emptyBox.transform.SetParent(CurrentLevel.transform);
        emptyBox.Init(emptyBoxData.ColorType);
        emptyBox.transform.position = SpawnPoint.position;
        AddedEmptyBoxToList(emptyBox);
        EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(emptyBox);

        index++;
    }

    #region EmptyBox List Management

    public void AddedEmptyBoxToList(EmptyBox emptyBox)
    {
        if (EmptyBoxList.Contains(emptyBox))
            return;

        EmptyBoxList.Add(emptyBox);
    }

    public void RemoveEmptyBoxFromList(EmptyBox emptyBox)
    {
        if (!EmptyBoxList.Contains(emptyBox))
            return;

        if (CurrentLevel == null)
            return;

        EmptyBoxList.Remove(emptyBox);

        if (index >= CurrentLevel.LevelSpawnData.EmptyBoxTypes.Count)
            return;

        SpawnEmptyBox();
    }

    #endregion

    public void ClearEmptyBoxList()
    {
        index = 0;
        EmptyBoxList.Clear();
    }

    #region Fail Condition Check

    private void CheckFailCondition()
    {
        CheckFailConditionAsync().Forget();
    }

    private async UniTask CheckFailConditionAsync()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(FAIL_CHECK_DELAY_SECONDS));
    }

    public bool FailCheck()
    {
        for (int i = 0; i < EmptyBoxList.Count; i++)
        {
            if (_gridTileManager.HasAnyValidPlacement(EmptyBoxList[i].EmptyBoxType))
            {
                return true;
            }
        }
        
        return false;
    }

    #endregion
}