using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Tile
{
    public Vector2 GridPosition;
    
    [Header("Neighbors")]
    public List<Neighbor> Neighbors = new List<Neighbor>();
    
    public void AddNeighbor(Direction direction, GridTile neighborGridTile)
    {
        foreach (var n in Neighbors)
        {
            if (n.Direction == direction) return; 
        }

        Neighbor newNeighbor = new Neighbor
        {
            Direction = direction,
            GridTile = neighborGridTile
        };
        Neighbors.Add(newNeighbor);
    }

    public GridTile GetNeighbor(Direction direction)
    {
        foreach (var neighbor in Neighbors)
        {
            if (neighbor.Direction == direction)
                return neighbor.GridTile;
        }
        return null; 
    }
}

[Serializable]
public struct Neighbor
{
    public Direction Direction; 
    public GridTile GridTile; 
}