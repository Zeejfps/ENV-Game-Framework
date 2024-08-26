using EasyGameFramework.Api;
using EasyGameFramework.Builder;
using OpenGLSandbox;

namespace ModelViewer;

class BuildContext : IBuildContext
{
    public IPanelRenderer PanelRenderer { get; }
    public ITextRenderer TextRenderer { get; }
    public FocusTree FocusTree { get; }

    public BuildContext(IPanelRenderer panelRenderer, FocusTree focusTree, ITextRenderer textRenderer)
    {
        PanelRenderer = panelRenderer;
        FocusTree = focusTree;
        TextRenderer = textRenderer;
    }
}

public sealed class ModelViewerApp : Game
{
    private BuildContext BuildContext { get; }
    private readonly Gui m_Gui;
    private readonly BitmapFontTextRenderer m_TextRenderer;
    private readonly PanelRenderer m_PanelRenderer;
    
    public ModelViewerApp(IGameContext gameContext) : base(gameContext)
    {
        m_TextRenderer = new BitmapFontTextRenderer(Window);
        m_PanelRenderer = new PanelRenderer(Window);
        var focusTree = new FocusTree(Input, Window);
        
        BuildContext = new BuildContext(m_PanelRenderer, focusTree, m_TextRenderer);
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
        GL46.glEnable(GL46.GL_BLEND);
        GL46.glBlendFunc(GL46.GL_SRC_ALPHA, GL46.GL_ONE_MINUS_SRC_ALPHA);
        GL46.glClearColor(0.1f, 0.2f, 0.3f, 1.0f);
        GL46.glClear(GL46.GL_COLOR_BUFFER_BIT | GL46.GL_DEPTH_BUFFER_BIT);
        
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