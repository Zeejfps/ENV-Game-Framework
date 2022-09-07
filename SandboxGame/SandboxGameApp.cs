using EasyGameFramework.Api;

namespace Framework;

public class SandboxGameApp : WindowedApp
{
    private SandboxGame Game { get; }
    
    public SandboxGameApp(IContext context, IEventLoop eventLoop) : base(context.Window, eventLoop)
    {
        Game = new SandboxGame(context, context.Logger, eventLoop);
    }

    protected override void OnRun()
    {
        Game.Start();
    }

    protected override void OnTerminate()
    {
        Game.Stop();
    }
}