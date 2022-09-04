using System.Numerics;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api.Events;

public readonly struct MouseMovedEvent
{
    public IMouse Mouse { get; init; }
    public Vector2 Delta { get; init; }
}