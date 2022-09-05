using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct GamepadConnectedEvent
{
    public IGamepad Gamepad { get; init; }
}