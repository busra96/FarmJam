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
    }

    private void OnDisable()
    {
        CollectableBoxSignals.OnCollectableBoxControl.RemoveListener(CollectableBoxControl);
    }

    private void CollectableBoxControl()
    {
        UTCollectableBoxControl().Forget();
    }

    public async UniTask UTCollectableBoxControl()
    {
        while (isProcessing) // Eğer şu an çalışıyorsa, bitmesini bekle
        {
            await taskCompletionSource.Task;
        }

        isProcessing = true;
        taskCompletionSource = new UniTaskCompletionSource();

        for (int i = 0; i < CollectableBoxes.Count; i++)
        {
            var box = CollectableBoxes[i];

            if (box == null) continue; // Silinen nesne olabilir

            await box.FindUnitBox();
        }

        isProcessing = false;
        taskCompletionSource.TrySetResult(); // Bekleyen diğer çağrılara haber ver
    }
}