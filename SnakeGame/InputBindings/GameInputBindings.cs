using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class GameInputBindings : InputBindings
{
    public InputAction QuitAction { get; } = new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.Escape),
            new GamepadButtonBinding(0, GamepadButton.Back)
        }
    };
    
    public InputAction ResetAction { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.R),
        }
    };
    
    public InputAction IncreaseSpeedAction { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.Equals),
            new GamepadButtonBinding(0, GamepadButton.RightBumper)
        }
    };
    
    public InputAction DecreaseSpeedAction { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.Minus),
            new GamepadButtonBinding(0, GamepadButton.LeftBumper)
        }
    };
    
    public InputAction PauseResumeAction { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.P),
        }
    };
    
    public override IEnumerable<InputAction> InputActions { get; }
    
    public GameInputBindings()
    {
        InputActions = new[]
        {
            QuitAction,
            ResetAction,
            IncreaseSpeedAction,
            DecreaseSpeedAction,
            PauseResumeAction,
        };
    }
}