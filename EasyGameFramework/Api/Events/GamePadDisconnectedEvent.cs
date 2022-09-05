using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct GamePadDisconnectedEvent
{
    public IGenericGamepad Gamepad { get; init; }
}