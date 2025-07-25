namespace ZGF.Gui;

public sealed class ImageStyle
{
    public StyleValue<uint> TintColor = new(0xFFFFFFFF, false);

    public void Apply(Style style)
    {
        if (style.TintColor.IsSet)
            TintColor = style.TintColor.Value;
    }
}