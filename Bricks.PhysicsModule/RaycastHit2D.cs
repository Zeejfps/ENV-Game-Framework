using System.Numerics;

namespace Bricks.PhysicsModule;

public readonly struct RaycastHit2D
{
    public Vector2 Normal { get; init; }
    public Vector2 Point { get; init; }
    public float Distance { get; init; }
}