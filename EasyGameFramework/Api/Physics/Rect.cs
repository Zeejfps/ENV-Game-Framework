using System.Numerics;

namespace EasyGameFramework.Api.Physics;

public readonly struct Rect
{
    public Vector2 BottomLeft { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float HalfWidth => Width * 0.5f;
    public float HalfHeight => Height * 0.5f;
    public float Left => BottomLeft.X;
    public float Right => BottomLeft.X + Width;
    public float Top => BottomLeft.Y + Height;
    public float Bottom => BottomLeft.Y;
    public Vector2 TopRight => BottomLeft + new Vector2(Width, Height);

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
}