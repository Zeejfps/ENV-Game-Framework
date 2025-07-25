using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawImageCommand
{
    public required RectF Position { get; init; }
    public required int ZIndex { get; init; }
    public required string ImageId { get; init; }
    public required ImageStyle Style { get; init; }
}