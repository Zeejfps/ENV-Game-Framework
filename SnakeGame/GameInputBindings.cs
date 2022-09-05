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
        
        { KeyboardKey.W,          InputActions.MoveUpAction },
        { KeyboardKey.UpArrow,    InputActions.MoveUpAction },
        
        { KeyboardKey.A,          InputActions.MoveLeftAction },
        { KeyboardKey.LeftArrow,  InputActions.MoveLeftAction },
        
        { KeyboardKey.D,          InputActions.MoveRightAction },
        { KeyboardKey.RightArrow, InputActions.MoveRightAction },
        
        { KeyboardKey.S,          InputActions.MoveDownAction },
        { KeyboardKey.DownArrow,  InputActions.MoveDownAction },
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

        action = null;
        return false;
    }
}