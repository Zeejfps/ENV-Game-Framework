using EasyGameFramework.Api;using OpenGL;
using static OpenGL.Gl;

namespace OpenGLSandbox;

public sealed class OpenGlSandboxGame : Game
{
    private readonly Program1Scene m_Program1Scene = new();
    
    public OpenGlSandboxGame(IContext context) : base(context)
    {
    }

    protected override void OnStartup()
    {
        Window.SetScreenSize(640, 640);

        m_Program1Scene.Load();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        m_Program1Scene.Render();
        glFlush();
    }

    protected override void OnShutdown()
    {
        m_Program1Scene.Unload();
    }
}