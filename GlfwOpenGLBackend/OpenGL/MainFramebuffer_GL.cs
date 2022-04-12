using OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class MainFramebuffer_GL : IFramebuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    public void Init(int width, int height, GetProcAddressHandler getProcAddress)
    {
        Width = width;
        Height = height;
        Import(getProcAddress);
        glEnable(GL_CULL_FACE);
        glEnable(GL_DEPTH_TEST);
    }

    public void Use()
    {
        glBindFramebuffer(GL_FRAMEBUFFER, 0);
    }

    public void Clear()
    {
        glClearColor(.42f, .607f, .82f, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
        glViewport(0, 0, width, height);
    }
}