using UnityEngine;

public static class FarmBoxMergeRules
{
    public const int MinCardCounter = 1;
    public const int MaxCardCounter = 4;
    public const int MaxCardsOnBoard = 12;

    public static int ClampCardCounter(int counter)
    {
        return Mathf.Clamp(counter, MinCardCounter, MaxCardCounter);
    }

    public static bool CanIncreaseCardCounter(int counter)
    {
        return counter < MaxCardCounter;
    }
}
