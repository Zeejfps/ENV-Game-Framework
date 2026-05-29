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
}