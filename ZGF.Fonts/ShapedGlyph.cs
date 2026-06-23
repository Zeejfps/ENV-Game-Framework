namespace ZGF.Fonts;

public readonly record struct ShapedGlyph
{
    public readonly uint GlyphIndex;
    public readonly float XOffset;
    public readonly float YOffset;
    public readonly float XAdvance;
    public readonly float YAdvance;
    public readonly int Cluster;

    // Source font id: fallback can split one line across fonts, so this is per-glyph.
    public readonly int FontId;

    public ShapedGlyph(uint glyphIndex, float xOffset, float yOffset,
        float xAdvance, float yAdvance, int cluster, int fontId)
    {
        GlyphIndex = glyphIndex;
        XOffset = xOffset;
        YOffset = yOffset;
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        Cluster = cluster;
        FontId = fontId;
    }
}
