using System.Numerics;

namespace Bricks.PhysicsModule;

public struct AABB
{
    public Vector2 Center { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float Left { get; private init; }
    public float Right { get; private set; }
    public float Top { get; private set; }
    public float Bottom { get; private set; }
    
    public Vector2 BottomLeft => new Vector2(Left, Bottom);
    public Vector2 TopRight => new Vector2(Right, Top);

    public static AABB Expand(AABB aabb, float dx, float dy)
    {
        return FromLeftTopWidthHeight(aabb.Left - dx, aabb.Top - dx, aabb.Width + dx + dx, aabb.Height + dy + dy);
    }
    
    public bool Intersects(AABB b)
    {
        return Left < b.Right && Right > b.Left && Top < b.Bottom && Bottom > b.Top;
    }
    
    public static AABB FromLeftTopWidthHeight(float left, float top, float width, float height)
    {
        var halfWith = width * 0.5f;
        var halfHeight = height * 0.5f;
        return new AABB
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