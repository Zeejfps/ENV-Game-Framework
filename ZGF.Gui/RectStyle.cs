using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class RectStyle
{
    public uint BackgroundColor { get; set; }
    
    public BorderColorStyle BorderColor { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
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