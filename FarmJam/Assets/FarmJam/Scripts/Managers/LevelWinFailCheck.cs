using Signals;
using UnityEngine;
using VContainer;

public class LevelWinFailCheck : MonoBehaviour
{
    public float Timer;
    private float currentTimer;
    private bool isControlling;

    [Inject] private readonly EmptyBoxSpawner EmptyBoxSpawner;
    [Inject] private readonly CollectableBoxManager CollectableBoxManager;
    
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
            GameStateSignals.OnGameWin?.Dispatch();
        }
        else if (!EmptyBoxSpawner.FailCheck())
        {
            GameStateSignals.OnGameFail?.Dispatch();
        }
    }
}
