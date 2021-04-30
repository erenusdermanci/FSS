using System;

namespace Entities.Pathfinding
{
    public class Node : IComparable<Node>
    {
        public readonly int X;
        public readonly int Y;
        public int Weight;

        public Node(int x, int y)
        {
            X = x;
            Y = y;
            Weight = 0;
        }

        public static bool operator ==(Node lhs, Node rhs)
        {
            return lhs?.X == rhs?.X && lhs?.Y == rhs?.Y;
        }

        public static bool operator !=(Node lhs, Node rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Node other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Node other && Equals(other);
        }

        public int CompareTo(Node other)
        {
            if (other == null)
                return -1;

            return Weight.CompareTo(other.Weight);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }
    }
}
