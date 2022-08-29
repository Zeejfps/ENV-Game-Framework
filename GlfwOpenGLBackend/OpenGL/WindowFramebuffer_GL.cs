using EasyGameFramework.API.AssetTypes;
using OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class WindowFramebuffer_GL : IGpuFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public uint Id => 0;

    public WindowFramebuffer_GL(int width, int height, GetProcAddressHandler getProcAddress)
    {
        Width = width;
        Height = height;
        Import(getProcAddress);
    }

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