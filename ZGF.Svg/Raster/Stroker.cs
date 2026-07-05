using System.Numerics;

namespace ZGF.Svg.Raster;

/// <summary>
/// Builds stroke geometry as a union of per-segment quads, join wedges, and cap
/// pieces, all emitted with consistent winding and filled with the nonzero rule —
/// overlaps and self-intersections at tight joins resolve automatically.
/// </summary>
internal sealed class Stroker
{
    private Vector2[] _scratch = new Vector2[64];

    public void StrokeContour(
        ReadOnlySpan<Vector2> points,
        bool closed,
        float halfWidth,
        SvgLineCap cap,
        SvgLineJoin join,
        float miterLimit,
        CellRasterizer raster)
    {
        if (halfWidth <= 0f || points.Length == 0)
            return;

        if (closed && points.Length > 1 && points[0] == points[^1])
        {
            points = points[..^1];
            if (points.Length == 1)
                closed = false;
        }

        if (points.Length == 1 || (points.Length == 2 && points[0] == points[1]))
        {
            // A zero-length subpath paints only with round caps, per spec.
            if (cap == SvgLineCap.Round)
                AddCircle(points[0], halfWidth, raster);
            else if (cap == SvgLineCap.Square)
                AddSquareDot(points[0], halfWidth, raster);
            return;
        }

        var segmentEnd = closed ? points.Length : points.Length - 1;
        var quad = _scratch.AsSpan(0, 4);
        for (var i = 0; i < segmentEnd; i++)
        {
            var p0 = points[i];
            var p1 = points[(i + 1) % points.Length];
            if (p0 == p1)
                continue;

            var n = Normal(p1 - p0) * halfWidth;
            quad[0] = p0 + n;
            quad[1] = p1 + n;
            quad[2] = p1 - n;
            quad[3] = p0 - n;
            raster.AddPolygonConsistent(quad);
        }

        // Joins at interior vertices (every vertex when closed).
        var joinStart = closed ? 0 : 1;
        var joinEnd = closed ? points.Length : points.Length - 1;
        for (var i = joinStart; i < joinEnd; i++)
        {
            var prev = points[(i - 1 + points.Length) % points.Length];
            var v = points[i];
            var next = points[(i + 1) % points.Length];
            if (prev == v || v == next)
                continue;
            AddJoin(prev, v, next, halfWidth, join, miterLimit, raster);
        }

        if (!closed)
        {
            var first = points[0];
            var last = points[^1];
            AddCap(first, FirstDirection(points, forward: true), halfWidth, cap, raster);
            AddCap(last, FirstDirection(points, forward: false), halfWidth, cap, raster);
        }
    }

    /// <summary>Outward direction at an endpoint (pointing away from the contour).</summary>
    private static Vector2 FirstDirection(ReadOnlySpan<Vector2> points, bool forward)
    {
        if (forward)
        {
            for (var i = 1; i < points.Length; i++)
            {
                if (points[i] != points[0])
                    return Vector2.Normalize(points[0] - points[i]);
            }
        }
        else
        {
            for (var i = points.Length - 2; i >= 0; i--)
            {
                if (points[i] != points[^1])
                    return Vector2.Normalize(points[^1] - points[i]);
            }
        }
        return Vector2.UnitX;
    }

