using System;
using Signals;
using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Tile Tile;

    public MeshRenderer _MeshRenderer;

    public Material GreenMat, RedMat;
    private Material DefaultMat;

    public UnitBox UnitBox;

    private void OnEnable()
    {
        GridTileSignals.OnGridMaterialCheck.AddListener(GridTileMaterialCheck);
    }
    private void OnDisable()
    {
        GridTileSignals.OnGridMaterialCheck.RemoveListener(GridTileMaterialCheck);
    }
    
    
    private void GridTileMaterialCheck()
    {
        if (UnitBox == null)
            SetDefaultMat();
    }

    public void SetMaterial(Material material)
    {
         DefaultMat = material;
        _MeshRenderer.material = material;
    }

    public void SetDefaultMat()
    {
        _MeshRenderer.material = DefaultMat;
    }

    public void IsGridOkeyAndNotOkey(bool isGridOkey)
    {
        if (isGridOkey)  _MeshRenderer.material = GreenMat;
        else _MeshRenderer.material = RedMat;
    }
}