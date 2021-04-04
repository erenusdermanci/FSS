using System;

namespace Utils
{
    public static class Helpers
    {
        private const float Epsilon = 0.01f;

        public static bool EqualsEpsilon(this float a, float b, float epsilon = Epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public static int Mod(int n, int m)
        {
            var r = n % m;
            return r < 0 ? r + m : r;
        }
    }
}