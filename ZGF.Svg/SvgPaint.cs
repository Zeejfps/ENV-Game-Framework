namespace ZGF.Svg;

public enum SvgFillRule : byte
{
    NonZero,
    EvenOdd,
}

public enum SvgLineCap : byte
{
    Butt,
    Round,
    Square,
}

public enum SvgLineJoin : byte
{
    Miter,
    Round,
    Bevel,
}

public enum SvgPaintKind : byte
{
    None,
    Color,
    CurrentColor,
}

/// <summary>
/// A resolved paint. For <see cref="SvgPaintKind.Color"/> the alpha byte of
/// <see cref="ColorArgb"/> already includes fill/stroke-opacity and inherited group
/// opacity. For <see cref="SvgPaintKind.CurrentColor"/> only the alpha byte is
/// meaningful — it is the folded opacity to multiply onto the caller-supplied color.
/// </summary>
internal readonly record struct SvgPaint(SvgPaintKind Kind, uint ColorArgb)
{
    public static readonly SvgPaint None = new(SvgPaintKind.None, 0);

    public uint Resolve(uint currentColorArgb)
    {
        switch (Kind)
        {
            case SvgPaintKind.Color:
                return ColorArgb;
            case SvgPaintKind.CurrentColor:
            {
                var ownAlpha = ColorArgb >> 24;
                var currentAlpha = currentColorArgb >> 24;
                var alpha = (ownAlpha * currentAlpha + 127) / 255;
                return (alpha << 24) | (currentColorArgb & 0x00FFFFFF);
            }
            default:
                return 0;
        }
    }
}
