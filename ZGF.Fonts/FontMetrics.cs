namespace ZGF.Fonts;

public readonly struct FontMetrics
{
    public readonly float Ascender;
    public readonly float Descender;
    public readonly float LineHeight;

    public FontMetrics(float ascender, float descender, float lineHeight)
    {
        Ascender = ascender;
        Descender = descender;
        LineHeight = lineHeight;
    }
}
