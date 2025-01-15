using UnityEngine;

public class GridTileFactory
{
    public GridTile CreateGridTile(GridTile gridTile, Vector3 pos, Quaternion rot ,Transform parent)
    {
        GridTile gridTileComponent = Object.Instantiate(gridTile,pos, rot, parent);
        return gridTileComponent;
    }
}