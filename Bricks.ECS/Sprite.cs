namespace Bricks.ECS;

public record struct Sprite
{
    public required SpriteKind Kind { get; set; }
}