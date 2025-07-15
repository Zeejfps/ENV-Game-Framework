namespace ZGF.Gui;

public readonly struct BorderSizeStyle
{
    public StyleValue<float> Left { get; init; }
    public StyleValue<float> Right { get; init; }
    public StyleValue<float> Top { get; init; }
    public StyleValue<float> Bottom { get; init; }

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