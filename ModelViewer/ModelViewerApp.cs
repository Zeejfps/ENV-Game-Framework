using EasyGameFramework.Api;
using EasyGameFramework.Builder;
using OpenGLSandbox;

namespace ModelViewer;

class BuildContext : IBuildContext
{
    public DiContainer DiContainer { get; } = new();
    
    public T Get<T>()
    {
        return DiContainer.New<T>();
    }
}

public sealed class ModelViewerApp : Game
{
    private BuildContext BuildContext { get; }
    private readonly Gui m_Gui;
    private readonly BitmapFontTextRenderer m_TextRenderer;
    private readonly PanelRenderer m_PanelRenderer;
    
    public ModelViewerApp(IContext context) : base(context)
    {
        m_TextRenderer = new BitmapFontTextRenderer(Window);
        m_PanelRenderer = new PanelRenderer(Window);
        
        BuildContext = new BuildContext();
        BuildContext.DiContainer.BindSingleton<ITextRenderer>(m_TextRenderer);
        BuildContext.DiContainer.BindSingleton<IPanelRenderer>(m_PanelRenderer);
        
        m_Gui = new Gui(Window);
    }

    protected override void OnStartup()
    {
        m_PanelRenderer.Load();
        m_TextRenderer.Load(new []
        {
            new BmpFontFile
            {
                FontName = "Segoe UI",
                PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
            },
        });
    }

    protected override void OnUpdate()
    {
        m_Gui.Update(BuildContext);
        
        m_PanelRenderer.Update();
        m_TextRenderer.Update();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnShutdown()
    {
    }
}