namespace ZGF.Gui;

public struct MouseExitEvent
{
    public required EventPhase Phase { get; set; }
    public required IMouse Mouse { get; init; }
    public bool IsConsumed { get; private set; }

    public void Consume()
    {
        IsConsumed = true;
    }
}