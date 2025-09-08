using System;
using System.Collections.Generic;

namespace Module.GridStorage
{
    public struct Slot<TItem> : IEquatable<Slot<TItem>>
    {
        public Point Origin { get; set; }
        public Size Size { get; set; }
        public TItem Item { get; set; }

        public bool Equals(Slot<TItem> other)
        {
            return Origin.Equals(other.Origin) && Size.Equals(other.Size) && EqualityComparer<TItem>.Default.Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            return obj is Slot<TItem> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Origin.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<TItem>.Default.GetHashCode(Item);
                return hashCode;
            }
        }

        public static bool operator ==(Slot<TItem> left, Slot<TItem> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Slot<TItem> left, Slot<TItem> right)
        {
            return !left.Equals(right);
        }
    }
}