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
    
    private void TogglePause()
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