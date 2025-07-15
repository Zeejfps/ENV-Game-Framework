using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct RectStyle
{
    public uint BackgroundColor { get; init; }
    public BorderSizeStyle BorderSize { get; init; }
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