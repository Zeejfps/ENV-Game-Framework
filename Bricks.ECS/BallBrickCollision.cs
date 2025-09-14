namespace Bricks.ECS;

public readonly record struct BallBrickCollision
{
    public required Entity BallEntity { get; init; }
    public required Entity BrickEntity { get; init; }
}