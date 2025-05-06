using System.Numerics;

namespace NodeGraphApp;

public static class BezierUtils
{
    // Evaluate a point on a cubic Bezier curve at time t
    public static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }

    // Distance from point to line segment
    public static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = point - a;

        float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
        t = Math.Clamp(t, 0, 1);

        Vector2 closest = a + t * ab;
        return Vector2.Distance(point, closest);
    }

    // Distance from a point to a cubic bezier curve
    public static float DistanceToCubicBezier(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps = 40)
    {
        float minDistance = float.MaxValue;

        Vector2 prev = CubicBezier(p0, p1, p2, p3, 0f);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 curr = CubicBezier(p0, p1, p2, p3, t);
            float dist = DistancePointToSegment(point, prev, curr);
            if (dist < minDistance)
                minDistance = dist;
            prev = curr;
        }

        return minDistance;
    }

    // Helper for hit test
    public static bool IsPointOverBezier(Vector2 mousePos, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float threshold = 2f)
    {
        float distance = DistanceToCubicBezier(mousePos, p0, p1, p2, p3);
        return distance <= threshold;
    }
    
    public static bool RectangleOverlapsBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, ScreenRect rect, int steps = 50)
    {
        var prev = CubicBezier(p0, p1, p2, p3, 0f);
        if (rect.Contains(prev)) return true;

        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var curr = CubicBezier(p0, p1, p2, p3, t);
            if (rect.Contains(curr)) return true;
            if (LineIntersectsRect(prev, curr, rect)) return true;

            prev = curr;
        }

        return false;
    }
    
    public static bool LineIntersectsRect(Vector2 a, Vector2 b, ScreenRect rect)
    {
        var rectLines = new[]
        {
            (new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top)),
            (new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom)),
            (new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom)),
            (new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Left, rect.Top))
        };

        foreach (var (p1, p2) in rectLines)
        {
            if (LinesIntersect(a, b, p1, p2))
                return true;
        }

        return false;
    }
    
    public static bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        var d = (a2.X - a1.X) * (b2.Y - b1.Y) - (a2.Y - a1.Y) * (b2.X - b1.X);
        if (d == 0) return false;

        var u = ((b1.X - a1.X) * (b2.Y - b1.Y) - (b1.Y - a1.Y) * (b2.X - b1.X)) / d;
        var v = ((b1.X - a1.X) * (a2.Y - a1.Y) - (b1.Y - a1.Y) * (a2.X - a1.X)) / d;

        return (u >= 0 && u <= 1) && (v >= 0 && v <= 1);
    }
}