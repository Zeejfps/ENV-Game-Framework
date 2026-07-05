using System.Numerics;
using ZGF.Svg.Scene;

namespace ZGF.Svg.Parsing;

/// <summary>
/// Converts SVG elliptical-arc segments to cubic bezier runs, per SVG 1.1
/// spec appendix F.6 (endpoint → center parameterization, F.6.5-F.6.6).
/// </summary>
internal static class ArcConverter
{
    public static void ArcToCubics(
        List<PathSegment> output,
        Vector2 from,
        float rx, float ry,
        float xAxisRotationDegrees,
        bool largeArc, bool sweep,
        Vector2 to)
    {
        if (from == to)
            return;

        rx = MathF.Abs(rx);
        ry = MathF.Abs(ry);
        if (rx == 0f || ry == 0f)
        {
            output.Add(PathSegment.LineTo(to));
            return;
        }

        var phi = xAxisRotationDegrees * (MathF.PI / 180f);
        var cosPhi = MathF.Cos(phi);
        var sinPhi = MathF.Sin(phi);

        // F.6.5.1: midpoint in the rotated frame.
        var dx2 = (from.X - to.X) * 0.5f;
        var dy2 = (from.Y - to.Y) * 0.5f;
        var x1p = cosPhi * dx2 + sinPhi * dy2;
        var y1p = -sinPhi * dx2 + cosPhi * dy2;

        // F.6.6.2: scale radii up if the endpoints cannot be spanned.
        var lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1f)
        {
            var s = MathF.Sqrt(lambda);
            rx *= s;
            ry *= s;
        }

        // F.6.5.2: center in the rotated frame.
        var rxSq = rx * rx;
        var rySq = ry * ry;
        var num = rxSq * rySq - rxSq * y1p * y1p - rySq * x1p * x1p;
        var den = rxSq * y1p * y1p + rySq * x1p * x1p;
        var radicand = MathF.Max(0f, num / den);
        var coef = MathF.Sqrt(radicand);
        if (largeArc == sweep)
            coef = -coef;
        var cxp = coef * (rx * y1p / ry);
        var cyp = coef * (-ry * x1p / rx);

        // F.6.5.3: center in the original frame.
        var cx = cosPhi * cxp - sinPhi * cyp + (from.X + to.X) * 0.5f;
        var cy = sinPhi * cxp + cosPhi * cyp + (from.Y + to.Y) * 0.5f;

        // F.6.5.5-6: start angle and sweep extent.
        var theta1 = Angle(1f, 0f, (x1p - cxp) / rx, (y1p - cyp) / ry);
        var deltaTheta = Angle(
            (x1p - cxp) / rx, (y1p - cyp) / ry,
            (-x1p - cxp) / rx, (-y1p - cyp) / ry);
        if (!sweep && deltaTheta > 0f)
            deltaTheta -= 2f * MathF.PI;
        else if (sweep && deltaTheta < 0f)
            deltaTheta += 2f * MathF.PI;

        // Slice into arcs of at most 90° and emit one cubic per slice.
        var sliceCount = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(deltaTheta) / (MathF.PI * 0.5f)));
        var sliceAngle = deltaTheta / sliceCount;
        var k = 4f / 3f * MathF.Tan(sliceAngle * 0.25f);

        var theta = theta1;
        var p0 = from;
        for (var i = 0; i < sliceCount; i++)
        {
            var thetaEnd = theta + sliceAngle;
            var p3 = i == sliceCount - 1 ? to : PointAt(cx, cy, rx, ry, cosPhi, sinPhi, thetaEnd);
            var d0 = DerivativeAt(rx, ry, cosPhi, sinPhi, theta);
            var d3 = DerivativeAt(rx, ry, cosPhi, sinPhi, thetaEnd);
            var c1 = p0 + k * d0;
            var c2 = p3 - k * d3;
            output.Add(PathSegment.CubicTo(c1, c2, p3));
            theta = thetaEnd;
            p0 = p3;
        }
    }

    private static Vector2 PointAt(float cx, float cy, float rx, float ry, float cosPhi, float sinPhi, float theta)
    {
        var x = rx * MathF.Cos(theta);
        var y = ry * MathF.Sin(theta);
        return new Vector2(
            cx + cosPhi * x - sinPhi * y,
            cy + sinPhi * x + cosPhi * y);
    }

    private static Vector2 DerivativeAt(float rx, float ry, float cosPhi, float sinPhi, float theta)
    {
        var x = -rx * MathF.Sin(theta);
        var y = ry * MathF.Cos(theta);
        return new Vector2(
            cosPhi * x - sinPhi * y,
            sinPhi * x + cosPhi * y);
    }

    /// <summary>Signed angle from vector (ux,uy) to vector (vx,vy), per F.6.5.4.</summary>
    private static float Angle(float ux, float uy, float vx, float vy)
    {
        var dot = ux * vx + uy * vy;
        var len = MathF.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        var cos = Math.Clamp(dot / len, -1f, 1f);
        var angle = MathF.Acos(cos);
        if (ux * vy - uy * vx < 0f)
            angle = -angle;
        return angle;
    }
}
