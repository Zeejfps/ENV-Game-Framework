using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class GameInputBindings : IInputBindings
{
    public Dictionary<KeyboardKey, string> KeyboardKeyToActionBindings { get; private set; } = new()
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

    public Dictionary<MouseButton, string> MouseButtonToActionBindings { get; }
    
    public string MouseXAxisBinding { get; }
    
    public string MouseYAxisBinding { get; }
}