using System.Numerics;

namespace Bricks;

public struct Rectangle
{
    public Vector2 Center { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float Left { get; private init; }
    public float Right { get; private set; }
    public float Top { get; private set; }
    public float Bottom { get; private set; }

    public bool Intersects(Rectangle b)
    {
        return Left < b.Right && Right > b.Left && Top < b.Bottom && Bottom > b.Top;
    }
    
    public static Rectangle LeftTopWidthHeight(float left, float top, float width, float height)
    {
        var halfWith = width * 0.5f;
        var halfHeight = height * 0.5f;
        return new Rectangle
        {
            Left = left,
            Top = top,
            Bottom = top + height,
            Right = left + width,
            Width = width,
            Height = height,
            Center = new Vector2(left + halfWith, top + halfHeight),
        };
    }
}