using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class RectStyle
{
    public StyleValue<uint> BackgroundColor;
    public PaddingStyle Padding;
    public BorderColorStyle BorderColor;
    public BorderSizeStyle BorderSize;
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