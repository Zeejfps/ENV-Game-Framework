using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    IContext Context { get; }

    private IWindow Window { get; }
    private TestScene Scene { get; }
    
    public SandboxGame(
        IContext context,
        ILogger logger,
        IEventLoop eventLoop) : base(eventLoop, logger)
    {
        Window = context.Window;
        Context = context;
        Scene = new TestScene(Context, Logger);
    }

    protected override void OnStart()
    {
        Window.Title = "Hello World";
        //Window.IsFullscreen = true;
        Window.Width = 1280;
        Window.Height = 720;
        Window.IsResizable = true;
        Window.IsVsyncEnabled = false;
        Window.OpenCentered();
        
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