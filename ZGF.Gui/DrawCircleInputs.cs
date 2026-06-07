using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawCircleInputs
{
    public required PointF Center { get; init; }
    public required float Radius { get; init; }
    public required uint Color { get; init; }
    public required int ZIndex { get; init; }

    // 0 (default) draws a filled disc; a positive value draws a ring with this
    // stroke width centered on the radius.
    public float Thickness { get; init; }
}
