using System.Numerics;

namespace Bricks;

public readonly struct RaycastHit2D
{
    public Vector2 Normal { get; init; }
    public Vector2 Point { get; init; }
    public float Distance { get; init; }
}

public readonly struct Ray2D
{
    public Vector2 Origin { get; init; }
    public Vector2 Direction { get; init; }
}

public static class Physics2D
{
    public static bool TryCast(this AABB bounds, Vector2 direction, AABB targetBounds, out RaycastHit2D hit)
    {
        var expandedTarget = AABB.Expand(targetBounds, bounds.Width * 0.5f, bounds.Height * 0.5f);
        var ray = new Ray2D
        {
            Origin = bounds.Center,
            Direction = direction,
        };
        var didHit = TryRaycastRect(ray, expandedTarget, out hit);
        if (!didHit)
            return false;

        if (hit.Distance <= 0.0f)
            return false;

        return true;
    }
    
    public static bool TryRaycastRect(Ray2D ray, AABB aabb, out RaycastHit2D hit)
    {
        var rayDirection = Vector2.Normalize(ray.Direction);
        var maxLength = ray.Direction.Length();
        var nearHitPoint = (aabb.BottomLeft - ray.Origin) / rayDirection;
        var farHitPoint = (aabb.TopRight - ray.Origin) / rayDirection;

        if (nearHitPoint.X > farHitPoint.X)
        {
            (farHitPoint.X, nearHitPoint.X) = (nearHitPoint.X, farHitPoint.X);
        }
        
        if (nearHitPoint.Y > farHitPoint.Y)
        {
            (farHitPoint.Y, nearHitPoint.Y) = (nearHitPoint.Y, farHitPoint.Y);
        }

        hit = default;
        if (nearHitPoint.X > farHitPoint.Y || nearHitPoint.Y > farHitPoint.X) 
            return false;

        var t = MathF.Max(nearHitPoint.X, nearHitPoint.Y);
        var farT = MathF.Min(farHitPoint.X, farHitPoint.Y);

        if (farT < 0) 
            return false;

        if (t > maxLength)
            return false;

        var hitPoint = ray.Origin + rayDirection * t;
        var normal = Vector2.Zero;
        if (nearHitPoint.X > nearHitPoint.Y)
        {
            if (rayDirection.X < 0)
                normal = new Vector2(1, 0);
            else
                normal = new Vector2(-1, 0);
        }
        else if (nearHitPoint.X < nearHitPoint.Y)
        {
            if (rayDirection.Y < 0)
                normal = new Vector2(0, 1);
            else
                normal = new Vector2(0, -1);
        }

        hit = new RaycastHit2D
        {
            Distance = t,
            Normal = normal,
            Point = hitPoint
        };
        return true;
    }
}