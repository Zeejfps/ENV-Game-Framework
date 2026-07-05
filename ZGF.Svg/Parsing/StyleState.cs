using System.Numerics;

namespace ZGF.Svg.Parsing;

/// <summary>
/// Inherited presentation state, resolved while walking the element tree.
/// Copied on push; mutated by attribute/style parsing for the current element.
/// </summary>
internal struct StyleState
{
    public SvgPaintKind FillKind;
    public uint FillArgb;            // as authored, alpha included
    public float FillOpacity;
    public SvgPaintKind StrokeKind;
    public uint StrokeArgb;
    public float StrokeOpacity;
    public SvgFillRule FillRule;
    public float StrokeWidth;
    public SvgLineCap Cap;
    public SvgLineJoin Join;
    public float MiterLimit;
    public int DashStart;
    public int DashCount;
    public float DashOffset;
    public Matrix3x2 Transform;
    /// <summary>Folded product of ancestor + own `opacity` (approximation: applied per-paint, not per-layer).</summary>
    public float OpacityProduct;

    public static StyleState CreateInitial() => new()
    {
        FillKind = SvgPaintKind.Color,
        FillArgb = 0xFF000000,
        FillOpacity = 1f,
        StrokeKind = SvgPaintKind.None,
        StrokeArgb = 0,
        StrokeOpacity = 1f,
        FillRule = SvgFillRule.NonZero,
        StrokeWidth = 1f,
        Cap = SvgLineCap.Butt,
        Join = SvgLineJoin.Miter,
        MiterLimit = 4f,
        DashStart = 0,
        DashCount = 0,
        DashOffset = 0f,
        Transform = Matrix3x2.Identity,
        OpacityProduct = 1f,
    };

    public SvgPaint ResolveFill() => ResolvePaint(FillKind, FillArgb, FillOpacity);
    public SvgPaint ResolveStroke() => ResolvePaint(StrokeKind, StrokeArgb, StrokeOpacity);

    private SvgPaint ResolvePaint(SvgPaintKind kind, uint argb, float paintOpacity)
    {
        if (kind == SvgPaintKind.None)
            return SvgPaint.None;

        var authoredAlpha = kind == SvgPaintKind.CurrentColor ? 255u : argb >> 24;
        var alpha = (uint)Math.Clamp(
            (int)MathF.Round(authoredAlpha * Math.Clamp(paintOpacity, 0f, 1f) * Math.Clamp(OpacityProduct, 0f, 1f)),
            0, 255);
        return new SvgPaint(kind, (alpha << 24) | (argb & 0x00FFFFFF));
    }
}
