namespace ZGF.Gui;

public sealed class TextStyle
{
    public StyleValue<uint> TextColor;
    public StyleValue<string> FontFamily; 
    public StyleValue<TextAlignment> HorizontalAlignment;
    public StyleValue<TextAlignment> VerticalAlignment;
    public StyleValue<bool> IsMultiLine;
}