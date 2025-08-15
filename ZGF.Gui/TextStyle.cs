namespace ZGF.Gui;

public sealed class TextStyle
{
    public StyleValue<uint> TextColor;
    public StyleValue<string> FontFamily; 
    public StyleValue<TextAlignment> HorizontalAlignment;
    public StyleValue<TextAlignment> VerticalAlignment;
    public StyleValue<bool> IsMultiLine;
    public StyleValue<TextWrap> TextWrap = new(Gui.TextWrap.NoWrap, false);
}