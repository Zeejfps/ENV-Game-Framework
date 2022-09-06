using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct GamePadDisconnectedEvent
{
    public IGamepad Gamepad { get; init; }
}