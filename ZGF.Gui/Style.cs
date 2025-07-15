namespace ZGF.Gui;

public sealed class Style
{
    public StyleValue<uint> BackgroundColor { get; set; }
    public PaddingStyle Padding { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
    public BorderColorStyle BorderColor { get; set; }
}