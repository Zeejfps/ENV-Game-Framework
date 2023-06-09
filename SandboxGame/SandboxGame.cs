using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    IContext Context { get; }

    private IWindow Window { get; }
    private TestScene Scene { get; }
    
    public SandboxGame(
        IContext context,
        ILogger logger) : base(context.Window, logger)
    {
        Window = context.Window;
        Context = context;
        Scene = new TestScene(Context, Logger);
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