using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Signals;
using VContainer;

public class CollectableBoxManager
{
    private const int PROCESS_DELAY_MS = 500;

    [Inject] private UnitBoxManager unitBoxManager;
    [Inject] private CollectableBoxParentFactory _collectableBoxParentFactory;

    public List<CollectableBox> CollectableBoxes = new List<CollectableBox>();
    private bool isProcessing = false;
    private CancellationTokenSource _cancellationTokenSource;

    public void Init()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Disable()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void SpawnCollectableBoxParent(Level level)
    {
        CollectableBoxParent collectableBoxParent = _collectableBoxParentFactory.Create(level.LevelSpawnData.CollectableBoxParent);
        collectableBoxParent.transform.SetParent(level.transform);
        collectableBoxParent.Init();

        foreach (var collectableBox in collectableBoxParent.CollectableBoxList)
        {
            CollectableBoxes.Add(collectableBox);
            collectableBox.Init(unitBoxManager);
        }

        ProcessCollectableBoxes().Forget();
    }

    private async UniTask ProcessCollectableBoxes()
    {
        if (isProcessing || _cancellationTokenSource == null)
            return;

        isProcessing = true;

        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Remove null boxes without lambda allocation
                for (int i = CollectableBoxes.Count - 1; i >= 0; i--)
                {
                    if (CollectableBoxes[i] == null)
                        CollectableBoxes.RemoveAt(i);
                }

                if (CollectableBoxes.Count == 0)
                {
                    isProcessing = false;
                  //  GameStateSignals.OnGameWin?.Dispatch();
                    break;
                }

                foreach (var collectableBox in CollectableBoxes)
                {
                    if (collectableBox != null && collectableBox.gameObject.activeInHierarchy)
                    {
                        await collectableBox.FindUnitBox();
                    }
                }

                await UniTask.Delay(PROCESS_DELAY_MS, cancellationToken: _cancellationTokenSource.Token);
            }
        }
        catch (System.OperationCanceledException)
        {
            isProcessing = false;
        }
    }
    
    public bool WinCheck()
    {
        bool isWin = CollectableBoxes.Count == 0;
        return isWin;
    }
}