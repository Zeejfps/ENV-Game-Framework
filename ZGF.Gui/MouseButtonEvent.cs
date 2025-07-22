using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct MouseButtonEvent
{
    public required PointF MousePoint { get; init; }
    public required MouseButton Button { get; init; }
    public required InputState State { get; init; }
}