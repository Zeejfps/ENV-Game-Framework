using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    private IContext Context { get; }
    private TestScene Scene { get; }
    
    public SandboxGame(
        IContext context,
        ILogger logger) : base(context.Window, logger)
    {
        Context = context;
        Scene = new TestScene(Context, Logger);
    }

    protected override void Configure(IWindow window)
    {
        window.Title = "Sandbox Game";
        //Window.IsFullscreen = true;
        window.ViewportWidth = 1280;
        window.ViewportHeight = 720;
        window.IsResizable = true;
        window.IsVsyncEnabled = true;
    }

    protected override void OnStart()
    {
        Scene.Load();
    }

    protected override void OnUpdate()
    {
        Scene.Update(Clock.UpdateDeltaTime);
    }

    protected override void OnRender()
    {
        Scene.Render();
    }

    protected override void OnStop()
    {
        
    }
}