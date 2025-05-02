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
    public static bool IsPointOverBezier(Vector2 mousePos, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float threshold = 1.5f)
    {
        float distance = DistanceToCubicBezier(mousePos, p0, p1, p2, p3);
        return distance <= threshold;
    }
}