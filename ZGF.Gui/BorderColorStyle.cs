namespace ZGF.Gui;

public readonly struct BorderColorStyle
{
    public StyleValue<uint> Left { get; init; }
    public StyleValue<uint> Right { get; init; }
    public StyleValue<uint> Top { get; init; }
    public StyleValue<uint> Bottom { get; init; }

    public static BorderColorStyle All(uint color)
    {
        return new BorderColorStyle
        {
            Left = color,
            Right = color,
            Top = color,
            Bottom = color,
        };
    }
}