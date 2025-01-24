using System.Numerics;

namespace OpenGLSandbox;

public struct Rect : IEquatable<Rect>
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
    
    public float HalfWidth => Width * 0.5f;
    public float HalfHeight => Height * 0.5f;
    public float Left => X;
    public float Right => X + Width;
    public float Top => Y + Height;
    public float Bottom => Y;

    public Vector2 BottomLeft
    {
        get => new(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }
    public Vector2 TopRight => BottomLeft + new Vector2(Width, Height);
    
    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(int x, int y)
    {
        return Contains(new Vector2(x, y));
    }
    
    public bool Contains(float x, float y)
    {
        return Contains(new Vector2(x, y));
    }
    
    public bool Contains(Vector2 point)
    {
        return IsPointInRect(this, point);
    }
    
    public static bool IsPointInRect(in Rect rect, Vector2 point)
    {
        var xMin = rect.BottomLeft.X;
        var xMax = rect.BottomLeft.X + rect.Width;

        var yMin = rect.BottomLeft.Y;
        var yMax = rect.BottomLeft.Y + rect.Height;
        
        return point.X >= xMin && point.Y >= yMin && point.X < xMax && point.Y < yMax;
    }

    public override string ToString()
    {
        return $"{nameof(BottomLeft)}: {BottomLeft}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}";
    }

    public bool Equals(Rect other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        return obj is Rect other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    public static bool operator ==(Rect left, Rect right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rect left, Rect right)
    {
        return !left.Equals(right);
    }
}