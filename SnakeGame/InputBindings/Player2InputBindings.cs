using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public sealed class Player2InputBindings : PlayerInputBindings
{
    // public override IReadOnlyDictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } =
    //     new Dictionary<KeyboardKey, string>
    //     {
    //         { KeyboardKey.UpArrow,          InputActions.MoveUpAction },
    //         { KeyboardKey.LeftArrow,          InputActions.MoveLeftAction },
    //         { KeyboardKey.RightArrow,          InputActions.MoveRightAction },
    //         { KeyboardKey.DownArrow,          InputActions.MoveDownAction },
    //     };
    //
    // public override IReadOnlyDictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = 
    //     new Dictionary<MouseButton, string>();
    //
    // public override bool TryResolveBinding(IGamepad gamepad, GamepadButtonOld button, out string? action)
    // {
    //     action = null;
    //     return false;
    // }
    
    public override InputAction MoveUp { get; } = new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.UpArrow),
            new GamepadButtonBinding(1, GamepadButton.DPadUp)        
        }
    };

    public override InputAction MoveLeft { get; } = new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.LeftArrow),
            new GamepadButtonBinding(1, GamepadButton.DPadLeft)
        }
    };
    
    public override InputAction MoveRight { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.RightArrow),
            new GamepadButtonBinding(1, GamepadButton.DPadRight)
        }
    };
    
    public override InputAction MoveDown { get; }= new()
    {
        ButtonBindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.DownArrow),
            new GamepadButtonBinding(1, GamepadButton.DPadDown)
        }
    };
}