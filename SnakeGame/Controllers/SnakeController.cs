using EasyGameFramework.Api;

namespace SampleGames;

public class SnakeController : Controller
{
    private int Index { get; }
    private Snake Snake { get; }
    protected override IInputBindings Bindings { get; }

    public SnakeController(IInputSystem inputSystem, int index, Snake snake) : base(inputSystem)
    {
        Index = index;
        Snake = snake;

        PlayerInputBindings bindings = Index == 0 ? new Player1InputBindings() : new Player2InputBindings();
        Bindings = bindings;
        
        BindOnPressed(bindings.MoveUp, Snake.TurnNorth);
        BindOnPressed(bindings.MoveLeft, Snake.TurnWest);
        BindOnPressed(bindings.MoveRight, Snake.TurnEast);
        BindOnPressed(bindings.MoveDown, Snake.TurnSouth);
    }

    protected override void OnEnable()
    {
        
    }
}