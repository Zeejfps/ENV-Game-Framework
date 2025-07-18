namespace ZGF.Gui;

public sealed class RectStyle
{
    public StyleValue<uint> BackgroundColor;
    public PaddingStyle Padding;
    public BorderColorStyle BorderColor;
    public BorderSizeStyle BorderSize;

    public void Apply(Style style)
    {
        if (style.BackgroundColor.IsSet)
            BackgroundColor = style.BackgroundColor.Value;
            
        style.Padding.ApplyTo(ref Padding);
        style.BorderSize.ApplyTo(ref BorderSize);
        style.BorderColor.ApplyTo(ref BorderColor);
    }
}