using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawBoxShadowInputs
{
    public required RectF Position { get; init; }
    public required BorderRadiusStyle BorderRadius { get; init; }
    public required BoxShadowStyle Shadow { get; init; }
    public required int ZIndex { get; init; }
}
