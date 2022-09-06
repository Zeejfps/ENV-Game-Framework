using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct MouseButtonStateChangedEvent
{
    public IMouse Mouse { get; init; }
    public MouseButton Button { get; init; }
}