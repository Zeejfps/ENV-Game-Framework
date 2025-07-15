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

    public void ApplyTo(ref BorderSizeStyle style)
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