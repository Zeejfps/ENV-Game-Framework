using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawImageCommand
{
    public required RectF Position { get; init; }
    public required int ZIndex { get; init; }
    public ImageInfo ImageInfo { get; init; }
}