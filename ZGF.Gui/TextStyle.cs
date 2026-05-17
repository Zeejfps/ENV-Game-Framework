namespace ZGF.Gui;

public sealed class TextStyle
{
    public StyleValue<uint> TextColor = new(0xFF000000, false);
    public StyleValue<string> FontFamily;
    public StyleValue<float> FontSize;
    public StyleValue<FontWeight> FontWeight;
    public StyleValue<TextAlignment> HorizontalAlignment;
    public StyleValue<TextAlignment> VerticalAlignment;
    public StyleValue<TextWrap> TextWrap;
}
