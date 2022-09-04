using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    IContext Context { get; }

    private TestScene Scene { get; set; }
    
    private ILogger Logger { get; }
    
    public SandboxGame(IContext context, ILogger logger) : base(context.Window, context.Input)
    {
        Context = context;
        Logger = logger;
    }

    protected override void OnStart()
    {
        Window.Title = "Hello World";
        //Window.IsFullscreen = true;
        Window.Width = 1280;
        Window.Height = 720;
        Window.IsResizable = true;
        Window.IsVsyncEnabled = true;
        Window.ShowCentered();
        
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