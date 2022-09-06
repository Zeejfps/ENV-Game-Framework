using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct GamepadDisconnectedEvent
{
    public IGamepad Gamepad { get; init; }
}