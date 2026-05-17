namespace ZGF.Gui;

public sealed class ImageStyle
{
    public StyleValue<uint> TintColor = new(0xFFFFFFFF, false);
    public StyleValue<float> Rotation = new(0f, false);

    public void Apply(Style style)
    {
        if (style.TintColor.IsSet)
            TintColor = style.TintColor.Value;

        if (style.Rotation.IsSet)
            Rotation = style.Rotation.Value;
    }
}