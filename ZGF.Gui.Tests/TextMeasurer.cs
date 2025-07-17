namespace ZGF.Gui.Tests;

public sealed class TextMeasurer : ITextMeasurer
{
    public float MeasureTextWidth(string text, TextStyle style)
    {
        return text.Length * 7.5f;
    }
}