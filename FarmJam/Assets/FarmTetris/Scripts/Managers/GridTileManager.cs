namespace FarmTetris
{
    using System.Collections.Generic;
    using UnityEngine;
    using VContainer;

    public class GridTileManager : MonoBehaviour
    {
        private const float TILE_SPACING = 2f;

        [Inject] private readonly GridTileFactory _gridTileFactory;

        public List<GridTile> TileList = new List<GridTile>();
        private readonly Dictionary<Vector2Int, GridTile> _tileDict = new Dictionary<Vector2Int, GridTile>();
        public GridTile GridTilePrefab;
        public int xValue;
        public int yValue;
        public Transform MiddlePoint;

        public Material GreyMat;
        public Material WhiteMat;

        private void OnEnable()
        {
            GridTileSignals.OnUnitBoxStateCheckCompleted.AddListener(ResolveCompletedLines);
        }

        private void OnDisable()
        {
            GridTileSignals.OnUnitBoxStateCheckCompleted.RemoveListener(ResolveCompletedLines);
        }

        public void Init()
        {
        }

        public void Disable()
        {
        }

        public void SpawnGrids(int xCount, int yCount, Transform parentTransform)
        {
            xValue = xCount;
            yValue = yCount;

            GenerateGridTiles(parentTransform);
            LinkNeighbors();
            CalculateMiddlePoint();
        }

        private void GenerateGridTiles(Transform parentTransform)
        {
            for (int i = 0; i < xValue; i++)
            {
                for (int j = 0; j < yValue; j++)
                {
                    SpawnGridTile(i, j, parentTransform);
                }
            }
        }

        private void SpawnGridTile(int x, int y, Transform parentTransform)
        {
            Vector3 pos = new Vector3(x * TILE_SPACING, 0, y * TILE_SPACING * -1f);
            Quaternion rot = Quaternion.identity;
            GridTile gridTile = _gridTileFactory.CreateGridTile(GridTilePrefab, pos, rot, transform);

            Material material = (y + x) % 2 == 0 ? WhiteMat : GreyMat;
            gridTile.SetMaterial(material);

            gridTile.Tile.GridPosition = new Vector2(x, y);
            gridTile.transform.parent = parentTransform;

            Vector2Int gridPos = new Vector2Int(x, y);
            TileList.Add(gridTile);
            _tileDict[gridPos] = gridTile;
        }

        private void CalculateMiddlePoint()
        {
            float xMiddlePos = (xValue - 1) * TILE_SPACING * 0.5f;
            float zMiddlePos = (yValue - 1) * -TILE_SPACING * 0.5f;
            MiddlePoint.position = new Vector3(xMiddlePos, 0, zMiddlePos);

            foreach (var tile in TileList)
            {
                tile.transform.parent = MiddlePoint.transform;
            }

            MiddlePoint.position = Vector3.zero;
        }

        private void LinkNeighbors()
        {
            Direction[] directions = (Direction[])System.Enum.GetValues(typeof(Direction));
            
            foreach (var currentTile in TileList)
            {
                Vector2Int currentPos = Vector2Int.RoundToInt(currentTile.Tile.GridPosition);

                foreach (Direction direction in directions)
                {
                    Vector2Int neighborPosition = GetNeighborPosition(currentPos, direction);

                    if (_tileDict.TryGetValue(neighborPosition, out GridTile neighborTile))
                    {
                        currentTile.Tile.AddNeighbor(direction, neighborTile);
                    }
                }
            }
        }

        private Vector2Int GetNeighborPosition(Vector2Int currentPosition, Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return currentPosition + Vector2Int.up;
                case Direction.Up:
                    return currentPosition + Vector2Int.down;
                case Direction.Left:
                    return currentPosition + Vector2Int.left;
                case Direction.Right:
                    return currentPosition + Vector2Int.right;
                default:
                    return currentPosition;
            }
        }

        public void DestroyAllGrids()
        {
            if (TileList.Count == 0)
            {
                return;
            }

            foreach (var tile in TileList)
            {
                Destroy(tile.gameObject);
            }

            TileList.Clear();
            _tileDict.Clear();
        }

        public void ResolveCompletedLines()
        {
            HashSet<UnitBox> matchedUnitBoxes = FindCompletedLineUnitBoxes();
            if (matchedUnitBoxes.Count == 0)
            {
                return;
            }

            foreach (UnitBox matchedUnitBox in matchedUnitBoxes)
            {
                matchedUnitBox?.PopFromGrid();
            }
        }

        private HashSet<UnitBox> FindCompletedLineUnitBoxes()
        {
            HashSet<UnitBox> completedLineUnitBoxes = new HashSet<UnitBox>();

            for (int y = 0; y < yValue; y++)
            {
                if (TryGetCompletedRowUnitBoxes(y, out List<UnitBox> rowUnitBoxes))
                {
                    AddUnitBoxes(completedLineUnitBoxes, rowUnitBoxes);
                }
            }

            for (int x = 0; x < xValue; x++)
            {
                if (TryGetCompletedColumnUnitBoxes(x, out List<UnitBox> columnUnitBoxes))
                {
                    AddUnitBoxes(completedLineUnitBoxes, columnUnitBoxes);
                }
            }

            return completedLineUnitBoxes;
        }

        private bool TryGetCompletedRowUnitBoxes(int rowIndex, out List<UnitBox> rowUnitBoxes)
        {
            rowUnitBoxes = new List<UnitBox>();

            for (int x = 0; x < xValue; x++)
            {
                if (!TryGetTile(new Vector2Int(x, rowIndex), out GridTile gridTile) ||
                    gridTile.UnitBox == null ||
                    !gridTile.UnitBox.IsFull)
                {
                    rowUnitBoxes.Clear();
                    return false;
                }

                rowUnitBoxes.Add(gridTile.UnitBox);
            }

            return rowUnitBoxes.Count > 0;
        }

        private bool TryGetCompletedColumnUnitBoxes(int columnIndex, out List<UnitBox> columnUnitBoxes)
        {
            columnUnitBoxes = new List<UnitBox>();

            for (int y = 0; y < yValue; y++)
            {
                if (!TryGetTile(new Vector2Int(columnIndex, y), out GridTile gridTile) ||
                    gridTile.UnitBox == null ||
                    !gridTile.UnitBox.IsFull)
                {
                    columnUnitBoxes.Clear();
                    return false;
                }

                columnUnitBoxes.Add(gridTile.UnitBox);
            }

            return columnUnitBoxes.Count > 0;
        }

        private static void AddUnitBoxes(HashSet<UnitBox> targetSet, List<UnitBox> sourceList)
        {
            for (int i = 0; i < sourceList.Count; i++)
            {
                if (sourceList[i] != null)
                {
                    targetSet.Add(sourceList[i]);
                }
            }
        }

        private bool TryGetTile(Vector2Int gridPosition, out GridTile gridTile)
        {
            return _tileDict.TryGetValue(gridPosition, out gridTile);
        }

        #region EmptyBox Placement Validation

        public bool HasAnyValidPlacement(EmptyBoxType emptyBoxType)
        {
            if (!EmptyBoxRotationData.Shapes.TryGetValue(emptyBoxType, out var shape))
            {
                Debug.LogError($"[GridTileManager] Shape not found for type: {emptyBoxType}");
                return false;
            }

            foreach (var originTile in TileList)
            {
                Vector2Int originPos = Vector2Int.RoundToInt(originTile.Tile.GridPosition);

                foreach (var rotationOffsets in shape.Rotations)
                {
                    if (CanPlaceShapeAt(originPos, rotationOffsets))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanPlaceShapeAt(Vector2Int origin, Vector2Int[] offsets)
        {
            foreach (var offset in offsets)
            {
                Vector2Int targetPos = origin + offset;

                if (!_tileDict.TryGetValue(targetPos, out GridTile targetTile))
                {
                    return false;
                }

                if (targetTile.UnitBox != null)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
