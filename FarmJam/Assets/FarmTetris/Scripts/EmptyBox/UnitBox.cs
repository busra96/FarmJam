namespace FarmTetris
{
    using System.Collections.Generic;
    using DG.Tweening;
    using UnityEngine;

    public class UnitBox : MonoBehaviour
    {
        private const float DESTROY_DURATION = 0.2f;
        private const float INITIAL_SCALE = 1f;

        public bool IsFull;
        public GameObject UnitBoxModel;
        private GridTile GridTile;
        private readonly List<GridTile> GridTiles = new List<GridTile>();
        [SerializeField] private List<GridControlCollider> GridControlColliders;

        public List<UnityBoxPoint> Points;
        private bool onDestroyed;

        public UnitBoxColorTypeAndMat UnitBoxColorTypeAndMat;
        public UnitBoxAudio UnitBoxAudio;
        public IReadOnlyList<GridControlCollider> Colliders => GridControlColliders;
        public GridTile CurrentGridTile => GridTile;
        public IReadOnlyList<UnitBoxNeighbor> Neighbors => _neighbors;

        [SerializeField] private List<DirectionMeshPair> directionMeshes;
        private readonly List<UnitBoxNeighbor> _neighbors = new List<UnitBoxNeighbor>();
        private readonly Dictionary<Direction, UnitBox> _neighborLookup = new Dictionary<Direction, UnitBox>();
        
        public void Init(ColorType colorType)
        {
            onDestroyed = false;
            IsFull = false;
            GridTiles.Clear();
            GridTile = null;
            ClearNeighbors();

            foreach (var point in Points)
            {
                point.Init(this);
            }

            UnitBoxColorTypeAndMat.ColorType = colorType;
            UnitBoxColorTypeAndMat.ActiveColor();
            UnitBoxSignals.OnThisUnitBoxIsFullCheck.RemoveListener(CheckIsFull);
            UnitBoxSignals.OnThisUnitBoxIsFullCheck.AddListener(CheckIsFull);
        }

        private void OnDisable()
        {
            UnitBoxSignals.OnThisUnitBoxIsFullCheck.RemoveListener(CheckIsFull);
        }
       
        public void JumpToGridTile(GridTile tile)
        {
            GridTiles.Clear();
            GridTile = tile;
            transform.SetParent(GridTile.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * INITIAL_SCALE;

            foreach (var collider in GridControlColliders)
            {
                if (collider == null || collider.GridTile == null)
                {
                    continue;
                }

                GridTiles.Add(collider.GridTile);
                collider.GridTile.UnitBox = this;
            }

            GridTileSignals.OnAddedUnitBox?.Dispatch(this);
            LevelManagerSignals.OnLevelWinFailCheckTimerRestart?.Dispatch();
        }

        public void CheckIsFull()
        {
            if (onDestroyed)
            {
                return;
            }

            bool isFull = true;
            foreach (var point in Points)
            {
                if (point == null || point.Collectable == null || point.Collectable.isJumping)
                {
                    isFull = false;
                    break;
                }
            }

            IsFull = isFull;
        }

        public void PopFromGrid()
        {
            if (onDestroyed)
            {
                return;
            }

            onDestroyed = true;
            IsFull = true;
            NotifyNeighborsAboutRemoval();
            ClearGridReferences();
            GridTileSignals.OnRemovedUnitBox?.Dispatch(this);
            UnitBoxSignals.OnThisUnitBoxDestroyed?.Dispatch(this);
            DestroyAnim();
        }

        private void DestroyAnim()
        {
            UnitBoxAudio.PlayWinClip();
            UnitBoxModel.transform.DOScale(Vector3.zero, DESTROY_DURATION)
                .SetEase(Ease.Linear)
                .OnComplete(() => Destroy(gameObject));
        }

        public GridControlCollider GetMainGridControlCollider()
        {
            return GridControlColliders.Find(collider => collider != null && collider.IsMain);
        }

        public UnityBoxPoint GetEmptyBoxPoint()
        {
            foreach (var unitBoxPoint in Points)
            {
                if (unitBoxPoint.Collectable == null)
                {
                    return unitBoxPoint;
                }
            }

            return null;
        }

        public void ClearNeighbors()
        {
            _neighbors.Clear();
            _neighborLookup.Clear();
            RefreshDirectionMeshes();
        }

        public void SetNeighbor(Direction direction, UnitBox neighbor)
        {
            if (neighbor == null)
            {
                RemoveNeighbor(direction);
                return;
            }

            _neighborLookup[direction] = neighbor;

            bool updatedExisting = false;
            for (int i = 0; i < _neighbors.Count; i++)
            {
                if (_neighbors[i].Direction != direction)
                {
                    continue;
                }

                _neighbors[i] = new UnitBoxNeighbor
                {
                    Direction = direction,
                    Neighbor = neighbor
                };
                updatedExisting = true;
                break;
            }

            if (!updatedExisting)
            {
                _neighbors.Add(new UnitBoxNeighbor
                {
                    Direction = direction,
                    Neighbor = neighbor
                });
            }

            RefreshDirectionMeshes();
        }

        public void RemoveNeighbor(Direction direction)
        {
            _neighborLookup.Remove(direction);
            _neighbors.RemoveAll(neighbor => neighbor.Direction == direction);
            RefreshDirectionMeshes();
        }

        public UnitBox GetNeighbor(Direction direction)
        {
            _neighborLookup.TryGetValue(direction, out UnitBox neighbor);
            return neighbor;
        }

        public void RefreshDirectionMeshes()
        {
            for (int i = 0; i < directionMeshes.Count; i++)
            {
                DirectionMeshPair directionMeshPair = directionMeshes[i];
                if (directionMeshPair == null || directionMeshPair.mesh == null)
                {
                    continue;
                }

                bool hasNeighbor = _neighborLookup.TryGetValue(directionMeshPair.direction, out UnitBox neighbor) && neighbor != null;
                directionMeshPair.mesh.SetActive(!hasNeighbor);
            }
        }

        private void ClearGridReferences()
        {
            for (int i = 0; i < GridTiles.Count; i++)
            {
                if (GridTiles[i] != null && GridTiles[i].UnitBox == this)
                {
                    GridTiles[i].UnitBox = null;
                    GridTiles[i].SetDefaultMat();
                }
            }

            for (int i = 0; i < GridControlColliders.Count; i++)
            {
                if (GridControlColliders[i] != null)
                {
                    GridControlColliders[i].GridTile = null;
                }
            }

            GridTiles.Clear();
            GridTile = null;
            ClearNeighbors();
        }

        private void NotifyNeighborsAboutRemoval()
        {
            List<UnitBoxNeighbor> existingNeighbors = new List<UnitBoxNeighbor>(_neighbors);

            for (int i = 0; i < existingNeighbors.Count; i++)
            {
                UnitBoxNeighbor unitBoxNeighbor = existingNeighbors[i];
                if (unitBoxNeighbor.Neighbor == null)
                {
                    continue;
                }

                unitBoxNeighbor.Neighbor.RemoveNeighbor(GetOppositeDirection(unitBoxNeighbor.Direction));
            }
        }

        private static Direction GetOppositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                default:
                    return direction;
            }
        }
    }
    
    [System.Serializable]
    public class DirectionMeshPair
    {
        public Direction direction;
        public GameObject mesh;
    }

    [System.Serializable]
    public struct UnitBoxNeighbor
    {
        public Direction Direction;
        public UnitBox Neighbor;
    }
}
