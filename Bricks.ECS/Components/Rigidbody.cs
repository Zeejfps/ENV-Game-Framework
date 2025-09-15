using System.Numerics;

namespace Bricks.ECS.Components;

public record struct Rigidbody
{
    public required Vector2 Velocity { get; set; }
    public required bool IsKinematic { get; set; }
}