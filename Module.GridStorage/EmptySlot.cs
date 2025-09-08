using System;

namespace GridStorageModule
{
    public struct EmptySlot : IEquatable<EmptySlot>
    {
        public Point Origin { get; set; }
        public Size Size { get; set; }

        public bool Equals(EmptySlot other)
        {
            return Origin.Equals(other.Origin) && Size.Equals(other.Size);
        }

        public override bool Equals(object obj)
        {
            return obj is EmptySlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Origin.GetHashCode() * 397) ^ Size.GetHashCode();
            }
        }

        public static bool operator ==(EmptySlot left, EmptySlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EmptySlot left, EmptySlot right)
        {
            return !left.Equals(right);
        }
    }
}