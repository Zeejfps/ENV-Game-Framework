using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class UIInputBindings : InputBindings
{
    protected override Dictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } = new()
    {
        {KeyboardKey.P, InputActions.PauseResumeAction}
    };

    protected override Dictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = new();
}