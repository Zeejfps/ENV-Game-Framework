namespace ZGF.Gui;

public readonly struct TextStyle
{
    public StyleValue<TextAlignment> HorizontalAlignment { get; init; }
    public StyleValue<TextAlignment> VerticalAlignment { get; init; }
}