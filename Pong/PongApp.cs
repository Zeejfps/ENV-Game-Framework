using EasyGameFramework.Api;

namespace Pong;

public sealed class PongApp : WindowedApp
{
    private PongGame Game { get; }
    
    public PongApp(IWindow window, ILogger logger) : base(window)
    {
        Game = new PongGame(window, logger);
    }

    protected override void Configure(IWindow window)
    {
        window.Title = "Pong";
        window.IsResizable = true;
        window.IsVsyncEnabled = false;
        window.CursorMode = CursorMode.Visible;
        window.SetViewportSize(640, 640);
    }

    protected override void OnOpen()
    {
        Game.Stopped += Game_OnStopped;
        Game.Start();
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