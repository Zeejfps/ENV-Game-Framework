using System.Numerics;

namespace ZGF.Geometry;

public readonly record struct PointF(float X, float Y)
{
    public float DistanceSqTo(PointF p2)
    {
        var dx = X - p2.X;
        var dy = Y - p2.Y;
        return dx * dx + dy * dy;
    }

    public static Vector2 operator -(PointF p1, PointF p2)
    {
        return new Vector2(p1.X - p2.X, p1.Y - p2.Y);
    }
}