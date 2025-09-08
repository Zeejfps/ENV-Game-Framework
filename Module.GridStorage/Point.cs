using System;

namespace Module.GridStorage
{
    public struct Point : IEquatable<Point>
    {
        public uint X { get; set; }
        public uint Y { get; set; }

        public static Point Of(uint x, uint y)
        {
            return new Point { X = x, Y = y };
        }


        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Point other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)X * 397) ^ (int)Y;
            }
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !left.Equals(right);
        }
    }
}