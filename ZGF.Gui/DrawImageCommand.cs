using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawImageCommand
{
    public required RectF Position { get; init; }
    public required int ZIndex { get; init; }
    public string ImageId { get; init; }
}