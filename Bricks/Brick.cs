using System.Numerics;
using Bricks;

public sealed class Brick
{
    public Vector2 Position { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }

    public Rectangle CalculateBoundsRectangle()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var left = Position.X - halfWidth;
        var top = Position.Y - halfHeight;
        return Rectangle.LeftTopWidthHeight(left, top, Width, Height);
    }
}