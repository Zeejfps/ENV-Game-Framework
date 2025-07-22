using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct MouseButtonEvent
{
    public required IMouse Mouse { get; init; }
    public required MouseButton Button { get; init; }
    public required InputState State { get; init; }
}