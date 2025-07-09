namespace ZnvQuadTree;

public readonly record struct RectF(float Left, float Bottom, float Width, float Height)
{
    public float Top => Bottom + Height;
    public float Right => Left + Width;
    
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
    
    public float DistanceSqTo(PointF point)
    {
        var closestX = Math.Max(this.Left, Math.Min(point.X, this.Right));
        var closestY = Math.Max(this.Bottom, Math.Min(point.Y, this.Top));

        var dx = point.X - closestX;
        var dy = point.Y - closestY;
        
        return (dx * dx) + (dy * dy);
    }
}