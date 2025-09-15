using System.Numerics;

namespace Bricks.ECS;

public record struct Sprite
{
    public required Vector2 Position { get; set; }
    public required SpriteKind Kind { get; set; }
}