using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawImageInputs
{
    public required RectF Position { get; init; }
    public required int ZIndex { get; init; }
    public required string ImageId { get; init; }
    public required uint TintColor { get; init; }
    public required float Rotation { get; init; }
}
