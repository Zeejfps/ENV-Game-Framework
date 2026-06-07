using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawBezierInputs
{
    public required PointF Start { get; init; }
    public required PointF Control { get; init; }
    public required PointF End { get; init; }
    public required float Thickness { get; init; }
    public required uint Color { get; init; }
    public required int ZIndex { get; init; }
}
