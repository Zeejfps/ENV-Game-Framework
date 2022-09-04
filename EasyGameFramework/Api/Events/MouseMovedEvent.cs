using System.Numerics;

namespace EasyGameFramework.Api.Events;

public readonly struct MouseMovedEvent
{
    public Vector2 Delta { get; init; }
}