using System.Numerics;

namespace Bricks.ECS;

public readonly record struct Rigidbody
{
    public required Vector2 Position { get; init; }
    public required Vector2 Velocity { get; init; }

}