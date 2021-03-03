using System;

public static class Helpers
{
    public const float Epsilon = 0.01f;

    public static bool EqualsEpsilon(this float a, float b, float epsilon = Epsilon)
    {
        return Math.Abs(a - b) < epsilon;
    }
}