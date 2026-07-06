using System.Numerics;
using ZGF.Svg.Scene;

namespace ZGF.Svg.Parsing;

/// <summary>Lowers SVG basic shapes to the same Move/Line/Cubic/Close segment stream as paths.</summary>
internal static class ShapeLowering
{
    // Control-point distance for approximating a quarter circle with one cubic.
    private const float Kappa = 0.5522848f;

    public static void AddRect(List<PathSegment> output, float x, float y, float w, float h, float rx, float ry)
    {
        if (w <= 0f || h <= 0f)
            return;

        // Per spec: one radius auto-defaults to the other; both clamp to half the side.
        if (rx < 0f) rx = ry;
        if (ry < 0f) ry = rx;
        if (rx < 0f) { rx = 0f; ry = 0f; }
        rx = MathF.Min(rx, w * 0.5f);
        ry = MathF.Min(ry, h * 0.5f);

        if (rx == 0f || ry == 0f)
        {
            output.Add(PathSegment.MoveTo(new Vector2(x, y)));
            output.Add(PathSegment.LineTo(new Vector2(x + w, y)));
            output.Add(PathSegment.LineTo(new Vector2(x + w, y + h)));
            output.Add(PathSegment.LineTo(new Vector2(x, y + h)));
            output.Add(PathSegment.ClosePath());
            return;
        }

        var kx = rx * Kappa;
        var ky = ry * Kappa;
        output.Add(PathSegment.MoveTo(new Vector2(x + rx, y)));
        output.Add(PathSegment.LineTo(new Vector2(x + w - rx, y)));
        output.Add(PathSegment.CubicTo(
            new Vector2(x + w - rx + kx, y),
            new Vector2(x + w, y + ry - ky),
            new Vector2(x + w, y + ry)));
        output.Add(PathSegment.LineTo(new Vector2(x + w, y + h - ry)));
        output.Add(PathSegment.CubicTo(
            new Vector2(x + w, y + h - ry + ky),
            new Vector2(x + w - rx + kx, y + h),
            new Vector2(x + w - rx, y + h)));
        output.Add(PathSegment.LineTo(new Vector2(x + rx, y + h)));
        output.Add(PathSegment.CubicTo(
            new Vector2(x + rx - kx, y + h),
            new Vector2(x, y + h - ry + ky),
            new Vector2(x, y + h - ry)));
        output.Add(PathSegment.LineTo(new Vector2(x, y + ry)));
        output.Add(PathSegment.CubicTo(
            new Vector2(x, y + ry - ky),
            new Vector2(x + rx - kx, y),
            new Vector2(x + rx, y)));
        output.Add(PathSegment.ClosePath());
    }

    public static void AddEllipse(List<PathSegment> output, float cx, float cy, float rx, float ry)
    {
        if (rx <= 0f || ry <= 0f)
            return;

        var kx = rx * Kappa;
        var ky = ry * Kappa;
        output.Add(PathSegment.MoveTo(new Vector2(cx + rx, cy)));
        output.Add(PathSegment.CubicTo(
            new Vector2(cx + rx, cy + ky),
            new Vector2(cx + kx, cy + ry),
            new Vector2(cx, cy + ry)));
        output.Add(PathSegment.CubicTo(
            new Vector2(cx - kx, cy + ry),
            new Vector2(cx - rx, cy + ky),
            new Vector2(cx - rx, cy)));
        output.Add(PathSegment.CubicTo(
            new Vector2(cx - rx, cy - ky),
            new Vector2(cx - kx, cy - ry),
            new Vector2(cx, cy - ry)));
        output.Add(PathSegment.CubicTo(
            new Vector2(cx + kx, cy - ry),
            new Vector2(cx + rx, cy - ky),
            new Vector2(cx + rx, cy)));
        output.Add(PathSegment.ClosePath());
    }

    public static void AddLine(List<PathSegment> output, float x1, float y1, float x2, float y2)
    {
        output.Add(PathSegment.MoveTo(new Vector2(x1, y1)));
        output.Add(PathSegment.LineTo(new Vector2(x2, y2)));
    }

    public static void AddPoly(List<PathSegment> output, ReadOnlySpan<char> points, bool close)
    {
        var reader = new PathDataReader(points);
        if (!reader.TryReadNumber(out var x) || !reader.TryReadNumber(out var y))
            return;
        output.Add(PathSegment.MoveTo(new Vector2(x, y)));
        var any = false;
        while (reader.TryReadNumber(out x))
        {
            if (!reader.TryReadNumber(out y))
                break;
            output.Add(PathSegment.LineTo(new Vector2(x, y)));
            any = true;
        }
        if (close && any)
            output.Add(PathSegment.ClosePath());
    }
}
