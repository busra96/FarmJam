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
        GenerateGridTiles();
        CalculateMiddlePoint();
    }

    public void Disable()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if(TileList.Count == 0) return;
            
            for (int i = 0; i < TileList.Count; i++)
                Destroy(TileList[i].gameObject);
            
            TileList.Clear();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            GenerateGridTiles();
            CalculateMiddlePoint();
        }
    }

    private void GenerateGridTiles()
    {
        for (int i = 0; i < xValue; i++)
        {
            for (int j = 0; j < yValue; j++)
                SpawmnGridTile(i,j);
        }
    }

    private void SpawmnGridTile(int x, int y)
    {
        Vector3 pos = new Vector3(x * 2.5f ,0, y * -1f * 2.5f);
        Quaternion rot = Quaternion.identity;
        GridTile gridTile =  _gridTileFactory.CreateGridTile(GridTilePrefab, pos, rot, transform);
        if((y + x) % 2 == 0) gridTile.SetMaterial(WhiteMat);
        else gridTile.SetMaterial(GreyMat);
        TileList.Add(gridTile);
    }

    private void CalculateMiddlePoint()
    {
        var XMiddlePos = (xValue - 1) * 2.5f * .5f;
        var ZMiddlePos = (yValue - 1) * -2.5f * .5f;
        MiddlePoint.position = new Vector3(XMiddlePos, 0, ZMiddlePos);

        for (int i = 0; i < TileList.Count; i++)
        {
            TileList[i].transform.parent = MiddlePoint.transform;
        }

        MiddlePoint.position = Vector3.zero;
    }
}