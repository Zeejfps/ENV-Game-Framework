using EasyGameFramework.Api;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public struct SpriteInstanceData
{
    
}

public interface ISprite : IInstancedItem<SpriteInstanceData>
{
    ITextureHandle Texture { get; }
    Rect ScreenRect { get; }
    Rect UvRect { get; }
}

public interface ISpriteRenderer
{
    void Add(ISprite sprite);
    void Render(ICamera camera);
    void Load();
}