namespace ZGF.Fonts;

public readonly struct ShapedGlyph
{
    public readonly uint GlyphIndex;
    public readonly float XOffset;
    public readonly float YOffset;
    public readonly float XAdvance;
    public readonly float YAdvance;
    public readonly int Cluster;

    public ShapedGlyph(uint glyphIndex, float xOffset, float yOffset,
        float xAdvance, float yAdvance, int cluster)
    {
        GlyphIndex = glyphIndex;
        XOffset = xOffset;
        YOffset = yOffset;
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        Cluster = cluster;
    }
}
