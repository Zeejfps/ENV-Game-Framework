using System.Numerics;

namespace Bricks.ECS;

public readonly record struct Transform
{
    public required Vector2 Position { get; init; }
}