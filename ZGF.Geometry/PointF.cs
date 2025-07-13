namespace ZGF.Geometry;

public readonly record struct PointF(float X, float Y)
{
    public float DistanceSqTo(PointF p2)
    {
        var dx = X - p2.X;
        var dy = Y - p2.Y;
        return dx * dx + dy * dy;
    }
}