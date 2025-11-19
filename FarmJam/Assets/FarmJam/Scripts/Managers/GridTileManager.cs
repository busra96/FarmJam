using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class GridTileManager : MonoBehaviour
{
    [Inject] private GridTileFactory _gridTileFactory;

    public List<GridTile> TileList = new List<GridTile>();
    public GridTile GridTilePrefab;
    public int xValue;
    public int yValue;
    public Transform MiddlePoint;

    public Material GreyMat;
    public Material WhiteMat;

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
                SpawmnGridTile(i,j,parentTransform);
        }
    }

    private void SpawmnGridTile(int x, int y,Transform parentTransform)
    {
        Vector3 pos = new Vector3(x *2,0, y * 2* -1f);
        Quaternion rot = Quaternion.identity;
        GridTile gridTile =  _gridTileFactory.CreateGridTile(GridTilePrefab, pos, rot, transform);
        if((y + x) % 2 == 0) gridTile.SetMaterial(WhiteMat);
        else gridTile.SetMaterial(GreyMat);
      
        gridTile.Tile.GridPosition = new Vector2(x, y);
        gridTile.transform.parent = parentTransform;
        TileList.Add(gridTile);
    }

    private void CalculateMiddlePoint()
    {
        var XMiddlePos = (xValue - 1) * 2 *.5f;
        var ZMiddlePos = (yValue - 1) * -2 *.5f;
        MiddlePoint.position = new Vector3(XMiddlePos, 0, ZMiddlePos);

        for (int i = 0; i < TileList.Count; i++)
            TileList[i].transform.parent = MiddlePoint.transform;

        MiddlePoint.position = Vector3.zero;
    }
    
    private void LinkNeighbors()
    {
        foreach (var currentTile in TileList)
        {
            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2 neighborPosition = GetNeighborPosition(currentTile.Tile.GridPosition, direction);

                GridTile neighborTile = TileList.Find(t => t.Tile.GridPosition == neighborPosition);
                if (neighborTile != null)
                {
                    currentTile.Tile.AddNeighbor(direction, neighborTile);
                }
            }
        }
    }

    private Vector2 GetNeighborPosition(Vector2 currentPosition, Direction direction)
    {
        switch (direction)
        {
            case Direction.Down:
                return currentPosition + Vector2.up;
            case Direction.Up:
                return currentPosition + Vector2.down;
            case Direction.Left:
                return currentPosition + Vector2.left;
            case Direction.Right:
                return currentPosition + Vector2.right;
            default:
                return currentPosition;
        }
    }

    public void DestroyAllGrids()
    {
        if(TileList.Count == 0) return;
            
        for (int i = 0; i < TileList.Count; i++)
            Destroy(TileList[i].gameObject);
            
        TileList.Clear();
    }

    #region  EmptyBox a göre GridTile kalıp kalmadıgına bakma 
        /// <summary>
        /// Verilen EmptyBoxType için grid üzerinde (herhangi bir rotasyonda) 
        /// yerleştirilebilecek en az bir boş pozisyon var mı?
        /// </summary>
        public bool HasAnyValidPlacement(EmptyBoxType emptyBoxType)
        {
            if (!EmptyBoxRotationData.Shapes.TryGetValue(emptyBoxType, out var shape))
            {
                Debug.LogError($"[GridTileManager] Shape not found for type: {emptyBoxType}");
                return false;
            }

            // Griddeki her tile'i origin gibi düşün
            foreach (var originTile in TileList)
            {
                // Origin pozisyonu (grid koordinatı)
                Vector2Int originPos = Vector2Int.RoundToInt(originTile.Tile.GridPosition);

                // Bu shape’in tüm rotasyonlarını dene
                foreach (var rotationOffsets in shape.Rotations)
                {
                    if (CanPlaceShapeAt(originPos, rotationOffsets))
                    {
                        // En az 1 geçerli pozisyon bulduk → yer var
                        return true;
                    }
                }
            }

            // Hiçbir tile + rotasyon kombinasyonunda sığmadı → yer yok
            return false;
        }
        
        /// <summary>
        /// Belirli bir origin grid pozisyonunda ve verilen offset pattern’i ile
        /// bu shape'i yerleştirebiliyor muyuz?
        /// </summary>
        private bool CanPlaceShapeAt(Vector2Int origin, Vector2Int[] offsets)
        {
            foreach (var offset in offsets)
            {
                Vector2Int targetPos = origin + offset;

                GridTile targetTile = GetTileAt(targetPos);

                // Grid dışında kalıyorsa
                if (targetTile == null)
                    return false;

                // Hücre doluysa
                if (targetTile.UnitBox != null)
                    return false;
            }

            // Tüm hücreler grid içinde ve boş ise → buraya bu shape sığar
            return true;
        }
        

        /// <summary>
        /// Verilen grid koordinatındaki GridTile'ı bulur.
        /// (Performans gerekirse bunu dictionary'e çevirebiliriz.)
        /// </summary>
        private GridTile GetTileAt(Vector2Int gridPos)
        {
            return TileList.Find(t =>
            {
                Vector2Int pos = Vector2Int.RoundToInt(t.Tile.GridPosition);
                return pos == gridPos;
            });
        }
    #endregion
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
}