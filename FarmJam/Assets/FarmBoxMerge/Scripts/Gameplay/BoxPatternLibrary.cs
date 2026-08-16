using UnityEngine;

public readonly struct BoxPatternDefinition
{
    public BoxPatternDefinition(MergeBoxPatternType patternType, Vector2Int[] cells)
    {
        PatternType = patternType;
        Cells = cells;
    }

    public MergeBoxPatternType PatternType { get; }
    public Vector2Int[] Cells { get; }
}

public static class BoxPatternLibrary
{
    private static readonly BoxPatternDefinition ThreeBoxPattern =
        new BoxPatternDefinition(MergeBoxPatternType.L, new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 1)
        });

    private static readonly BoxPatternDefinition[] FourBoxPatterns =
    {
        new BoxPatternDefinition(MergeBoxPatternType.Square, new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1)
        }),
        new BoxPatternDefinition(MergeBoxPatternType.L, new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 0)
        }),
        new BoxPatternDefinition(MergeBoxPatternType.T, new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 1)
        }),
        new BoxPatternDefinition(MergeBoxPatternType.Z, new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(1, 1), new Vector2Int(1, 2)
        })
    };

    public static BoxPatternDefinition Resolve(int boxCount, bool randomizeFourBoxPatterns)
    {
        int count = FarmBoxMergeRules.ClampCardCounter(boxCount);

        return count switch
        {
            1 => new BoxPatternDefinition(MergeBoxPatternType.Single, new[] { Vector2Int.zero }),
            2 => CreateLine(2),
            3 => ThreeBoxPattern,
            4 when randomizeFourBoxPatterns => FourBoxPatterns[Random.Range(0, FourBoxPatterns.Length)],
            4 => FourBoxPatterns[0],
            _ => FourBoxPatterns[0]
        };
    }

    public static Vector3[] GetCenteredPositions(Vector2Int[] cells, float spacing)
    {
        if (cells == null || cells.Length == 0)
        {
            return System.Array.Empty<Vector3>();
        }

        int minX = cells[0].x;
        int maxX = cells[0].x;
        int minY = cells[0].y;
        int maxY = cells[0].y;

        for (int i = 1; i < cells.Length; i++)
        {
            minX = Mathf.Min(minX, cells[i].x);
            maxX = Mathf.Max(maxX, cells[i].x);
            minY = Mathf.Min(minY, cells[i].y);
            maxY = Mathf.Max(maxY, cells[i].y);
        }

        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector3[] positions = new Vector3[cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2 centeredCell = cells[i] - center;
            positions[i] = new Vector3(centeredCell.x * spacing, 0f, centeredCell.y * spacing);
        }

        return positions;
    }

    private static BoxPatternDefinition CreateLine(int count)
    {
        Vector2Int[] cells = new Vector2Int[count];
        for (int i = 0; i < count; i++)
        {
            cells[i] = new Vector2Int(i, 0);
        }

        return new BoxPatternDefinition(MergeBoxPatternType.Line, cells);
    }
}
