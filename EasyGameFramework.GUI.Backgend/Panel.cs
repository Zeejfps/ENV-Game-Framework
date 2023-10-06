using System.Numerics;
using OpenGL;

namespace OpenGLSandbox;

public struct Panel
{
    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public Color BackgroundColor;

    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public Vector4 BorderRadius;

    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public Rect ScreenRect;

    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public Color BorderColor;

    [InstancedAttrib(4, Gl.GL_FLOAT)]
    public BorderSize BorderSize;

    public override string ToString()
    {
        return $"{nameof(ScreenRect)}: {ScreenRect}, {nameof(BackgroundColor)}: {BackgroundColor}, {nameof(BorderColor)}: {BorderColor}, {nameof(BorderSize)}: {BorderSize}, {nameof(BorderRadius)}: {BorderRadius}";
    }
}