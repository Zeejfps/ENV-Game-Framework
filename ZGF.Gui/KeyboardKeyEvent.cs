using ZGF.KeyboardModule;

namespace ZGF.Gui;

public readonly struct KeyboardKeyEvent
{
    public required KeyboardKey Key { get; init; }
    public required InputState State { get; init; }
    public required InputModifiers Modifiers { get; init; }
}