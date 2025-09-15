namespace Bricks.ECS;

public readonly record struct BallCollision
{
    public required Entity BallEntity { get; init; }
    public required Entity OtherEntity { get; init; }
}