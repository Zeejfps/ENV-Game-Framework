namespace ZGF.Gui;

public sealed class Context
{
    public required MouseInputSystem MouseInputSystem { get; init; }
    public required ITextMeasurer TextMeasurer { get; init; }
    public required Component ContextMenuPane { get; init; }
    
    public T Get<T>()
    {
        return default;
    }
}

public interface ITextMeasurer
{
    float MeasureTextWidth(string text, TextStyle style);
    float MeasureTextHeight(string text, TextStyle style);
}