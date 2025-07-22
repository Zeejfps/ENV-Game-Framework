namespace ZGF.Gui;

public interface ITextMeasurer
{
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);
    float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style);
}