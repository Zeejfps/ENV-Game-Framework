using EasyGameFramework.Api;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class TestGame : Game
{
    private IEntity m_CurrentWorld;

    private readonly BitmapFontTextRenderer m_TextRenderer;
    
    public TestGame(IContext context) : base(context)
    {
        m_TextRenderer = new BitmapFontTextRenderer(context.Window);
        m_CurrentWorld = new MainWorld(context.Window, m_TextRenderer);
    }

    protected override void OnStartup()
    {
        Window.Title = "OOP ECS";
        Window.OpenCentered();
        
        m_TextRenderer.Load(new BmpFontFile
        {
            FontName = "test",
            PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
        });
        
        m_CurrentWorld.Load();
    }

    protected override void OnUpdate()
    {
        m_TextRenderer.Update();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnShutdown()
    {
        m_TextRenderer.Unload();
    }
}