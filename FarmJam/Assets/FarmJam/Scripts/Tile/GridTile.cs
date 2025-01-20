using System;
using UnityEngine;


public class GridTile : MonoBehaviour
{
    public Tile Tile;

    public MeshRenderer _MeshRenderer;


    public void SetMaterial(Material material)
    {
        _MeshRenderer.material = material;
    }
}
