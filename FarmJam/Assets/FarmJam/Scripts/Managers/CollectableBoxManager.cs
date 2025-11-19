using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using VContainer;

public class CollectableBoxManager
{
    [Inject] private UnitBoxManager unitBoxManager;
    [Inject] private CollectableBoxParentFactory _collectableBoxParentFactory;
    
    public List<CollectableBox> CollectableBoxes = new List<CollectableBox>();
    private bool isProcessing = false;
    
    public void Init()
    {
    }

    public void Disable()
    {
        
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
            
        
        UTProcessCollectableBoxes().Forget();
    }
    
    public async UniTask UTProcessCollectableBoxes()
    {
        isProcessing = true;
        while (true) // Sonsuz döngü
        {
            // Eğer tüm CollectableBox'lar yok olduysa döngüyü sonlandır
            CollectableBoxes.RemoveAll(box => box == null);
            if (CollectableBoxes.Count == 0)
            {
                isProcessing = false;
                break; // Döngüden çık
            }

            // Tüm aktif CollectableBox nesneleri sırayla işlenir
            foreach (var collectableBox in CollectableBoxes)
            {
                if (collectableBox != null && collectableBox.gameObject.activeInHierarchy)
                {
                    await collectableBox.FindUnitBox();
                }
            }

            await UniTask.Delay(500); // 2 saniye bekleyerek tekrar kontrol et
        }
        
        GameStateSignals.OnGameWin?.Dispatch();
    }
}