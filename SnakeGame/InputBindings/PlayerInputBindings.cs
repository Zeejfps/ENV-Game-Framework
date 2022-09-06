using EasyGameFramework.Api;

namespace SampleGames;

public abstract class PlayerInputBindings : InputBindings
{
    public abstract InputAction MoveUp { get; }
    public abstract InputAction MoveLeft { get; }
    public abstract InputAction MoveRight { get; }
    public abstract InputAction MoveDown { get; }

    private InputAction[]? m_InputActions;
    public override IEnumerable<InputAction> InputActions
    {
        get
        {
            return m_InputActions ??= new[]
            {
                MoveUp,
                MoveLeft,
                MoveRight,
                MoveDown
            };
        }
    }
}