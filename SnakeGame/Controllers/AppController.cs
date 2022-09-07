using EasyGameFramework.Api;

namespace SampleGames;

public class AppController : Controller
{
    protected override IInputBindings Bindings => m_Bindings;

    private IInputBindings m_Bindings;
    
    private GameInputBindings GameInputBindings { get; }
    private UIInputBindings UIInputBindings { get; }
    private SnakeGameApp App { get; }
    private SnakeGame Game { get; }
    
    public AppController(SnakeGameApp app)
    {
        App = app;
        Game = App.Game;

        GameInputBindings = new GameInputBindings();
        UIInputBindings = new UIInputBindings();
        
        m_Bindings = GameInputBindings;

        BindOnPressed(GameInputBindings.QuitAction, App.Terminate);
        BindOnPressed(GameInputBindings.ResetAction, Game.Restart);
        BindOnPressed(GameInputBindings.IncreaseSpeedAction, Game.IncreaseSpeed);
        BindOnPressed(GameInputBindings.DecreaseSpeedAction, Game.DecreaseSpeed);
        BindOnPressed(GameInputBindings.PauseResumeAction, TogglePause);
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