using ZGF.KeyboardModule;

namespace ZGF.Gui;

public struct KeyboardKeyEvent : IEvent
{
    public required KeyboardKey Key { get; init; }
    public required InputState State { get; init; }
    public required InputModifiers Modifiers { get; init; }
    public required EventPhase Phase { get; set; }
    public bool IsConsumed { get; private set; }
    public void Consume()
    {
        IsConsumed = true;
    }
}