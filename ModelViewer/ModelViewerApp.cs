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

    public ModelViewerApp(IContext context) : base(context)
    {
        //BuildContext = new BuildContext();
        //BuildContext.DiContainer.BindSingleton();
        m_Gui = new Gui(Window);
    }

    protected override void OnStartup()
    {
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