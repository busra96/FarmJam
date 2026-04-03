using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

namespace FarmBlast
{
    public enum FB_EmptyUnitBoxLayoutType
    {
        Single,
        Double,
        Triple,
        LType1,
        LType2,
        LType3,
        LType4
    }

    public class FB_EmptyUnitBoxParent : MonoBehaviour
    {
        private struct PlacementTarget
        {
            public UnitBox UnitBox;
            public GridControlCollider MainCollider;
        }

        private const float DEFAULT_SPACING = 2.1f;
        private const float SPAWN_SCALE = 0.7f;
        private const float SPAWN_DURATION = 0.2f;
        private static readonly ColorType[] AVAILABLE_COLOR_TYPES = (ColorType[])System.Enum.GetValues(typeof(ColorType));

        private static readonly FB_EmptyUnitBoxLayoutType[] SPAWNABLE_LAYOUT_TYPES =
        {
            FB_EmptyUnitBoxLayoutType.Single,
            FB_EmptyUnitBoxLayoutType.Double,
            FB_EmptyUnitBoxLayoutType.Triple,
            FB_EmptyUnitBoxLayoutType.LType1,
            FB_EmptyUnitBoxLayoutType.LType2,
            FB_EmptyUnitBoxLayoutType.LType3,
            FB_EmptyUnitBoxLayoutType.LType4
        };

