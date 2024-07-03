using EasyGameFramework.Api;
using OpenGL;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public struct SpriteInstanceData
{
    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public Rect ScreenRect;
}

public interface ISprite : IInstancedItem<SpriteInstanceData>
{
    ITextureHandle Texture { get; }
    Rect UvRect { get; }
}

public interface ISpriteRenderer
{
    void Add(ISprite sprite);
    void Render(ICamera camera);
    void Load();
}