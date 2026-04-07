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
            private const int RESPAWN_DELAY_MS = 150;

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

                SpawnCollectableBoxParentInternal(collectableBoxParent, spawnPoint);

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

                        List<CollectableBox> currentCollectableBoxes = new List<CollectableBox>(CollectableBoxes);
                        for (int i = 0; i < currentCollectableBoxes.Count; i++)
                        {
                            CollectableBox collectableBox = currentCollectableBoxes[i];
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

            private void SpawnCollectableBoxParentInternal(CollectableBoxParent collectableBoxParent, GameObject spawnPoint)
            {
                CollectableBoxParent spawnedCollectableBoxParent = _collectableBoxParentFactory.Create(collectableBoxParent);
                spawnedCollectableBoxParent.transform.SetParent(spawnPoint.transform);
                spawnedCollectableBoxParent.transform.localPosition = Vector3.zero;
                spawnedCollectableBoxParent.transform.localRotation = Quaternion.identity;
                spawnedCollectableBoxParent.transform.localScale = Vector3.one;
                spawnedCollectableBoxParent.Init();

                foreach (var collectableBox in spawnedCollectableBoxParent.CollectableBoxList)
                {
                    if (collectableBox == null)
                    {
                        continue;
                    }

                    CollectableBoxes.Add(collectableBox);
                    collectableBox.Init(unitBoxManager, OnCollectableBoxDestroyed);
                }
            }

            private void OnCollectableBoxDestroyed(CollectableBox destroyedCollectableBox)
            {
                if (destroyedCollectableBox == null)
                {
                    return;
                }

                CollectableBoxes.Remove(destroyedCollectableBox);
                CollectableBoxParent collectableBoxParent = destroyedCollectableBox.GetComponentInParent<CollectableBoxParent>();
                int respawnSlotId = destroyedCollectableBox.RespawnSlotId;
                if (collectableBoxParent == null || respawnSlotId < 0)
                {
                    return;
                }

                RespawnCollectableBoxWithDelay(collectableBoxParent, respawnSlotId).Forget();
            }

            private async UniTaskVoid RespawnCollectableBoxWithDelay(CollectableBoxParent collectableBoxParent, int respawnSlotId)
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                try
                {
                    await UniTask.Delay(RESPAWN_DELAY_MS, cancellationToken: _cancellationTokenSource.Token);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }

                if (collectableBoxParent == null)
                {
                    return;
                }

                CollectableBox respawnedCollectableBox = collectableBoxParent.RespawnCollectableBox(respawnSlotId);
                if (respawnedCollectableBox == null)
                {
                    return;
                }

                CollectableBoxes.Add(respawnedCollectableBox);
                respawnedCollectableBox.Init(unitBoxManager, OnCollectableBoxDestroyed);
                ProcessCollectableBoxes().Forget();
            }
    }
}
