using System.Numerics;

namespace NodeGraphApp;

public readonly struct ScreenRect : IEquatable<ScreenRect>
{
    public required float Left { get; init; }
    public required float Bottom { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public float Top => Bottom + Height;
    public float Right => Left + Width;

    public bool Equals(ScreenRect other)
    {
        return Left.Equals(other.Left) && Bottom.Equals(other.Bottom) && Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        return obj is ScreenRect other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Bottom, Width, Height);
    }

    public static bool operator ==(ScreenRect left, ScreenRect right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScreenRect left, ScreenRect right)
    {
        return !left.Equals(right);
    }

    public static ScreenRect FromLeftBottomTopRight(float left, float bottom, float top, float right)
    {
        return new ScreenRect
        {
            Left = left,
            Bottom = bottom,
            Width = right - left,
            Height = top - bottom,
        };
    }

    public static ScreenRect FromLBWH(float left, float bottom, float width, float height)
    {
        return new ScreenRect
        {
            Left = left,
            Bottom = bottom,
            Width = width,
            Height = height,
        };
    }
    
    public static ScreenRect FromLeftTopWidthHeight(float left, float top, float width, float height)
    {
        return new ScreenRect
        {
            Left = left,
            Bottom = top - height,
            Width = width,
            Height = height,
        };
    }

    public override string ToString()
    {
        return
            $"{nameof(Left)}: {Left}, {nameof(Bottom)}: {Bottom}, {nameof(Top)}: {Top}, {nameof(Right)}: {Right}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}";
    }

    public bool Contains(Vector2 point)
    {
        if (Right < point.X)
            return false;
        if (Top < point.Y)
            return false;
        if (Left > point.X)
            return false;
        if (Bottom > point.Y)
            return false;
        return true;
    }
}