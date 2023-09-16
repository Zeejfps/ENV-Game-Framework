using EasyGameFramework.Api;using OpenGL;
using static OpenGL.Gl;

namespace OpenGLSandbox;

public sealed class OpenGlSandboxGame : Game
{
    private readonly IScene m_Scene;
    
    public OpenGlSandboxGame(IContext context) : base(context)
    {
        m_Scene = new BasicRenderingScene();
    }

    protected override void OnStartup()
    {
        Window.SetScreenSize(640, 640);

        m_Scene.Load();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        m_Scene.Render();
        glFlush();
    }

    protected override void OnShutdown()
    {
        m_Scene.Unload();
    }
}