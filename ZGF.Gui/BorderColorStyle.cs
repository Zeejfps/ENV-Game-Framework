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

    public void ApplyTo(ref BorderColorStyle style)
    {
        if (Left.IsSet)
            style.Left = Left.Value;
        
        if (Right.IsSet)
            style.Right = Right.Value;
        
        if (Top.IsSet)
            style.Top = Top.Value;
        
        if (Bottom.IsSet)
            style.Bottom = Bottom.Value;
    }
}