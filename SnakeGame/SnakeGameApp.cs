using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SnakeGameApp : WindowedApp
{
    private IContext Context { get; }
    private IGpu Gpu { get; }
    private IWindow Window { get; }
    private IInputSystem Input { get; }
    private IContainer Container { get; }
    private IPlayerPrefs PlayerPrefs { get; }
    
    private SnakeController Player1Controller { get; }
    private SnakeController Player2Controller { get; }
    private AppController AppController { get; }
    
    public SnakeGame Game { get; }

    public SnakeGameApp(
        IContext context,
        IPlayerPrefs playerPrefs,
        IInputSystem inputSystem,
        IEventLoop eventLoop) : base(context.Window, eventLoop)
    {
        Context = context;
        Window = context.Window;
        Gpu = context.Gpu;
        Input = inputSystem;
        Container = context.Container;
        PlayerPrefs = playerPrefs;

        Game = new SnakeGame(Window, Gpu, eventLoop, context.Logger);
        
        Player1Controller = new SnakeController(0, Game.Snakes[0]);
        Player2Controller = new SnakeController(1, Game.Snakes[1]);
        AppController = new AppController(this);
    }
    
    protected override void OnRun()
    {
        Player1Controller.Bind(Input);
        Player2Controller.Bind(Input);
        AppController.Bind(Input);
        Game.Start();
    }

    protected override void OnTerminate()
    {
        Game.Stop();
    }
}