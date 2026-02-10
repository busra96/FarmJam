using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Tile
{
    public Vector2 GridPosition;
    
    [Header("Neighbors")]
    public List<Neighbor> Neighbors = new List<Neighbor>();
    private Dictionary<Direction, GridTile> _neighborDict = new Dictionary<Direction, GridTile>();
    
    public void AddNeighbor(Direction direction, GridTile neighborGridTile)
    {
        if (_neighborDict.ContainsKey(direction))
            return;

        Neighbor newNeighbor = new Neighbor
        {
            Direction = direction,
            GridTile = neighborGridTile
        };
        Neighbors.Add(newNeighbor);
        _neighborDict[direction] = neighborGridTile;
    }

    public GridTile GetNeighbor(Direction direction)
    {
        _neighborDict.TryGetValue(direction, out GridTile neighbor);
        return neighbor;
    }
}

[Serializable]
public struct Neighbor
{
    public Direction Direction; 
    public GridTile GridTile; 
}