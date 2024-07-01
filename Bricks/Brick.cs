using EasyGameFramework.Api.Physics;

namespace Bricks;

public sealed class Brick : ISprite
{
    public ITextureHandle Texture { get; set; }
    public Rect ScreenRect { get; set; }
    public Rect UvRect { get; set; }
}