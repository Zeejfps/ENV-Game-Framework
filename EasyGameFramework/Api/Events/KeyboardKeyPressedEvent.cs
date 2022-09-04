using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct KeyboardKeyPressedEvent
{
    public KeyboardKey Key { get; init; }
}