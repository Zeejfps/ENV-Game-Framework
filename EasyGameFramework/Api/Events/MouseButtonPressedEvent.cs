using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct MouseButtonPressedEvent
{
    public MouseButton Button { get; init; }
}