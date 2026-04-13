using System.Collections.Generic;
using UnityEngine;

public enum MergeBoxPatternType
{
    Single,
    Line,
    L,
    T,
    Z,
    Square
}

public class MergeBoxParent : MonoBehaviour
{
    [SerializeField] private int counterValue;
    [SerializeField] private ColorType colorType;
    [SerializeField] private MergeBoxPatternType patternType;
    [SerializeField] private List<Box> boxes = new List<Box>();

    public int CounterValue => counterValue;
    public ColorType ColorType => colorType;
    public MergeBoxPatternType PatternType => patternType;
    public IReadOnlyList<Box> Boxes => boxes;

    public void Initialize(int newCounterValue, ColorType newColorType, MergeBoxPatternType newPatternType, IList<Box> spawnedBoxes)
    {
        counterValue = Mathf.Max(1, newCounterValue);
        colorType = newColorType;
        patternType = newPatternType;
        boxes.Clear();

        for (int i = 0; i < spawnedBoxes.Count; i++)
        {
            Box box = spawnedBoxes[i];
            if (box == null)
            {
                continue;
            }

            boxes.Add(box);
            box.Initialize(this, boxes.Count - 1, newColorType);
        }
    }
}
