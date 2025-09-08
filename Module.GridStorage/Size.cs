using System;

namespace GridStorageModule
{
    public struct Size : IEquatable<Size>
    {
        public uint Width { get; set; }
        public uint Height { get; set; }

        public static Size Of(uint width, uint height)
        {
            return new Size { Width = width, Height = height };
        }

        public bool Equals(Size other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is Size other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Width * 397) ^ (int)Height;
            }
        }

        public static bool operator ==(Size left, Size right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Size left, Size right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}