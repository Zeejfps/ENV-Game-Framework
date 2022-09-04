using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct KeyboardKeyPressedEvent
{
    public IKeyboard Keyboard { get; init; }
    public KeyboardKey Key { get; init; }
}