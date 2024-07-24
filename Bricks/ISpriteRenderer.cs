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

public struct Sprite
{
    [VertexAttrib(4, Gl.GL_FLOAT)]
    public ScreenRect AtlasRect;
    
    [VertexAttrib(4, Gl.GL_FLOAT)]
    public ScreenRect ScreenRect;

    [VertexAttrib(4, Gl.GL_FLOAT)]
    public Color Tint;
}

public interface ISpriteRenderer
{
    void Add(IEntity<Sprite> sprite);
    void Load();
    void Render(Matrix4x4 viewProjectionMatrix);
}