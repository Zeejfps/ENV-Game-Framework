namespace ZGF.Geometry;

public readonly record struct RectF
{
    public float Left { get; init; }
    public float Bottom { get; init; }
    public float Height { get; init; }
    public float Width { get; init; }
    public float Top => Bottom + Height;
    public float Right => Left + Width;
    public PointF BottomLeft => new(Left, Bottom);
    public PointF Center => new(Left + Width * 0.5f, Bottom + Height * 0.5f);
    public PointF TopRight => new(Right, Top);
    public float Area => Width * Height;

    public RectF(float left, float bottom, float width, float height)
    {
        Left = left;
        Bottom = bottom;
        Width = width;
        Height = height;
    }
    
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
               otherRect.Right < Right &&
               otherRect.Bottom >= Bottom &&
               otherRect.Top < Top;
    }
    
    public bool ContainsPoint(PointF point)
    {
        return point.X >= Left && point.X < Right &&
               point.Y >= Bottom && point.Y < Top;
    }
    
    public float DistanceSqTo(PointF point)
    {
        var closestX = Math.Max(this.Left, Math.Min(point.X, this.Right));
        var closestY = Math.Max(this.Bottom, Math.Min(point.Y, this.Top));

        var dx = point.X - closestX;
        var dy = point.Y - closestY;
        
        return (dx * dx) + (dy * dy);
    }

    public static RectF CreateMinimumBoundingRect(RectF r1, RectF r2)
    {
        var left = Math.Min(r1.Left, r2.Left);
        var bottom = Math.Min(r1.Bottom, r2.Bottom);
        var right = Math.Max(r1.Right, r2.Right);
        var top = Math.Max(r1.Top, r2.Top);
        return new RectF(left, bottom, right - top, top - bottom);
    }
}