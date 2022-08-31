using EasyGameFramework.Api.AssetTypes;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class WindowFramebuffer_GL : IGpuFramebuffer
{
    public WindowFramebuffer_GL(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public uint Id => 0;

    public void Clear(float r, float g, float b, float a)
    {
        glClearColor(r, g, b, a);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void SetSize(int width, int height)
    {
        Width = width;
        Height = height;
        glViewport(0, 0, width, height);
    }

    public void Dispose()
    {
    }
}