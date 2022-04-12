using OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class WindowFramebuffer_GL : IFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ITexture ColorTexture { get; }

    public void Init(int width, int height, GetProcAddressHandler getProcAddress)
    {
        Width = width;
        Height = height;
        Import(getProcAddress);
        glEnable(GL_CULL_FACE);
    }

    public void Use()
    {
        glBindFramebuffer(GL_FRAMEBUFFER, 0);
        glViewport(0, 0, Width, Height);
        glDisable(GL_DEPTH_TEST);
    }

    public void Clear(float r, float g, float b)
    {
        glClearColor(r,g,b, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
        glViewport(0, 0, width, height);
    }

    public void Dispose()
    {
    }
}