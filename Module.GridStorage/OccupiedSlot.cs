using System;
using System.Collections.Generic;

namespace GridStorageModule
{
    public struct OccupiedSlot<TItem> : IEquatable<OccupiedSlot<TItem>>
    {
        public Point Origin { get; set; }
        public Size Size { get; set; }
        public TItem Item { get; set; }

        public bool Equals(OccupiedSlot<TItem> other)
        {
            return Origin.Equals(other.Origin) && Size.Equals(other.Size) && EqualityComparer<TItem>.Default.Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            return obj is OccupiedSlot<TItem> other && Equals(other);
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

        public static bool operator ==(OccupiedSlot<TItem> left, OccupiedSlot<TItem> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OccupiedSlot<TItem> left, OccupiedSlot<TItem> right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Origin: {Origin}, Size: {Size}, Item: {Item}";
        }
    }
}