using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Tile Tile;

    public MeshRenderer _MeshRenderer;

    public Material GreenMat, RedMat;
    private Material DefaultMat;
    
    public EmptyBox EmptyBox;

    public GridControlCollider GridControlCollider;

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