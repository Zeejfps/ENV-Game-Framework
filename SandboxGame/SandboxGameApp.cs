using EasyGameFramework.Api;

namespace Framework;

public class SandboxGameApp : WindowedApp
{
    private SandboxGame Game { get; }
    
    public SandboxGameApp(IContext context, IEventLoop eventLoop) : base(context.Window, eventLoop)
    {
        Game = new SandboxGame(context, context.Logger, eventLoop);
    }

    protected override void Configure(IWindow window)
    {
        window.Title = "Sandbox Game";
        //Window.IsFullscreen = true;
        window.Width = 1280;
        window.Height = 720;
        window.IsResizable = true;
        window.IsVsyncEnabled = true;
    }

    protected override void Start()
    {
        Game.Start();
    }

    protected override void Stop()
    {
        Game.Stop();
    }
}