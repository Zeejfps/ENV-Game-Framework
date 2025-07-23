namespace ZGF.Gui;

public readonly struct MouseWheelScrolledEvent
{
    public required IMouse Mouse { get; init; }
    public required double DeltaX { get; init; }
    public required double DeltaY { get; init; }
}