        private static readonly Dictionary<FB_EmptyUnitBoxLayoutType, Vector2Int[]> LAYOUT_CELLS =
            new Dictionary<FB_EmptyUnitBoxLayoutType, Vector2Int[]>
            {
                {
                    FB_EmptyUnitBoxLayoutType.Single,
                    new[]
                    {
                        new Vector2Int(0, 0)
                    }
                },
                {
                    FB_EmptyUnitBoxLayoutType.Double,
                    new[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0)
                    }
                },
                {
                    FB_EmptyUnitBoxLayoutType.Triple,
                    new[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0)
                    }
                },
                {
                    FB_EmptyUnitBoxLayoutType.LType1,
                    new[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 0)
                    }
                },
                {
                    FB_EmptyUnitBoxLayoutType.LType2,
                    new[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(1, 1)
                    }
                },
                {
                    FB_EmptyUnitBoxLayoutType.LType3,
                    new[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    }
                },
                {
                    FB_EmptyUnitBoxLayoutType.LType4,
                    new[]
                    {
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 0),
                        new Vector2Int(1, 1)
                    }
                }
            };

        [SerializeField] private UnitBox _unitBoxPrefab;
        [SerializeField] private float _spacing = DEFAULT_SPACING;
        [SerializeField] private FB_EmptyUnitBoxLayoutType _layoutType = FB_EmptyUnitBoxLayoutType.Single;
        [SerializeField] private List<UnitBox> _emptyUnitBoxes = new List<UnitBox>();

        public IReadOnlyList<UnitBox> EmptyUnitBoxes => _emptyUnitBoxes;
        public FB_EmptyUnitBoxLayoutType LayoutType => _layoutType;
        public float Spacing => _spacing;
        public static IReadOnlyList<FB_EmptyUnitBoxLayoutType> SpawnableLayoutTypes => SPAWNABLE_LAYOUT_TYPES;

        public FB_EmptyUnitBoxParentMovement _emptyUnitBoxMovement;
        public UnitBox UnitBox;
        public List<GridControlCollider> GridControlColliders = new List<GridControlCollider>();
        private bool _onGridTile;
        private bool isActive;
        
        private CancellationTokenSource _cancellationTokenSource;
        
        private void Awake()
        {
            if (_unitBoxPrefab == null)
            {
                _unitBoxPrefab = GetComponentInChildren<UnitBox>(true);
            }

            CacheExistingUnitBoxes();
            Init();
        }

        private void OnValidate()
        {
            _spacing = Mathf.Max(0.01f, _spacing);

            if (_unitBoxPrefab == null)
            {
                _unitBoxPrefab = GetComponentInChildren<UnitBox>(true);
            }

            CacheExistingUnitBoxes();
        }
        
        public void Disable()
        {
            if (_emptyUnitBoxMovement != null)
            {
                _emptyUnitBoxMovement.Disable();
            }

            _cancellationTokenSource?.Cancel();
            FB_EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnBoxReturnedToStart);
        }
        
        public void Init()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one * SPAWN_SCALE, SPAWN_DURATION).SetEase(Ease.InBounce);

            if (_emptyUnitBoxMovement != null)
            {
                _emptyUnitBoxMovement.Init();
            }

            FB_EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.RemoveListener(OnBoxReturnedToStart);
            FB_EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition.AddListener(OnBoxReturnedToStart);

            InitializeUnitBoxesWithRandomColors();
        }
        
        [ContextMenu("Rebuild Current Layout")]
        public void RebuildCurrentLayout()
        {
            BuildLayout(_layoutType, _spacing);
        }

        public void Initialize(FB_EmptyUnitBoxLayoutType layoutType, float spacing = DEFAULT_SPACING)
        {
            _layoutType = layoutType;
            _spacing = spacing > 0f ? spacing : DEFAULT_SPACING;
            BuildLayout(_layoutType, _spacing);
        }

        public void DetachUnitBox(UnitBox unitBox, bool worldPositionStays = true)
        {
            if (unitBox == null)
            {
                return;
            }

            unitBox.transform.SetParent(null, worldPositionStays);
            _emptyUnitBoxes.Remove(unitBox);
        }

        public void DetachAllUnitBoxes(bool worldPositionStays = true)
        {
            for (int i = 0; i < _emptyUnitBoxes.Count; i++)
            {
                if (_emptyUnitBoxes[i] == null)
                {
                    continue;
                }

                _emptyUnitBoxes[i].transform.SetParent(null, worldPositionStays);
            }

            _emptyUnitBoxes.Clear();
        }

        private void BuildLayout(FB_EmptyUnitBoxLayoutType layoutType, float spacing)
        {
            if (!LAYOUT_CELLS.TryGetValue(layoutType, out Vector2Int[] cells))
            {
                Debug.LogError($"[FB_EmptyUnitBoxParent] Layout tanimi bulunamadi: {layoutType}", this);
                return;
            }

            List<UnitBox> existingUnitBoxes = new List<UnitBox>(GetComponentsInChildren<UnitBox>(true));
            UnitBox template = ResolveTemplate(existingUnitBoxes);

            if (template == null)
            {
                Debug.LogError("[FB_EmptyUnitBoxParent] FB_EmptyUnitBox prefab/template bulunamadi.", this);
                return;
            }

            Vector2 center = CalculateBoundsCenter(cells);
            _emptyUnitBoxes.Clear();

            for (int i = 0; i < cells.Length; i++)
            {
                UnitBox unitBox = GetOrCreateUnitBox(i, existingUnitBoxes, template);

                if (unitBox == null)
                {
                    continue;
                }

                SetupUnitBox(unitBox, cells[i], center, spacing, i);
                _emptyUnitBoxes.Add(unitBox);
            }

            RemoveUnusedUnitBoxes(existingUnitBoxes, cells.Length);
            RefreshUnitBoxCollections();
            InitializeUnitBoxesWithRandomColors();
        }

        private void CacheExistingUnitBoxes()
        {
            _emptyUnitBoxes.Clear();
            _emptyUnitBoxes.AddRange(GetComponentsInChildren<UnitBox>(true));
            UnitBox = _emptyUnitBoxes.Count > 0 ? _emptyUnitBoxes[0] : null;
            RefreshGridControlColliders();
        }

        private void RefreshGridControlColliders()
        {
            GridControlColliders.Clear();

            for (int i = 0; i < _emptyUnitBoxes.Count; i++)
            {
                UnitBox emptyUnitBox = _emptyUnitBoxes[i];
                if (emptyUnitBox == null)
                {
                    continue;
                }

                IReadOnlyList<GridControlCollider> colliders = emptyUnitBox.Colliders;
                for (int j = 0; j < colliders.Count; j++)
                {
                    GridControlCollider collider = colliders[j];
                    if (collider != null)
                    {
                        GridControlColliders.Add(collider);
                    }
                }
            }
        }

        private void RefreshUnitBoxCollections()
        {
            CacheExistingUnitBoxes();
        }

        private void InitializeUnitBoxesWithRandomColors()
        {
            if (_emptyUnitBoxes.Count == 0 || AVAILABLE_COLOR_TYPES.Length == 0)
            {
                return;
            }

            ColorType firstAssignedColor = GetRandomColorType();
            bool hasDifferentColor = false;

            for (int i = 0; i < _emptyUnitBoxes.Count; i++)
            {
                if (_emptyUnitBoxes[i] == null)
                {
                    continue;
                }

                ColorType randomColor = GetRandomColorType();
                _emptyUnitBoxes[i].Init(randomColor);

                if (i == 0)
                {
                    firstAssignedColor = randomColor;
                }
                else if (randomColor != firstAssignedColor)
                {
                    hasDifferentColor = true;
                }
            }

            if (_emptyUnitBoxes.Count > 1 && !hasDifferentColor && AVAILABLE_COLOR_TYPES.Length > 1)
            {
                UnitBox lastUnitBox = _emptyUnitBoxes[_emptyUnitBoxes.Count - 1];
                if (lastUnitBox != null)
                {
                    lastUnitBox.Init(GetRandomColorTypeExcept(firstAssignedColor));
                }
            }
        }

        private static ColorType GetRandomColorType()
        {
            return AVAILABLE_COLOR_TYPES[Random.Range(0, AVAILABLE_COLOR_TYPES.Length)];
        }

        private static ColorType GetRandomColorTypeExcept(ColorType excludedColor)
        {
            if (AVAILABLE_COLOR_TYPES.Length <= 1)
            {
                return excludedColor;
            }

            ColorType randomColor = excludedColor;
            while (randomColor == excludedColor)
            {
                randomColor = GetRandomColorType();
            }

            return randomColor;
        }

        private UnitBox ResolveTemplate(List<UnitBox> existingUnitBoxes)
        {
            if (_unitBoxPrefab != null)
            {
                return _unitBoxPrefab;
            }

            if (existingUnitBoxes.Count == 0)
            {
                return null;
            }

            _unitBoxPrefab = existingUnitBoxes[0];
            return _unitBoxPrefab;
        }

        private UnitBox GetOrCreateUnitBox(int index, List<UnitBox> existingUnitBoxes, UnitBox template)
        {
            if (index < existingUnitBoxes.Count)
            {
                UnitBox existingUnitBox = existingUnitBoxes[index];
                existingUnitBox.gameObject.SetActive(true);
                return existingUnitBox;
            }

            return Instantiate(template, transform);
        }

        private void SetupUnitBox(UnitBox unitBox, Vector2Int cell, Vector2 center, float spacing, int index)
        {
            Transform unitBoxTransform = unitBox.transform;
            unitBoxTransform.SetParent(transform, false);
            unitBoxTransform.localRotation = Quaternion.identity;
            unitBoxTransform.localScale = Vector3.one;
            unitBoxTransform.localPosition = CalculateLocalPosition(cell, center, spacing);
            unitBox.name = $"FB_EmptyUnitBox_{index}";
        }

        private void RemoveUnusedUnitBoxes(List<UnitBox> existingUnitBoxes, int requiredCount)
        {
            for (int i = requiredCount; i < existingUnitBoxes.Count; i++)
            {
                if (existingUnitBoxes[i] == null)
                {
                    continue;
                }

                DestroyUnitBox(existingUnitBoxes[i].gameObject);
            }
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

        private static Vector3 CalculateLocalPosition(Vector2Int cell, Vector2 center, float spacing)
        {
            float localX = (cell.x - center.x) * spacing;
            float localZ = (center.y - cell.y) * spacing;
            return new Vector3(localX, 0f, localZ);
        }

        private static void DestroyUnitBox(GameObject unitBoxObject)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(unitBoxObject);
                return;
            }

            Object.DestroyImmediate(unitBoxObject);
        }
        
        public void Selected(Vector3 pos)
        {
            RefreshUnitBoxCollections();
            _emptyUnitBoxMovement.HandleMouseDown(pos);
            SetSelectableColliderActive(false);
            TrackInput().Forget();
        
            FB_EmptyBoxSignals.OnRemovedEmptyBox?.Dispatch(this);
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
            gridControlCollider.GridTile?.IsGridOkeyAndNotOkey(isPlacementValid);
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
            PlacementTarget placementTarget = placementTargets[i];
            placementTarget.UnitBox.JumpToGridTile(placementTarget.MainCollider.GridTile);
        }

        GridControlColliders.Clear();
        _emptyUnitBoxes.Clear();
        FB_EmptyBoxSignals.OnTheEmptyBoxRemoved?.Dispatch(this);
        DestroySelf().Forget();
        EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch();
        EmptyBoxSignals.OnFailConditionCheck?.Dispatch();
    }

    private bool TryCollectPlacementTargets(out List<PlacementTarget> placementTargets)
    {
        placementTargets = new List<PlacementTarget>();
        HashSet<GridTile> targetedTiles = new HashSet<GridTile>();

        for (int i = 0; i < _emptyUnitBoxes.Count; i++)
        {
            UnitBox emptyUnitBox = _emptyUnitBoxes[i];
            if (emptyUnitBox == null)
            {
                return false;
            }

            GridControlCollider mainCollider = emptyUnitBox.GetMainGridControlCollider();
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
                UnitBox = emptyUnitBox,
                MainCollider = mainCollider
            });
        }

        return placementTargets.Count > 0;
    }

    private void ResetToStart()
    {
        foreach (var gridControlCollider in GridControlColliders)
        {
            if (gridControlCollider != null)
            {
                gridControlCollider.GridTile = null;
            }
        }

        FB_EmptyBoxSignals.OnAddedEmptyBox?.Dispatch(this);

        if (_emptyUnitBoxMovement != null)
        {
            _emptyUnitBoxMovement.ReturnToStart();
            return;
        }

        SetSelectableColliderActive(true);
    }
    
    private void OnBoxReturnedToStart(FB_EmptyUnitBoxParentMovement emptyBoxMovement)
    {
        if(emptyBoxMovement != _emptyUnitBoxMovement) return;
        SetSelectableColliderActive(true);
    }
    
    private async UniTaskVoid DestroySelf()
    {
        if (this == null || _emptyUnitBoxMovement == null) return;

        Disable();
        isActive = false;
        _cancellationTokenSource?.Cancel();

        _emptyUnitBoxMovement.enabled = false;

        await UniTask.DelayFrame(1);

        if (this != null)
            Destroy(gameObject);
    }
   }
}
