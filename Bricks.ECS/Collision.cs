namespace Bricks.ECS;

public readonly record struct Collision
{
    public required Entity FirstEntity { get; init; }
    public required Entity SecondEntity { get; init; }
}