    private void AddJoin(
        Vector2 prev, Vector2 v, Vector2 next,
        float halfWidth, SvgLineJoin join, float miterLimit,
        CellRasterizer raster)
    {
        var d0 = Vector2.Normalize(v - prev);
        var d1 = Vector2.Normalize(next - v);
        var cross = d0.X * d1.Y - d0.Y * d1.X;
        var dot = Vector2.Dot(d0, d1);

        // The gap between adjacent segment quads is a wedge of area ~hw^2 * theta / 2.
        // Skip joins whose whole gap is below AA visibility.
        var theta = MathF.Atan2(MathF.Abs(cross), dot);
        if (halfWidth * halfWidth * theta * 0.5f < 0.02f)
            return;

        var n0 = Normal(d0);
        var n1 = Normal(d1);
        // The coverage gap is on the outer side of the turn.
        var side = cross > 0f ? 1f : -1f;
        var a0 = v + n0 * (halfWidth * side);
        var a1 = v + n1 * (halfWidth * side);

        // A round join deviates from the miter point by hw*(1/cos(theta/2) - 1): at the
        // shallow angles curve flattening produces, a 4-point miter wedge is
        // indistinguishable from the tessellated circle and far cheaper.
        var useMiter = join == SvgLineJoin.Miter;
        if (join == SvgLineJoin.Round)
        {
            var deviation = halfWidth * (1f / MathF.Cos(theta * 0.5f) - 1f);
            if (deviation < 0.05f)
                useMiter = true;
            else
            {
                AddCircle(v, halfWidth, raster);
                return;
            }
        }

        if (useMiter)
        {
            var bisector = n0 * side + n1 * side;
            var bisectorLength = bisector.Length();
            if (bisectorLength > 1e-6f)
            {
                var cosHalf = bisectorLength * 0.5f;  // |n0+n1|/2 = cos(alpha/2)
                var ratio = 1f / cosHalf;
                if (join == SvgLineJoin.Round || ratio <= miterLimit)
                {
                    var m = v + bisector / bisectorLength * (halfWidth * ratio);
                    var wedge = _scratch.AsSpan(0, 4);
                    wedge[0] = v;
                    wedge[1] = a0;
                    wedge[2] = m;
                    wedge[3] = a1;
                    raster.AddPolygonConsistent(wedge);
                    return;
                }
            }
            // Fall through to bevel (miter limit exceeded or 180° turn).
        }

        var tri = _scratch.AsSpan(0, 3);
        tri[0] = v;
        tri[1] = a0;
        tri[2] = a1;
        raster.AddPolygonConsistent(tri);
    }

    private void AddCap(Vector2 p, Vector2 outward, float halfWidth, SvgLineCap cap, CellRasterizer raster)
    {
        switch (cap)
        {
            case SvgLineCap.Round:
                AddCircle(p, halfWidth, raster);
                break;
            case SvgLineCap.Square:
            {
                var n = Normal(outward) * halfWidth;
                var ext = outward * halfWidth;
                var quad = _scratch.AsSpan(0, 4);
                quad[0] = p + n;
                quad[1] = p + n + ext;
                quad[2] = p - n + ext;
                quad[3] = p - n;
                raster.AddPolygonConsistent(quad);
                break;
            }
        }
    }

    private void AddSquareDot(Vector2 p, float halfWidth, CellRasterizer raster)
    {
        var quad = _scratch.AsSpan(0, 4);
        quad[0] = p + new Vector2(-halfWidth, -halfWidth);
        quad[1] = p + new Vector2(halfWidth, -halfWidth);
        quad[2] = p + new Vector2(halfWidth, halfWidth);
        quad[3] = p + new Vector2(-halfWidth, halfWidth);
        raster.AddPolygonConsistent(quad);
    }

    private void AddCircle(Vector2 center, float r, CellRasterizer raster)
    {
        if (r <= 0f)
            return;

        // Segment count from chordal error: e = r(1 - cos(pi/k)) <= tolerance.
        int k;
        if (r <= PathFlattener.ToleranceDevicePx)
        {
            k = 4;
        }
        else
        {
            var theta = MathF.Acos(1f - PathFlattener.ToleranceDevicePx / r);
            k = Math.Clamp((int)MathF.Ceiling(MathF.PI / theta), 4, 128);
        }

        if (_scratch.Length < k)
            _scratch = new Vector2[k * 2];
        var pts = _scratch.AsSpan(0, k);
        for (var i = 0; i < k; i++)
        {
            var angle = i * (2f * MathF.PI / k);
            pts[i] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
        }
        raster.AddPolygonConsistent(pts);
    }

    private static Vector2 Normal(Vector2 d1)
    {
        return Vector2.Normalize(new Vector2(d1.Y, -d1.X));
    }
}
