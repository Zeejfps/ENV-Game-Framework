namespace ZGF.Gui;

public struct MouseMoveEvent : IEvent
{
    public required IMouse Mouse { get; init; }
    public required EventPhase Phase { get; set; }
    public bool IsConsumed { get; private set; }
    public void Consume()
    {
        IsConsumed = true;
    }
}