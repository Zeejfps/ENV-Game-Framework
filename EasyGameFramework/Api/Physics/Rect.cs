using System.Numerics;

namespace EasyGameFramework.Api.Physics;

public readonly struct Rect
{
    public Vector2 Position { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float Left => Position.X;
    public float Right => Position.X + Width;

    public bool Contains(Vector2 point)
    {
        return IsPointInRect(this, point);
    }
    
    public static bool IsPointInRect(in Rect rect, Vector2 point)
    {
        var xMin = rect.Position.X;
        var xMax = rect.Position.X + rect.Width;

        var yMin = rect.Position.Y;
        var yMax = rect.Position.Y + rect.Height;
        
        return point.X >= xMin && point.Y >= yMin && point.X < xMax && point.Y < yMax;
    }
}