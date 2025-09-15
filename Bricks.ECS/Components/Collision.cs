using System.Numerics;

namespace Bricks.ECS.Components;

public readonly record struct Collision
{
    public required Entity FirstEntity { get; init; }
    public required Entity SecondEntity { get; init; }
    public required Vector2 Normal { get; init; }
}