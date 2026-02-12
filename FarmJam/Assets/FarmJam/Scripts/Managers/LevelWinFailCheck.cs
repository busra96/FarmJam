using Signals;
using UnityEngine;
using VContainer;

public class LevelWinFailCheck : MonoBehaviour
{
    public float Timer;
    private float currentTimer;
    private bool isControlling;

    [Inject] private EmptyBoxSpawner EmptyBoxSpawner;
    [Inject] private CollectableBoxManager CollectableBoxManager;
    
    public void Init()
    {
        LevelManagerSignals.OnLevelWinFailCheckTimerRestart.AddListener(ControlTimerRestart);
    }

    public void Disable()
    {
        LevelManagerSignals.OnLevelWinFailCheckTimerRestart.RemoveListener(ControlTimerRestart);
    }

    public void Update()
    {
        if (isControlling)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0)
            {
                isControlling = false;
                Control();
            }
        }
    }

    public void ControlTimerRestart()
    {
        isControlling = true;
        currentTimer = Timer;
    }

    public void Control()
    {
        if (CollectableBoxManager.WinCheck())
        {
            Debug.Log(" WIN ");
            GameStateSignals.OnGameWin?.Dispatch();
        }
        else if (EmptyBoxSpawner.FailCheck() == false)
        {
            Debug.Log( " FAIL ");
            GameStateSignals.OnGameFail?.Dispatch();
        }
        else
        {
            Debug.Log(" OYUN DEVAM EDIYOR ");
        }
    }
}
