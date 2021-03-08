using System;
using UnityEngine;
using Random = System.Random;

namespace Utils
{
    public static class Helpers
    {
        private const float Epsilon = 0.01f;

        public static bool EqualsEpsilon(this float a, float b, float epsilon = Epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public static float GetRandomShiftAmount(Random rng, float baseAmount)
        {
            return baseAmount * ((float) rng.NextDouble() - 0.5f) * 2.0f;
        }
        
        public static byte ShiftColorComponent(int component, float amount)
        {
            return (byte)Mathf.Clamp(component + component * amount, 0.0f, 255.0f);
        }
    }
}