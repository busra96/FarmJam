using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FarmBlast
{
        public class CollectableBoxManager
        { 
            private const int PROCESS_DELAY_MS = 150;

            [Inject] private readonly UnitBoxManager unitBoxManager;
            [Inject] private readonly GridTileManager _gridTileManager;
            [Inject] private readonly CollectableBoxParentFactory _collectableBoxParentFactory;

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

            public void SpawnCollectableBoxParent(CollectableBoxParent collectableBoxParent,GameObject spawnPoint)
            {
                if (collectableBoxParent == null || spawnPoint == null)
                    return;

                CollectableBoxParent _collectableBoxParent = _collectableBoxParentFactory.Create(collectableBoxParent);
                _collectableBoxParent.transform.SetParent(spawnPoint.transform);
                _collectableBoxParent.transform.localPosition = Vector3.zero;
                _collectableBoxParent.transform.localRotation = Quaternion.identity;
                _collectableBoxParent.transform.localScale = Vector3.one;
                _collectableBoxParent.Init();

                foreach (var collectableBox in _collectableBoxParent.CollectableBoxList)
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
                        bool movedAnyCollectable = false;

                        // Remove null boxes without lambda allocation
                        for (int i = CollectableBoxes.Count - 1; i >= 0; i--)
                        {
                            if (CollectableBoxes[i] == null || CollectableBoxes[i].IsDestroying)
                                CollectableBoxes.RemoveAt(i);
                        }

                        if (CollectableBoxes.Count == 0)
                        {
                            isProcessing = false;
                            break;
                        }

                        foreach (var collectableBox in CollectableBoxes)
                        {
                            if (collectableBox != null && !collectableBox.IsDestroying && collectableBox.gameObject.activeInHierarchy)
                            {
                                movedAnyCollectable |= await collectableBox.FindUnitBox();
                            }
                        }

                        if (movedAnyCollectable)
                        {
                            _gridTileManager?.ResolveMatches();
                        }

                        await UniTask.Delay(PROCESS_DELAY_MS, cancellationToken: _cancellationTokenSource.Token);
                    }
                }
                catch (System.OperationCanceledException)
                {
                    isProcessing = false;
                }
            }
    }
}
