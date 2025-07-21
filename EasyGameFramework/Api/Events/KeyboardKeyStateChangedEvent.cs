using EasyGameFramework.Api.InputDevices;
using ZGF.KeyboardModule;

namespace EasyGameFramework.Api.Events;

public readonly struct KeyboardKeyStateChangedEvent
{
    public IKeyboard Keyboard { get; init; }
    public KeyboardKey Key { get; init; }
}