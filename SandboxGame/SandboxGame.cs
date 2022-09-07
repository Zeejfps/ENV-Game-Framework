using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : GameApp
{
    IContext Context { get; }

    private TestScene Scene { get; set; }
    
    public SandboxGame(
        IContext context,
        ILogger logger,
        IEventLoop eventLoop) : base(context.Window, eventLoop, logger)
    {
        Context = context;
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
        
        Scene = new TestScene(Context, Logger);
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