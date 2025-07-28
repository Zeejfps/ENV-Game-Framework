using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawRectInputs
{
    public required RectF Position { get; init; }
    public required RectStyle Style {get; init; }
    public required int ZIndex { get; init; }
}