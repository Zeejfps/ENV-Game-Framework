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
}
