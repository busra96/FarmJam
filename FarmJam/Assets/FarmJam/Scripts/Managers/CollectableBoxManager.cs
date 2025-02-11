using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;
using VContainer;

public class CollectableBoxManager : MonoBehaviour
{
    [Inject] private UnitBoxManager unitBoxManager;
    public List<CollectableBox> CollectableBoxes;
    private UniTaskCompletionSource taskCompletionSource;
    private bool isProcessing = false;

    private void OnEnable()
    {
        CollectableBoxSignals.OnCollectableBoxControl.AddListener(CollectableBoxControl);
        CollectableBoxSignals.OnCollectableBoxDestroyed.AddListener(RemovedCollectableBox);
    }

    private void OnDisable()
    {
        CollectableBoxSignals.OnCollectableBoxControl.RemoveListener(CollectableBoxControl);
        CollectableBoxSignals.OnCollectableBoxDestroyed.RemoveListener(RemovedCollectableBox);
    }

    private void CollectableBoxControl()
    {
        UTCollectableBoxControl();
    }

    public async UniTask UTCollectableBoxControl()
    {
        while (isProcessing) // Eğer şu an çalışıyorsa, bitmesini bekle
        {
            await taskCompletionSource.Task;
        }

        isProcessing = true;
        taskCompletionSource = new UniTaskCompletionSource();

        foreach (var box in CollectableBoxes)
        {
            await box.FindUnitBox();
        }

        isProcessing = false;
        taskCompletionSource.TrySetResult(); // Bekleyen diğer çağrılara haber ver
    }

    public void RemovedCollectableBox(CollectableBox collectableBox)
    {
        if(CollectableBoxes.Contains(collectableBox))
            CollectableBoxes.Remove(collectableBox);
    }
}