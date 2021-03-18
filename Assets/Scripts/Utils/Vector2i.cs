using System;

namespace Utils
{
    public struct Vector2i
    {
        public int x;
        public int y;

        public Vector2i(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(Vector2i lhs, Vector2i rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator !=(Vector2i lhs, Vector2i rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Vector2i other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2i other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static float Distance(Vector2i a, float bx, float by)
        {
            var num1 = a.x - bx;
            var num2 = a.y - by;
            return (float) Math.Sqrt(num1 * (double) num1 + num2 * (double) num2);
        }
    }
}