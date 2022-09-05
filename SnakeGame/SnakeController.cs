using EasyGameFramework.Api;

namespace SampleGames;

public class GameController : Controller
{
    protected override IInputBindings Bindings => m_Bindings;

    private IInputBindings m_Bindings;
    
    private SnakeGame Game { get; }
    public GameController(SnakeGame game)
    {
        Game = game;
        
        BindAction(InputActions.QuitAction, Game.Stop);
        BindAction(InputActions.ResetAction, Game.ResetLevel);
        BindAction(InputActions.IncreaseSpeedAction, Game.IncreaseSpeed);
        BindAction(InputActions.DecreaseSpeedAction, Game.DecreaseSpeed);
        BindAction(InputActions.PauseResumeAction, TogglePause);
        
        m_Bindings = new GameInputBindings();
    }
    
    public void TogglePause()
    {
        if (Game.IsPaused)
        {
            Game.IsPaused = false;
            m_Bindings = new GameInputBindings();
            return;
        }
        
        Game.IsPaused = true;
        m_Bindings = new UIInputBindings();
    }
}

public class SnakeController : Controller
{
    private int Index { get; }
    private Snake Snake { get; }
    protected override IInputBindings Bindings { get; }

    public SnakeController(int index, Snake snake)
    {
        Index = index;
        Snake = snake;
        
        BindAction(InputActions.MoveUpAction, Snake.TurnNorth);
        BindAction(InputActions.MoveLeftAction, Snake.TurnWest);
        BindAction(InputActions.MoveRightAction, Snake.TurnEast);
        BindAction(InputActions.MoveDownAction, Snake.TurnSouth);
        BindAction(InputActions.IncreaseSpeedAction, IncreaseSpeed);
        BindAction(InputActions.DecreaseSpeedAction, DecreaseSpeed);

        if (Index == 0)
        {
            Bindings = new Player1InputBindings();
        }
        else
        {
            Bindings = new Player2InputBindings();
        }
    }
    
    private void IncreaseSpeed()
    {
        Snake.Speed += 0.5f;
    }

    private void DecreaseSpeed()
    {
        Snake.Speed -= 0.5f;
    }
}