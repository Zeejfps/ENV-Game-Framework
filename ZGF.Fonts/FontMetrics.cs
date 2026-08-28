namespace ZGF.Fonts;

public readonly struct FontMetrics
{
    public readonly float Ascender;
    public readonly float Descender;
    public readonly float LineHeight;

    /// <summary>
    /// Where the underline sits, as an offset from the baseline in the same units as
    /// <see cref="Ascender"/>. Negative, because an underline is below the baseline.
    /// </summary>
    public readonly float UnderlinePosition;

    /// <summary>How thick the underline is drawn. At least one pixel.</summary>
    public readonly float UnderlineThickness;

    public FontMetrics(float ascender, float descender, float lineHeight)
        : this(ascender, descender, lineHeight, descender * 0.5f, 1f)
    {
    }

    public FontMetrics(float ascender, float descender, float lineHeight,
        float underlinePosition, float underlineThickness)
    {
        Ascender = ascender;
        Descender = descender;
        LineHeight = lineHeight;
        UnderlinePosition = underlinePosition;
        UnderlineThickness = underlineThickness;
    }
}
