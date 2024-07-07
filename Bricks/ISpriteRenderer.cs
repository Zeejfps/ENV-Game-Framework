using System.Numerics;
using OpenGL;
using OpenGLSandbox;

namespace Bricks;

public struct ScreenRect
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
}

public struct SpriteInstanceData
{
    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public ScreenRect AtlasRect;
    
    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public ScreenRect ScreenRect;

    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public Color Tint;
}

public interface ISprite : IInstancedItem<SpriteInstanceData>
{
    
}

public interface ISpriteRenderer
{
    void Add(ISprite sprite);
    void Render(Matrix4x4 viewProjectionMatrix);
    void Load();
}