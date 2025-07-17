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
        return text.Length * 7.5f;
    }
}