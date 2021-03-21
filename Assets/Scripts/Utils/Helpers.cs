using System;
using UnityEngine;

namespace Utils
{
    public static class Helpers
    {
        private const float Epsilon = 0.01f;

        public static bool EqualsEpsilon(this float a, float b, float epsilon = Epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public static float GetRandomShiftAmount(float baseAmount)
        {
            return baseAmount * ((float) StaticRandom.Get().NextDouble() - 0.5f) * 2.0f;
        }

        public static byte ShiftColorComponent(byte component, float amount)
        {
            return (byte)Mathf.Clamp(component + component * amount, 0.0f, 255.0f);
        }

        public static int Mod(int n, int m)
        {
            var r = n % m;
            return r < 0 ? r + m : r;
        }
    }
}