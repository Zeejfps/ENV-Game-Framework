using System.Numerics;

namespace ZGF.Svg.Scene;

/// <summary>
/// One paint operation over a segment range, with all inheritance and transform
/// composition resolved at parse time. Coordinates stay in user space; the
/// viewBox→device transform is applied at raster time.
/// </summary>
internal readonly struct SvgDrawCommand
{
    public required int SegStart { get; init; }
    public required int SegCount { get; init; }
    public required Matrix3x2 Transform { get; init; }
    public required SvgPaint Fill { get; init; }
    public required SvgPaint Stroke { get; init; }
    public SvgFillRule FillRule { get; init; }
    public float StrokeWidth { get; init; }
    public float MiterLimit { get; init; }
    public SvgLineCap Cap { get; init; }
    public SvgLineJoin Join { get; init; }
    public int DashStart { get; init; }
    public int DashCount { get; init; }
    public float DashOffset { get; init; }
}
