using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class GameInputBindings : InputBindings
{
    public override Dictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } = new()
    {
        { KeyboardKey.Escape,     InputActions.QuitAction },
        { KeyboardKey.R,          InputActions.ResetAction },
        { KeyboardKey.Equals,     InputActions.IncreaseSpeedAction },
        { KeyboardKey.Minus,      InputActions.DecreaseSpeedAction },
        { KeyboardKey.P,          InputActions.PauseResumeAction },
    };

    public override Dictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = new()
    {
        { MouseButton.Left,       InputActions.ResetAction },
    };
    
    public override bool TryResolveBinding(IGenericGamepad gamepad, InputButton button, out string? action)
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

        if (button == gamepad.RightBumperButton)
        {
            action = InputActions.IncreaseSpeedAction;
            return true;
        }

        if (button == gamepad.LeftBumperButton)
        {
            action = InputActions.DecreaseSpeedAction;
            return false;
        }

        action = null;
        return false;
    }
}