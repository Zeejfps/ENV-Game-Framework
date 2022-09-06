using System.Numerics;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct MouseWheelScrolledEvent
{
    public IMouse Mouse { get; init; }
    public float DeltaX { get; init; }
    public float DeltaY { get; init; }
}