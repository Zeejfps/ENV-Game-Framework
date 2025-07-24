namespace ZGF.Gui;

public struct MouseButtonEvent : IEvent
{
    public required IMouse Mouse { get; init; }
    public required MouseButton Button { get; init; }
    public required InputState State { get; init; }
    public required EventPhase Phase { get; set; }
    public bool IsConsumed { get; private set; }
    public void Consume()
    {
        IsConsumed = true;
    }
}