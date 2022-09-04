using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class UIInputBindings : IInputBindings
{
    public Dictionary<KeyboardKey, string> KeyboardKeyToActionBindings { get; } = new()
    {
        {KeyboardKey.P, InputActions.PauseResumeAction}
    };

    public Dictionary<MouseButton, string> MouseButtonToActionBindings { get; } = new();
    public string MouseXAxisBinding { get; } = string.Empty;
    public string MouseYAxisBinding { get; } = string.Empty;
}