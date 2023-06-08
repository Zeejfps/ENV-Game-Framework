using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SnakeGameApp : WindowedApp
{
    private IContext Context { get; }
    private IGpu Gpu { get; }
    private IInputSystem Input { get; }
    private IContainer Container { get; }
    private GameController GameController { get; }
    private SnakeGame Game { get; }

    public SnakeGameApp(
        IContext context,
        IInputSystem inputSystem,
        IEventLoop eventLoop) : base(context.Window, eventLoop)
    {
        Context = context;
        Gpu = context.Gpu;
        Input = inputSystem;
        Container = context.Container;

        Game = new SnakeGame(Window, eventLoop, context.Logger);
        GameController = new GameController(Input, Game);
    }
    
    protected override void Configure(IWindow window)
    {
        window.ViewportWidth = 640;
        window.ViewportHeight = 640;
        window.IsVsyncEnabled = true;
        window.IsResizable = false;
        window.Title = "SNAEK";
        window.CursorMode = CursorMode.HiddenAndLocked;
    }

    protected override void OnOpen()
    {
        GameController.Enable();
        Game.Stopped += Game_OnStopped;
        Game.Start();
    }

    protected override void OnClose()
    {
        Game.Stop();
    }

    private void Game_OnStopped()
    {
        GameController.Disable();
        Game.Stopped -= Game_OnStopped;
        Close();
    }
}