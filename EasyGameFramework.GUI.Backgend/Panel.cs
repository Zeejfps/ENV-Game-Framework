using System.Numerics;
using OpenGL;

namespace OpenGLSandbox;

public struct Panel
{
    [VertexAttrib(4, Gl.GL_FLOAT)]
    public Color BackgroundColor;

    [VertexAttrib(4, Gl.GL_FLOAT)]
    public Vector4 BorderRadius;

    [VertexAttrib(4, Gl.GL_FLOAT)]
    public Rect ScreenRect;

    [VertexAttrib(4, Gl.GL_FLOAT)]
    public Color BorderColor;

    [VertexAttrib(4, Gl.GL_FLOAT)]
    public BorderSize BorderSize;

    public override string ToString()
    {
        return $"{nameof(ScreenRect)}: {ScreenRect}, {nameof(BackgroundColor)}: {BackgroundColor}, {nameof(BorderColor)}: {BorderColor}, {nameof(BorderSize)}: {BorderSize}, {nameof(BorderRadius)}: {BorderRadius}";
    }
}