using System.Numerics;
using EasyGameFramework.Api.Rendering;

namespace SimplePlatformer;

public struct Sprite
{
    public IGpuTextureHandle? SpriteSheet { get; set; }
    public Vector2 Offset { get; set; }
    public Vector2 Size { get; set; }
    public Vector3 Color { get; set; }
    public Vector2 Pivot { get; set; }
    
    public bool FlipX { get; set; }
}