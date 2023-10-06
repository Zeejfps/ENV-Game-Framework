using EasyGameFramework.Api;

namespace ModelViewer;

public sealed class ModelViewerApp : Game
{
    private readonly Gui m_Gui;

    public ModelViewerApp(IContext context) : base(context)
    {
        m_Gui = new Gui(Window);
    }

    protected override void OnStartup()
    {
        m_Gui.IsVisible = true;
    }

    protected override void OnUpdate()
    {
        //m_Gui.Update(context);
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnShutdown()
    {
    }
}