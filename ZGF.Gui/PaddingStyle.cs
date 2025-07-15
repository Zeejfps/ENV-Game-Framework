namespace ZGF.Gui;

public struct PaddingStyle
{
    public StyleValue<int> Left { get; set; }
    public StyleValue<int> Right { get; set; }
    public StyleValue<int> Top { get; set; }
    public StyleValue<int> Bottom { get; set; }

    public static PaddingStyle All(int size)
    {
        return new PaddingStyle
        {
            Left = size,
            Right = size,
            Top = size,
            Bottom = size,
        };
    }

    public void Apply(ref PaddingStyle padding)
    {
        if (Left.IsSet)
            padding.Left = Left.Value;
        
        if (Right.IsSet)
            padding.Right = Right.Value;
        
        if (Top.IsSet)
            padding.Top = Top.Value;
        
        if (Bottom.IsSet)
            padding.Bottom = Bottom.Value;
    }
}