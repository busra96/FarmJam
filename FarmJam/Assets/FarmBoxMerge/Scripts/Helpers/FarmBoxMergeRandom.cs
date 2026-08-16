using System;
using UnityEngine;

public static class FarmBoxMergeRandom
{
    private static readonly ColorType[] ColorTypes = (ColorType[])Enum.GetValues(typeof(ColorType));

    public static ColorType ColorType()
    {
        return ColorTypes.Length == 0
            ? default
            : ColorTypes[UnityEngine.Random.Range(0, ColorTypes.Length)];
    }
}
