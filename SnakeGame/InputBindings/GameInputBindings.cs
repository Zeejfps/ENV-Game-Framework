using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using ZGF.KeyboardModule;

namespace SampleGames;

public class GameInputBindings : InputBindings
{
    public InputAction QuitAction { get; } = new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.Escape),
        }
    };
    
    public InputAction ResetAction { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.R),
            new GamepadButtonBinding(0, GamepadButton.Back)
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
            new GamepadButtonBinding(0, GamepadButton.Start),
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