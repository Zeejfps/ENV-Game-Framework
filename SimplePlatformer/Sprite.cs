using System.Numerics;

namespace SimplePlatformer;

public struct Sprite
{
    public Vector2 UVs { get; set; }
    public Vector3 Color { get; set; }
    public Vector2 Pivot { get; set; }
}