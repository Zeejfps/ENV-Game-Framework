namespace Bricks.ECS.Components;

public record struct Sprite
{
    public required SpriteKind Kind { get; set; }
    public required float Width { get; set; }
    public required float Height { get; set; }
}