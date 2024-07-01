using EasyGameFramework.Api.Physics;

namespace Bricks;

public sealed class Sprite
{
    public ITextureHandle TextureHandle { get; set; }
    public Rect UVs { get; set; }
}