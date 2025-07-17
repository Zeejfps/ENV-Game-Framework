namespace ZGF.Gui;

public sealed class Context
{
    public required MouseInputSystem MouseInputSystem { get; init; }

    public float MeasureTextWidth(string text, TextStyle style)
    {
        return text.Length * 7.5f;
    }
}