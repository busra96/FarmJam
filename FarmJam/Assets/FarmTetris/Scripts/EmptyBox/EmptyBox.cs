namespace FarmTetris
{
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;

    public class EmptyBox : MonoBehaviour
    {
        private struct PlacementTarget
        {
            public UnitBox UnitBox;
            public GridControlCollider MainCollider;
        }

        private const float SPAWN_SCALE = 0.7f;
        private const float SPAWN_DURATION = 0.2f;
        private const float DEFAULT_UNIT_SPACING = 2f;

        public EmptyBoxType EmptyBoxType;
        public UnitBox UnitBox;
        public List<UnitBox> UnitBoxes = new List<UnitBox>();
        private EmptyBoxMovement _emptyBoxMovement;
        private EmptyBoxAudio _emptyBoxAudio;
        public Collider Collider;
        
        [SerializeField] private Transform _unitBoxParent;
        [SerializeField] private float _unitSpacing = DEFAULT_UNIT_SPACING;
        public List<GridControlCollider> GridControlColliders = new List<GridControlCollider>();
        private bool _onGridTile;
        
        private bool isActive;
        private CancellationTokenSource _cancellationTokenSource;

        public void Init(ColorType colorType)
        {
            Init(colorType, EmptyBoxType);
        }

        public void Init(ColorType colorType, EmptyBoxType emptyBoxType)
        {
            _emptyBoxMovement = GetComponent<EmptyBoxMovement>();
            _emptyBoxAudio = GetComponent<EmptyBoxAudio>();
            _cancellationTokenSource = new CancellationTokenSource();
            EmptyBoxType = emptyBoxType;

            BuildLayout(colorType);
            
            _emptyBoxAudio.PlayAudioClip(EmptyBoxAudioClipType.Spawn);
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one * SPAWN_SCALE, SPAWN_DURATION).SetEase(Ease.InBounce);
            
            _emptyBoxMovement.Init();
            
            EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnBoxReturnedToStart);
            EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.AddListener(OnBoxReturnedToStart);
            EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch();
            
            SetSelectableColliderActive(true);
        }

        public void Disable()
        {
            _emptyBoxMovement.Disable();
            _cancellationTokenSource?.Cancel();
            EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnBoxReturnedToStart);
        }
        
        public void Selected(Vector3 pos)
        {
            RefreshCollections();
            _emptyBoxAudio.PlayAudioClip(EmptyBoxAudioClipType.Select);
            _emptyBoxMovement.HandleMouseDown(pos);
            SetSelectableColliderActive(false);
            TrackInput().Forget();
            
            EmptyBoxSignals.OnRemovedEmptyBox?.Dispatch(this);
        }

        public void Deselected(Vector3 pos)
        {
            isActive = false;
            HandleMouseUpRaycastCheck();
        }

        private void SetSelectableColliderActive(bool isActive)
        {
            foreach (var gridControlCollider in GridControlColliders)
            {
                if (gridControlCollider != null)
                {
                    gridControlCollider.gameObject.SetActive(_onGridTile ? isActive : !isActive);
                }
            }
        }
        
        private async UniTask TrackInput()
        {
            isActive = true;

            while (isActive && !_cancellationTokenSource.IsCancellationRequested)
            {
                CheckRaycast();
                await UniTask.Yield();
            }
        }

        private void CheckRaycast()
        {
            bool isPlacementValid = IsPlacementValid();

            foreach (var gridControlCollider in GridControlColliders)
            {
                gridControlCollider?.GridTile?.IsGridOkeyAndNotOkey(isPlacementValid);
            }
        }

        private void HandleMouseUpRaycastCheck()
        {
            bool isPlacementValid = IsPlacementValid();

            if (isPlacementValid)
            {
                PlaceOnGrid();
            }
            else
            {
                ResetToStart();
            }
        }

        private bool IsPlacementValid()
        {
            HashSet<GridTile> targetedTiles = new HashSet<GridTile>();

            for (int i = 0; i < GridControlColliders.Count; i++)
            {
                GridControlCollider gridControlCollider = GridControlColliders[i];
                if (gridControlCollider == null || !gridControlCollider.ReturnOnGridTileIsAvailable())
                {
                    return false;
                }

                if (!targetedTiles.Add(gridControlCollider.GridTile))
                {
                    return false;
                }
            }

            return true;
        }

        private void PlaceOnGrid()
        {
            if (!TryCollectPlacementTargets(out List<PlacementTarget> placementTargets))
            {
                ResetToStart();
                return;
            }

            for (int i = 0; i < placementTargets.Count; i++)
            {
                placementTargets[i].UnitBox.JumpToGridTile(placementTargets[i].MainCollider.GridTile);
            }

            GridControlColliders.Clear();
            UnitBoxes.Clear();
            EmptyBoxSignals.OnTheEmptyBoxRemoved?.Dispatch(this);
            DestroySelf().Forget();
            EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch();
            EmptyBoxSignals.OnFailConditionCheck?.Dispatch();
        }

        private void ResetToStart()
        {
            _emptyBoxAudio.PlayAudioClip(EmptyBoxAudioClipType.Back);
            foreach (var gridControlCollider in GridControlColliders)
            {
                if (gridControlCollider != null)
                {
                    gridControlCollider.GridTile = null;
                }
            }

            EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(this);
            _emptyBoxMovement.HandleMouseUp();
        }
        
        private void OnBoxReturnedToStart(EmptyBoxMovement emptyBoxMovement)
        {
            if (emptyBoxMovement != _emptyBoxMovement)
            {
                return;
            }

            SetSelectableColliderActive(true);
        }
        
        private async UniTaskVoid DestroySelf()
        {
            if (this == null || _emptyBoxMovement == null)
            {
                return;
            }

            Disable();
            isActive = false;
            _cancellationTokenSource?.Cancel();

            _emptyBoxMovement.enabled = false;

            await UniTask.DelayFrame(1);

            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private void BuildLayout(ColorType colorType)
        {
            Vector2Int[] shapeCells = EmptyBoxRotationData.GetBaseShapeCells(EmptyBoxType);
            if (shapeCells == null || shapeCells.Length == 0)
            {
                Debug.LogError($"[EmptyBox] Shape data not found for {EmptyBoxType}", this);
                return;
            }

            List<UnitBox> existingUnitBoxes = new List<UnitBox>(GetComponentsInChildren<UnitBox>(true));
            UnitBox templateUnitBox = ResolveUnitBoxTemplate(existingUnitBoxes);
            if (templateUnitBox == null)
            {
                Debug.LogError("[EmptyBox] UnitBox template not found.", this);
                return;
            }

            EnsureUnitBoxParent(templateUnitBox);

            Vector2 center = CalculateBoundsCenter(shapeCells);
            UnitBoxes.Clear();

            for (int i = 0; i < shapeCells.Length; i++)
            {
                UnitBox unitBox = GetOrCreateUnitBox(i, existingUnitBoxes, templateUnitBox);
                if (unitBox == null)
                {
                    continue;
                }

                SetupUnitBox(unitBox, shapeCells[i], center, i);
                unitBox.Init(colorType);
                UnitBoxes.Add(unitBox);
            }

            RemoveUnusedUnitBoxes(existingUnitBoxes, shapeCells.Length);
            ConfigureLocalNeighbors(shapeCells);
            UnitBox = UnitBoxes.Count > 0 ? UnitBoxes[0] : UnitBox;
            RefreshCollections();
        }

        private void EnsureUnitBoxParent(UnitBox templateUnitBox)
        {
            if (_unitBoxParent != null)
            {
                return;
            }

            if (templateUnitBox != null && templateUnitBox.transform.parent != null)
            {
                _unitBoxParent = templateUnitBox.transform.parent;
                return;
            }

            _unitBoxParent = transform;
        }

        private UnitBox ResolveUnitBoxTemplate(List<UnitBox> existingUnitBoxes)
        {
            if (UnitBox != null)
            {
                return UnitBox;
            }

            if (existingUnitBoxes.Count == 0)
            {
                return null;
            }

            UnitBox = existingUnitBoxes[0];
            return UnitBox;
        }

        private UnitBox GetOrCreateUnitBox(int index, List<UnitBox> existingUnitBoxes, UnitBox templateUnitBox)
        {
            if (index < existingUnitBoxes.Count)
            {
                UnitBox existingUnitBox = existingUnitBoxes[index];
                existingUnitBox.gameObject.SetActive(true);
                return existingUnitBox;
            }

            return Instantiate(templateUnitBox, _unitBoxParent);
        }

        private void SetupUnitBox(UnitBox unitBox, Vector2Int cell, Vector2 center, int index)
        {
            Transform unitBoxTransform = unitBox.transform;
            unitBoxTransform.SetParent(_unitBoxParent, false);
            unitBoxTransform.localRotation = Quaternion.identity;
            unitBoxTransform.localScale = Vector3.one;
            unitBoxTransform.localPosition = CalculateLocalPosition(cell, center);
            unitBox.name = $"UnitBox_{index}";
        }

        private void RemoveUnusedUnitBoxes(List<UnitBox> existingUnitBoxes, int requiredCount)
        {
            for (int i = requiredCount; i < existingUnitBoxes.Count; i++)
            {
                if (existingUnitBoxes[i] != null)
                {
                    Destroy(existingUnitBoxes[i].gameObject);
                }
            }
        }

        private void RefreshCollections()
        {
            UnitBoxes.RemoveAll(unitBox => unitBox == null);
            GridControlColliders.Clear();

            for (int i = 0; i < UnitBoxes.Count; i++)
            {
                IReadOnlyList<GridControlCollider> colliders = UnitBoxes[i].Colliders;
                for (int j = 0; j < colliders.Count; j++)
                {
                    if (colliders[j] != null)
                    {
                        GridControlColliders.Add(colliders[j]);
                    }
                }
            }

            RefreshSelectionCollider();
        }

        private void ConfigureLocalNeighbors(IReadOnlyList<Vector2Int> shapeCells)
        {
            Dictionary<Vector2Int, UnitBox> unitBoxByCell = new Dictionary<Vector2Int, UnitBox>();

            for (int i = 0; i < UnitBoxes.Count && i < shapeCells.Count; i++)
            {
                if (UnitBoxes[i] != null)
                {
                    unitBoxByCell[shapeCells[i]] = UnitBoxes[i];
                    UnitBoxes[i].ClearNeighbors();
                }
            }

            foreach (KeyValuePair<Vector2Int, UnitBox> cellToUnitBox in unitBoxByCell)
            {
                TryAssignNeighbor(unitBoxByCell, cellToUnitBox.Value, cellToUnitBox.Key + Vector2Int.left, Direction.Left);
                TryAssignNeighbor(unitBoxByCell, cellToUnitBox.Value, cellToUnitBox.Key + Vector2Int.right, Direction.Right);
                TryAssignNeighbor(unitBoxByCell, cellToUnitBox.Value, cellToUnitBox.Key + Vector2Int.up, Direction.Down);
                TryAssignNeighbor(unitBoxByCell, cellToUnitBox.Value, cellToUnitBox.Key + Vector2Int.down, Direction.Up);
            }
        }

        private static void TryAssignNeighbor(Dictionary<Vector2Int, UnitBox> unitBoxByCell, UnitBox sourceUnitBox, Vector2Int neighborCell, Direction direction)
        {
            if (sourceUnitBox == null)
            {
                return;
            }

            if (unitBoxByCell.TryGetValue(neighborCell, out UnitBox neighborUnitBox) && neighborUnitBox != null)
            {
                sourceUnitBox.SetNeighbor(direction, neighborUnitBox);
            }
        }

        private void RefreshSelectionCollider()
        {
            if (!(Collider is BoxCollider selectionCollider))
            {
                return;
            }

            bool hasBounds = false;
            Bounds combinedBounds = default;

            for (int i = 0; i < UnitBoxes.Count; i++)
            {
                Collider sourceCollider = GetSelectionSourceCollider(UnitBoxes[i]);
                if (sourceCollider == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = sourceCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(sourceCollider.bounds);
                }
            }

            if (!hasBounds)
            {
                selectionCollider.center = Vector3.zero;
                selectionCollider.size = Vector3.one * 2f;
                return;
            }

            Transform colliderTransform = selectionCollider.transform;
            selectionCollider.center = colliderTransform.InverseTransformPoint(combinedBounds.center);
            Vector3 localSize = colliderTransform.InverseTransformVector(combinedBounds.size);
            selectionCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private static Collider GetSelectionSourceCollider(UnitBox unitBox)
        {
            if (unitBox == null)
            {
                return null;
            }

            if (unitBox.UnitBoxModel != null)
            {
                Collider modelCollider = unitBox.UnitBoxModel.GetComponent<Collider>();
                if (modelCollider != null)
                {
                    return modelCollider;
                }
            }

            Collider[] colliders = unitBox.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].GetComponent<GridControlCollider>() == null)
                {
                    return colliders[i];
                }
            }

            return null;
        }

        private bool TryCollectPlacementTargets(out List<PlacementTarget> placementTargets)
        {
            placementTargets = new List<PlacementTarget>();
            HashSet<GridTile> targetedTiles = new HashSet<GridTile>();

            for (int i = 0; i < UnitBoxes.Count; i++)
            {
                UnitBox unitBox = UnitBoxes[i];
                if (unitBox == null)
                {
                    return false;
                }

                GridControlCollider mainCollider = unitBox.GetMainGridControlCollider();
                if (mainCollider == null || mainCollider.GridTile == null || mainCollider.GridTile.UnitBox != null)
                {
                    return false;
                }

                if (!targetedTiles.Add(mainCollider.GridTile))
                {
                    return false;
                }

                placementTargets.Add(new PlacementTarget
                {
                    UnitBox = unitBox,
                    MainCollider = mainCollider
                });
            }

            return placementTargets.Count > 0;
        }

        private Vector3 CalculateLocalPosition(Vector2Int cell, Vector2 center)
        {
            float localX = (cell.x - center.x) * _unitSpacing;
            float localZ = (center.y - cell.y) * _unitSpacing;
            return new Vector3(localX, 0f, localZ);
        }

        private static Vector2 CalculateBoundsCenter(IReadOnlyList<Vector2Int> cells)
        {
            int minX = cells[0].x;
            int maxX = cells[0].x;
            int minY = cells[0].y;
            int maxY = cells[0].y;

            for (int i = 1; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }
    }
}
