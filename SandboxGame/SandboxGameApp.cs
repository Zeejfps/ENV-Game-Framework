using EasyGameFramework.Api;

namespace Framework;

public class SandboxGameApp : WindowedApp
{
    private SandboxGame Game { get; }
    
    public SandboxGameApp(IContext context) : base(context.Window)
    {
        Game = new SandboxGame(context, context.Logger);
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

    protected override void OnOpen()
    {
        Game.Start();
    }

    protected override void OnClose()
    {
        Game.Stop();
    }
}