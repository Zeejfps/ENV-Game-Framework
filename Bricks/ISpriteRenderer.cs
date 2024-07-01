using EasyGameFramework.Api.Physics;

namespace Bricks;

public interface ISprite
{
    ITextureHandle Texture { get; }
    Rect ScreenRect { get; }
    Rect UvRect { get; }
}

public interface ISpriteRenderer
{
    void Add(ISprite sprite);
    void Render();
    void Setup();
}