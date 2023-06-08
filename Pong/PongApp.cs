using EasyGameFramework.Api;

namespace Pong;

public sealed class PongApp : WindowedApp
{
    private PongGame Game { get; }
    
    public PongApp(IWindow window, ILogger logger, IEventLoop eventLoop) : base(window, eventLoop)
    {
        Game = new PongGame(window, eventLoop, logger);
    }

    protected override void Configure(IWindow window)
    {
        window.Title = "Pong";
        window.IsResizable = false;
        window.IsVsyncEnabled = true;
        window.CursorMode = CursorMode.HiddenAndLocked;
        window.SetViewportSize(640, 480);
    }

    protected override void OnOpen()
    {
        Game.Start();
        Game.Stopped += Game_OnStopped;
    }

    private void Game_OnStopped()
    {
        Game.Stopped -= Game_OnStopped;
        Close();
    }

    protected override void OnClose()
    {
        Game.Stop();
    }
}