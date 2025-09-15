using System.Numerics;

namespace Bricks.ECS;

public record struct Rigidbody
{
    public required Vector2 Position { get; set; }
    public required Vector2 Velocity { get; set; }
    public required bool IsKinematic { get; set; }
}