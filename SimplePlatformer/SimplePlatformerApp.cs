using EasyGameFramework.Api;

namespace SimplePlatformer;

public class SimplePlatformerApp : WindowedApp
{
    private SimplePlatformer Game { get; }
    
    public SimplePlatformerApp(IWindow window, IContainer container) : base(window)
    {
        Game = container.New<SimplePlatformer>();
    }

    protected override void Configure(IWindow window)
    {
        window.Title = "Simple Platformer";
        window.IsFullscreen = true;
        //window.CursorMode = CursorMode.HiddenAndLocked;
        window.IsResizable = false;
        window.IsVsyncEnabled = true;
        window.SetViewportSize(1280, 720);
    }

    protected override void OnOpen()
    {
        Game.Stopped += Game_OnStopped;
        Game.Start();
    }

    protected override void OnClose()
    {
        Game.Stop();
    }

    private void Game_OnStopped()
    {
        Game.Stopped -= Game_OnStopped;
        Close();
    }
}