using System.ComponentModel;
using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace Pong.Physics;

public readonly struct RaycastResult2D
{
    public Vector2 Normal { get; init; }
    public Vector2 HitPoint { get; init; }
    public float T { get; init; }
}

public sealed class Physics2D
{
    public bool TryRaycastRect(Ray2D ray, Rect rect, out RaycastResult2D result)
    {
        var nearHitPoint = (rect.BottomLeft - ray.Origin) / ray.Direction;
        var farHitPoint = (rect.TopRight - ray.Origin) / ray.Direction;

        if (nearHitPoint.X > farHitPoint.X)
        {
            var temp = farHitPoint.X;
            farHitPoint.X = nearHitPoint.X;
            nearHitPoint.X = temp;
        }
        
        if (nearHitPoint.Y > farHitPoint.Y)
        {
            var temp = farHitPoint.Y;
            farHitPoint.Y = nearHitPoint.Y;
            nearHitPoint.Y = temp;
        }

        result = default;
        if (nearHitPoint.X > farHitPoint.Y || nearHitPoint.Y > farHitPoint.X) 
            return false;

        var t = MathF.Max(nearHitPoint.X, nearHitPoint.Y);
        var farT = MathF.Min(farHitPoint.X, farHitPoint.Y);

        if (farT < 0) 
            return false;

        var hitPoint = ray.Origin + ray.Direction * t;
        var normal = Vector2.Zero;
        if (nearHitPoint.X > nearHitPoint.Y)
        {
            if (ray.Direction.X < 0)
                normal = new Vector2(1, 0);
            else
                normal = new Vector2(-1, 0);
        }
        else if (nearHitPoint.X < nearHitPoint.Y)
        {
            if (ray.Direction.Y < 0)
                normal = new Vector2(0, 1);
            else
                normal = new Vector2(0, -1);
        }

        result = new RaycastResult2D
        {
            T = t,
            Normal = normal,
            HitPoint = hitPoint
        };
        return true;
    }
}