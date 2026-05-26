namespace ZGF.Gui;

public sealed class Style
{
    // --- Visual ---
    public StyleValue<uint> TintColor { get; set; }
    public StyleValue<float> Rotation { get; set; }
    public StyleValue<uint> BackgroundColor { get; set; }
    public PaddingStyle Padding { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
    public BorderColorStyle BorderColor { get; set; }
    public BorderRadiusStyle BorderRadius { get; set; }
    public BoxShadowStyle BoxShadow { get; set; }

    // --- Text ---
    public StyleValue<uint> TextColor { get; set; }
    public StyleValue<string> FontFamily { get; set; }
    public StyleValue<float> FontSize { get; set; }
    public StyleValue<FontWeight> FontWeight { get; set; }
    public StyleValue<TextAlignment> HorizontalAlignment { get; set; }
    public StyleValue<TextAlignment> VerticalAlignment { get; set; }
    public StyleValue<TextWrap> TextWrap { get; set; }

    // --- Layout ---
    public StyleValue<float> PreferredWidth { get; set; }
    public StyleValue<float> PreferredHeight { get; set; }
}
