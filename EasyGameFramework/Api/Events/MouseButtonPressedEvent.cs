using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct MouseButtonPressedEvent
{
    public IMouse Mouse { get; init; }
    public MouseButton Button { get; init; }
}