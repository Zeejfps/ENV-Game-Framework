using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class RectStyle
{
    public uint BackgroundColor;
    public PaddingStyle Padding;
    public BorderColorStyle BorderColor;
    public BorderSizeStyle BorderSize;
}

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

public readonly struct BorderColorStyle
{
    public uint Left { get; init; }
    public uint Right { get; init; }
    public uint Top { get; init; }
    public uint Bottom { get; init; }

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

public readonly struct BorderSizeStyle
{
    public float Left { get; init; }
    public float Right { get; init; }
    public float Top { get; init; }
    public float Bottom { get; init; }

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

public readonly struct DrawRectCommand
{
    public required RectF Position { get; init; }
    public required RectStyle Style {get; init; }
}

public readonly struct DrawTextCommand
{
    public required RectF Position { get; init; }
    public required string Text { get; init; }
    public required TextStyle Style {get; init; }
}