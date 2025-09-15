namespace Bricks.ECS.Components;

public readonly record struct CircleCollider
{
    public required float Radius { get; init; }
}