using EasyGameFramework.Api;

namespace ModelViewer;

public sealed class ModelViewerApp : Game
{
    private readonly Gui m_Gui;

    public ModelViewerApp(IContext context) : base(context)
    {
        m_Gui = new Gui();
    }

    protected override void OnStartup()
    {
        m_Gui.IsVisible = true;
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnShutdown()
    {
    }
}

public class Gui
{
    public bool IsVisible { get; set; }
}