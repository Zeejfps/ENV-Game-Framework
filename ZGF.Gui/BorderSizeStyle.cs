namespace ZGF.Gui;

public struct BorderSizeStyle
{
    public StyleValue<float> Left { get; set; }
    public StyleValue<float> Right { get; set; }
    public StyleValue<float> Top { get; set; }
    public StyleValue<float> Bottom { get; set; }

    public static BorderSizeStyle All(float size)
    {
        return new BorderSizeStyle
        {
            Left = size,
            Right = size,
            Top = size,
            Bottom = size,
        };
    }
}