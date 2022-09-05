using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public sealed class Player1InputBindings : InputBindings
{
    public override IReadOnlyDictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } =
        new Dictionary<KeyboardKey, string>
        {
            { KeyboardKey.W,          InputActions.MoveUpAction },
            { KeyboardKey.A,          InputActions.MoveLeftAction },
            { KeyboardKey.D,          InputActions.MoveRightAction },
            { KeyboardKey.S,          InputActions.MoveDownAction },
        };

    public override IReadOnlyDictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = 
        new Dictionary<MouseButton, string>();
    
    public override bool TryResolveBinding(IGenericGamepad gamepad, GamepadButton button, out string? action)
    {
        if (button == gamepad.SouthButton)
        {
            action = InputActions.ResetAction;
            return true;
        }

        if (button == gamepad.DPadLeftButton)
        {
            action = InputActions.MoveLeftAction;
            return true;
        }

        if (button == gamepad.DPadRightButton)
        {
            action = InputActions.MoveRightAction;
            return true;
        }

        if (button == gamepad.DPadUpButton)
        {
            action = InputActions.MoveUpAction;
            return true;
        }

        if (button == gamepad.DPadDownButton)
        {
            action = InputActions.MoveDownAction;
            return true;
        }
        
        action = null;
        return false;
    }
}