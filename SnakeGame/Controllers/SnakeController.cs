using EasyGameFramework.Api;

namespace SampleGames;

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

        if (Index == 0)
        {
            Bindings = new Player1InputBindings();
        }
        else
        {
            Bindings = new Player2InputBindings();
        }
    }
}