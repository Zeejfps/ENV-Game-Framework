namespace ZGF.Gui.Tests;

public sealed class TextMeasurer : ITextMeasurer
{
    private readonly BitmapFont _bitmapFont;

    public TextMeasurer(BitmapFont bitmapFont)
    {
        _bitmapFont = bitmapFont;
    }

    public float MeasureTextWidth(string text, TextStyle style)
    {
        var totalWidth = 0f;
        foreach (var codePoint in text.AsCodePoints())
        {
            if (!_bitmapFont.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            
            totalWidth += glyphInfo.XAdvance;
        }
        return totalWidth;
    }
}