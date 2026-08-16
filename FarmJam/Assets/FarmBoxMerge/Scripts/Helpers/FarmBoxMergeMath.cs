using UnityEngine;

public static class FarmBoxMergeMath
{
    public static float EaseOutCubic(float value)
    {
        float normalized = Mathf.Clamp01(value);
        return 1f - Mathf.Pow(1f - normalized, 3f);
    }

    public static float SmoothStep(float value)
    {
        float normalized = Mathf.Clamp01(value);
        return normalized * normalized * (3f - (2f * normalized));
    }

    public static float DampFactor(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Mathf.Max(0f, deltaTime));
    }

    public static float SqrColorDistance(Color first, Color second)
    {
        float red = first.r - second.r;
        float green = first.g - second.g;
        float blue = first.b - second.b;
        float alpha = first.a - second.a;
        return (red * red) + (green * green) + (blue * blue) + (alpha * alpha);
    }
}
