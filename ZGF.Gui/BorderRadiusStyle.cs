namespace ZGF.Gui;

public struct BorderRadiusStyle
{
    public StyleValue<float> TopLeft { get; set; }
    public StyleValue<float> TopRight { get; set; }
    public StyleValue<float> BottomRight { get; set; }
    public StyleValue<float> BottomLeft { get; set; }

    public static BorderRadiusStyle All(float radius)
    {
        return new BorderRadiusStyle
        {
            TopLeft = radius,
            TopRight = radius,
            BottomRight = radius,
            BottomLeft = radius,
        };
    }

    public void ApplyTo(ref BorderRadiusStyle style)
    {
        if (TopLeft.IsSet)
            style.TopLeft = TopLeft.Value;

        if (TopRight.IsSet)
            style.TopRight = TopRight.Value;

        if (BottomRight.IsSet)
            style.BottomRight = BottomRight.Value;

        if (BottomLeft.IsSet)
            style.BottomLeft = BottomLeft.Value;
    }
}
