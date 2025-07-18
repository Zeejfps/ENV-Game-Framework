using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawTextCommand
{
    public required RectF Position { get; init; }
    public required string Text { get; init; }
    public required TextStyle Style {get; init; }
    public required int ZIndex { get; init; }
}