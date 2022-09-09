using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;

namespace SimplePlatformer;

public struct Sprite
{
    public IHandle<IGpuTexture>? SpriteSheet { get; set; }
    public Vector2 UVs { get; set; }
    public Vector2 Size { get; set; }
    public Vector3 Color { get; set; }
    public Vector2 Pivot { get; set; }
}