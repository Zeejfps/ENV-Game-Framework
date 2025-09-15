using System.Numerics;

namespace Bricks.ECS.Components;

public record struct Transform
{
    public Vector2 Position { get; set; }
}