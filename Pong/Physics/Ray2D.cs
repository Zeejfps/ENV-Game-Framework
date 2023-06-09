using System.Numerics;

namespace Pong.Physics;

public readonly struct Ray2D
{
    public Vector2 Origin { get; init; }
    public Vector2 Direction { get; init; }
}