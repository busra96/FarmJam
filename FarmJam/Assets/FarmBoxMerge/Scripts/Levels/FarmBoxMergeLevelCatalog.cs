using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmBoxMergeLevelCatalog", menuName = "FarmBoxMerge/Level Catalog")]
public class FarmBoxMergeLevelCatalog : ScriptableObject
{
    [SerializeField] private List<FarmBoxMergeLevelDefinition> levels = new List<FarmBoxMergeLevelDefinition>();

    public IReadOnlyList<FarmBoxMergeLevelDefinition> Levels => levels;
    public int Count => levels != null ? levels.Count : 0;

    public FarmBoxMergeLevelDefinition GetLevel(int index)
    {
        return index >= 0 && index < Count ? levels[index] : null;
    }

    public int NormalizeIndex(int index, bool loop)
    {
        if (Count == 0)
        {
            return -1;
        }

        return loop ? ((index % Count) + Count) % Count : Mathf.Clamp(index, 0, Count - 1);
    }
}
