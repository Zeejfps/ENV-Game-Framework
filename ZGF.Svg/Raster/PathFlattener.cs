using System.Numerics;
using ZGF.Svg.Scene;

namespace ZGF.Svg.Raster;

/// <summary>
/// Flattens normalized segments into device-space polylines. Control points are
/// transformed first (affine transforms commute with bezier construction), then
/// cubics are subdivided uniformly using Wang's formula so the maximum deviation
/// from the true curve stays under the device-space tolerance.
/// </summary>
internal static class PathFlattener
{
    /// <summary>
    /// Max chord deviation in device px for fill boundaries. Kept well under a quarter
    /// pixel because inscribed chords bias curves inward systematically — at 0.25 the
    /// area loss on small circles is visible (~1%); at 0.08 it stays under 8-bit AA noise.
    /// </summary>
    public const float ToleranceDevicePx = 0.08f;

    /// <summary>
    /// Stroke centerlines tolerate a coarser flatten: the deviation only shifts the
    /// stroke sideways by a fraction of its width, and halving segment count roughly
    /// halves stroking cost (each segment becomes a quad).
    /// </summary>
    public const float StrokeToleranceDevicePx = 0.2f;

    public static void Flatten(
        ReadOnlySpan<PathSegment> segments,
        in Matrix3x2 transform,
        PathBuffer output,
        float tolerance = ToleranceDevicePx)
    {
        var current = Vector2.Zero;
        var hasContour = false;

        foreach (var seg in segments)
        {
            switch (seg.Kind)
            {
                case SegKind.Move:
                {
                    output.BeginContour();
                    current = Vector2.Transform(seg.P3, transform);
                    output.Add(current);
                    hasContour = true;
                    break;
                }
                case SegKind.Line:
                {
                    if (!hasContour)
                        goto case SegKind.Move;
                    var p = Vector2.Transform(seg.P3, transform);
                    if (p != current)
                    {
                        output.Add(p);
                        current = p;
                    }
                    break;
                }
                case SegKind.Cubic:
                {
                    if (!hasContour)
                    {
                        output.BeginContour();
                        output.Add(current);
                        hasContour = true;
                    }
                    var c1 = Vector2.Transform(seg.P1, transform);
                    var c2 = Vector2.Transform(seg.P2, transform);
                    var p3 = Vector2.Transform(seg.P3, transform);
                    FlattenCubic(current, c1, c2, p3, output, tolerance);
                    current = p3;
                    break;
                }
                case SegKind.Close:
                {
                    output.CloseContour();
                    hasContour = false;
                    break;
                }
            }
        }

        output.EndPath();
    }

    private static void FlattenCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, PathBuffer output, float tolerance)
    {
        // Wang's formula: ||B''(t)|| <= 6 * max(|p0-2p1+p2|, |p1-2p2+p3|) = 6M, and the
        // chord error of n uniform pieces is bounded by ||B''||/(8n^2), so
        // n = ceil(sqrt(3M / (4*tolerance))) guarantees the tolerance.
        var m = MathF.Max(
            (p0 - 2f * p1 + p2).Length(),
            (p1 - 2f * p2 + p3).Length());
        var n = (int)MathF.Ceiling(MathF.Sqrt(3f * m / (4f * tolerance)));
        n = Math.Clamp(n, 1, 256);

        var step = 1f / n;
        for (var i = 1; i < n; i++)
        {
            var t = i * step;
            var u = 1f - t;
            var point =
                u * u * u * p0 +
                3f * u * u * t * p1 +
                3f * u * t * t * p2 +
                t * t * t * p3;
            output.Add(point);
        }
        output.Add(p3);
    }
}
