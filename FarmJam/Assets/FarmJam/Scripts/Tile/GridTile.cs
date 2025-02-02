using Signals;
using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Tile Tile;

    public MeshRenderer _MeshRenderer;

    public Material GreenMat, RedMat;
    private Material DefaultMat;
    
    public bool OnThisGridHasEmptyBox;
    public EmptyBox EmptyBox;

    public GridControlCollider GridControlCollider;

    private void OnEnable()
    {
        GridTileSignals.OnGridTileMaterialColorCheck.AddListener(OnGridTileMaterialColorCheck);
    }
    private void OnDisable()
    {
        GridTileSignals.OnGridTileMaterialColorCheck.RemoveListener(OnGridTileMaterialColorCheck);
    }
    
    private void OnGridTileMaterialColorCheck()
    {
        Debug.Log("OnGridTileMaterialColorCheck ");
        if(GridControlCollider == null)
            SetDefaultMat();
    }

    public void SetGridControlCollider(GridControlCollider controlCollider)
    {
        GridControlCollider = controlCollider;
    }

    public void SetMaterial(Material material)
    {
         DefaultMat = material;
        _MeshRenderer.material = material;
    }

    public void SetEmptyBox(EmptyBox emptyBox)
    {
        EmptyBox = emptyBox;
    }

    public void SetGreenMat()
    {
        _MeshRenderer.material = GreenMat;
    }

    public void SetRedMat()
    {
        _MeshRenderer.material = RedMat;
    }

    public void SetDefaultMat()
    {
        _MeshRenderer.material = DefaultMat;
    }

    public void IsGridOkeyAndNotOkey(bool isGridOkey)
    {
        if (isGridOkey) SetGreenMat();
        else SetRedMat();
    }
}