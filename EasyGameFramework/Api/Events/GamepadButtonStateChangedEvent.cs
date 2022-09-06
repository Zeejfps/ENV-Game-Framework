using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct GamepadButtonStateChangedEvent
{
    public IGamepad Gamepad { get; init; }
    public GamepadButton Button { get; init; }
}