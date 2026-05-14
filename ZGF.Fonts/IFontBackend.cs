namespace ZGF.Fonts;

public interface IFontBackend : IDisposable
{
    int AtlasWidth { get; }
    int AtlasHeight { get; }
    ReadOnlySpan<byte> AtlasPixels { get; }

    bool AtlasDirty { get; }
    AtlasDirtyRect DirtyRect { get; }
    void ClearDirty();

    FontHandle LoadFontFromFile(string path, int pixelSize);
    FontMetrics GetMetrics(FontHandle font);

    uint GetGlyphIndex(FontHandle font, int codePoint);

    bool TryGetGlyph(FontHandle font, uint glyphIndex, out GlyphRenderInfo info);

    float GetKerning(FontHandle font, uint prevGlyphIndex, uint glyphIndex);

    int ShapeText(FontHandle font, ReadOnlySpan<char> text, Span<ShapedGlyph> output);
}

public readonly struct AtlasDirtyRect
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public AtlasDirtyRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool IsEmpty => Width <= 0 || Height <= 0;
}
