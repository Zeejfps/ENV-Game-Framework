using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct MouseMoveEvent
{
    public required PointF MousePosition { get; init; }
}