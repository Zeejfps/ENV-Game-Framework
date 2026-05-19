namespace ZGF.Gui;

public struct BoxShadowStyle
{
    public StyleValue<float> OffsetX { get; set; }
    public StyleValue<float> OffsetY { get; set; }
    public StyleValue<float> Blur { get; set; }
    public StyleValue<float> Spread { get; set; }
    public StyleValue<uint> Color { get; set; }

    public bool IsActive => Color.IsSet && (Color.Value >> 24) != 0u;

    public void ApplyTo(ref BoxShadowStyle style)
    {
        if (OffsetX.IsSet) style.OffsetX = OffsetX.Value;
        if (OffsetY.IsSet) style.OffsetY = OffsetY.Value;
        if (Blur.IsSet)    style.Blur    = Blur.Value;
        if (Spread.IsSet)  style.Spread  = Spread.Value;
        if (Color.IsSet)   style.Color   = Color.Value;
    }
}
