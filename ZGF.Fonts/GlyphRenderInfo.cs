namespace ZGF.Fonts;

public readonly struct GlyphRenderInfo
{
    public readonly int BitmapLeft;
    public readonly int BitmapTop;
    public readonly int Width;
    public readonly int Height;
    public readonly float XAdvance;

    public readonly int AtlasX;
    public readonly int AtlasY;

    public GlyphRenderInfo(int bitmapLeft, int bitmapTop, int width, int height,
        float xAdvance, int atlasX, int atlasY)
    {
        BitmapLeft = bitmapLeft;
        BitmapTop = bitmapTop;
        Width = width;
        Height = height;
        XAdvance = xAdvance;
        AtlasX = atlasX;
        AtlasY = atlasY;
    }
}
