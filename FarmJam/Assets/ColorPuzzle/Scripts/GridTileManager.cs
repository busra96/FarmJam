using System.Collections.Generic;
using UnityEngine;

namespace ColorPuzzle
{
    public class GridTileManager : MonoBehaviour
    {
        public List<GridTile> TileList = new List<GridTile>();
        public GridTile GridTilePrefab;
        public int xValue;
        public int yValue;
        public Transform MiddlePoint;

        public Material DefaultMat;

        public void Start()
        {
            SpawnGrids(MiddlePoint);
        }
        public void Disable()
        {
            
        }

        public void SpawnGrids( Transform parentTransform)
        {
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
            Vector3 pos = new Vector3(x *2,0, y * 2* -1f) + new Vector3(x * .1f, 0,  y * -.1f);
            Quaternion rot = Quaternion.identity;
            GridTile gridTile =  CreateGridTile(GridTilePrefab, pos, rot, transform);
            gridTile.SetMaterial(DefaultMat);
          
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
        
        public GridTile CreateGridTile(GridTile gridTile, Vector3 pos, Quaternion rot ,Transform parent)
        {
            GridTile gridTileComponent = Object.Instantiate(gridTile,pos, rot, parent);
            return gridTileComponent;
        }
    }
}

