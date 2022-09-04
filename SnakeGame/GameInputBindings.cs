using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class GameInputBindings : InputBindings
{
    protected override Dictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } = new()
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

    protected override Dictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = new()
    {
        { MouseButton.Left,       InputActions.ResetAction },
    };
}