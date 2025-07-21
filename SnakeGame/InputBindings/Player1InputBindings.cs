using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using ZGF.KeyboardModule;

namespace SampleGames;

public sealed class Player1InputBindings : PlayerInputBindings
{
    public override InputAction MoveUp { get; } = new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.W),
            new GamepadButtonBinding(0, GamepadButton.DPadUp)        
        }
    };

    public override InputAction MoveLeft { get; } = new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.A),
            new GamepadButtonBinding(0, GamepadButton.DPadLeft)
        }
    };
    
    public override InputAction MoveRight { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.D),
            new GamepadButtonBinding(0, GamepadButton.DPadRight)
        }
    };
    
    public override InputAction MoveDown { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.S),
            new GamepadButtonBinding(0, GamepadButton.DPadDown)
        }
    };
}