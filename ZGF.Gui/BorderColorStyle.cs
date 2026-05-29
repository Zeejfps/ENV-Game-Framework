namespace ZGF.Gui;

public struct BorderColorStyle
{
    public StyleValue<uint> Left { get; set; }
    public StyleValue<uint> Right { get; set; }
    public StyleValue<uint> Top { get; set; }
    public StyleValue<uint> Bottom { get; set; }

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