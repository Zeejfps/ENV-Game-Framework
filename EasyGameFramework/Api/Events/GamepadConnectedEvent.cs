using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct GamepadConnectedEvent
{
    public int Slot { get; init; }
    public IGamepad Gamepad { get; init; }
}