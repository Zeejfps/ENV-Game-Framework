namespace ZnvQuadTree;

public readonly struct RectF : IEquatable<RectF>
{
    public RectF(float left, float bottom, float width, float height)
    {
        Left = left;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    public float Left { get; }
    public float Bottom { get; }
    public float Top => Bottom + Height;
    public float Right => Left + Width;
    public float Width { get; }
    public float Height { get;  }

    public bool Intersects(RectF otherRect)
    {
        if (Right <= otherRect.Left || Left >= otherRect.Right)
            return false; 

        if (Top <= otherRect.Bottom || Bottom >= otherRect.Top)
            return false;

        return true;
    }

    public bool FullyContains(RectF otherRect)
    {
        return otherRect.Left >= Left &&
               otherRect.Right <= Right &&
               otherRect.Bottom >= Bottom &&
               otherRect.Top <= Top;
    }
    
    public bool Contains(PointF point)
    {
        return point.X >= Left && point.X <= Right &&
               point.Y >= Bottom && point.Y <= Top;
    }
    
    public bool Equals(RectF other)
    {
        return Left.Equals(other.Left) && Bottom.Equals(other.Bottom) && Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        return obj is RectF other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Bottom, Width, Height);
    }

    public static bool operator ==(RectF left, RectF right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RectF left, RectF right)
    {
        return !left.Equals(right);
    }
}