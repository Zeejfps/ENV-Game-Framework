using EasyGameFramework.Api;

namespace SampleGames;

public class GameController : Controller
{
    protected override IInputBindings Bindings => m_Bindings;

    private IInputBindings m_Bindings;

    private GameInputBindings GameInputBindings { get; }
    private UIInputBindings UIInputBindings { get; }
    
    private SnakeGame Game { get; }
    private SnakeController Player1Controller { get; }
    private SnakeController Player2Controller { get; }

    public GameController(IInputSystem inputSystem, SnakeGame game) : base(inputSystem)
    {
        Game = game;

        GameInputBindings = new GameInputBindings();
        UIInputBindings = new UIInputBindings();
        
        m_Bindings = GameInputBindings;

        BindOnPressed(GameInputBindings.QuitAction, Game.Exit);
        BindOnPressed(GameInputBindings.ResetAction, Game.Restart);
        BindOnPressed(GameInputBindings.IncreaseSpeedAction, Game.IncreaseSpeed);
        BindOnPressed(GameInputBindings.DecreaseSpeedAction, Game.DecreaseSpeed);
        BindOnPressed(GameInputBindings.PauseResumeAction, TogglePause);
        
        Player1Controller = new SnakeController(inputSystem, 0, Game.Snakes[0]);
        Player2Controller = new SnakeController(inputSystem, 1, Game.Snakes[1]);
    }

    protected override void OnEnable()
    {
        Player1Controller.Enable();
        Player2Controller.Enable();
    }

    private void TogglePause()
    {
        if (Game.IsPaused)
        {
            Game.IsPaused = false;
            m_Bindings = GameInputBindings;
            return;
        }
        
        Game.IsPaused = true;
        m_Bindings = UIInputBindings;
    }
}