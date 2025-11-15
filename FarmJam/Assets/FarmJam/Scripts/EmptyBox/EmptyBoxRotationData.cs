using System.Collections.Generic;
using UnityEngine;

public static class EmptyBoxRotationData
{
    public static readonly Dictionary<EmptyBoxType, EmptyBoxShape> Shapes = new Dictionary<EmptyBoxType, EmptyBoxShape>
    {
        {
            EmptyBoxType.OnePiece,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.OnePiece,
                Rotations = new List<Vector2Int[]>
                {
                    //x
                    new Vector2Int[] { new Vector2Int(0, 0) }, //0 
                    new Vector2Int[] { new Vector2Int(0, 0) }, //90 
                    new Vector2Int[] { new Vector2Int(0, 0) }, //180
                    new Vector2Int[] { new Vector2Int(0, 0) }  //270
                }
            }
        },
        {
            EmptyBoxType.Small,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.Small,
                Rotations = new List<Vector2Int[]>
                {
                    // x x
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0)
                    }, 
                    // x
                    // x
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1)
                    }, 
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0)
                    }, 
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1)
                    }  
                }
            }
        },
        {
            EmptyBoxType.Square,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.Square,
                Rotations = new List<Vector2Int[]>
                {
                    // x x
                    // x x
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    }, 
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    }, 
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    }, 
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    }  
                }
            }
        },
        {
            EmptyBoxType.Middle,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.Middle,
                Rotations = new List<Vector2Int[]>
                {
                    // x x x
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0)
                    }, 
                    // x
                    // x
                    // x
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2)
                    }, 
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0)
                    }, 
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2)
                    }  
                }
            }
        },
        {
            EmptyBoxType.Large,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.Large,
                Rotations = new List<Vector2Int[]>
                {
                    // x x x x
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(3, 0)
                    }, 
                    // x
                    // x
                    // x
                    // x
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2),
                        new Vector2Int(0, 3)
                    }, 
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(3, 0)
                    }, 
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2),
                        new Vector2Int(0, 3)
                    }  
                }
            }
        },
        {
            EmptyBoxType.L,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.L,
                Rotations = new List<Vector2Int[]>
                {
                    // x 
                    // x x
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1)
                    }, 
                    // x x
                    // x
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, -1)
                    }, 
                    // x x 
                    //   x
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(-1, 0),
                        new Vector2Int(0, -1)
                    }, 
                    //   x
                    // x x
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(-1, 0),
                        new Vector2Int(0, 1)
                    }  
                }
            }
        },
        {
            EmptyBoxType.LargeL,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.LargeL,
                Rotations = new List<Vector2Int[]>
                {
                    // x
                    // x x x 
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, -1),
                        new Vector2Int(-1, 0),
                        new Vector2Int(-2, 0)
                    }, 
                    // x x
                    // x
                    // x
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(-1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2)
                    }, 
                    // x x x 
                    //     x
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(0, 1)
                    }, 
                    //    x
                    //    x
                    //  x x
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, -1),
                        new Vector2Int(0, -2)
                    }  
                }
            }
        },
        {
            EmptyBoxType.T,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.T,
                Rotations = new List<Vector2Int[]>
                {
                    // x x x
                    //   x  
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(-1, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, -1)
                    }, 
                    //   x 
                    // x x 
                    //   x 
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, -1),
                        new Vector2Int(-1, 0)
                    }, 
                    //   x
                    // x x x 
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(-1, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1)
                    }, 
                    // x
                    // x x
                    // x
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, -1),
                        new Vector2Int(1, 0)
                    }  
                }
            }
        },
        {
            EmptyBoxType.Z,
            new EmptyBoxShape
            {
                Type = EmptyBoxType.Z,
                Rotations = new List<Vector2Int[]>
                {
                    // x x 
                    //   x x 
                    new Vector2Int[] //0 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(-1, 1)
                    }, 
                    //   x 
                    // x x 
                    // x  
                    new Vector2Int[] //90 
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, -1),
                        new Vector2Int(1, 0),
                        new Vector2Int(1, 1)
                    }, 
                    // x x 
                    //   x x 
                    new Vector2Int[] //180
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(-1, 1)
                    }, 
                    //   x
                    // x x 
                    // x  
                    new Vector2Int[] //270
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, -1),
                        new Vector2Int(1, 0),
                        new Vector2Int(1, 1)
                    }  
                }
            }
        },
    };
    
    
    public static Vector2Int[] GetShapeCells(EmptyBoxType type, int angle)
    {
        if (!Shapes.TryGetValue(type, out EmptyBoxShape shape))
        {
            Debug.LogError($"Shape not found for type: {type}");
            return null;
        }

        int rotationIndex = Mathf.FloorToInt(angle / 90f) % 4;

        return shape.Rotations[rotationIndex];
    }
}



public class EmptyBoxShape
{
    public EmptyBoxType Type;
    public List<Vector2Int[]> Rotations = new List<Vector2Int[]>();
}
