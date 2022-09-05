using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class UIInputBindings : InputBindings
{
    public override Dictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } = new()
    {
        {KeyboardKey.P, InputActions.PauseResumeAction}
    };

    public override Dictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = new();
    public override bool TryResolveBinding(IGenericGamepad gamepad, GamepadButton button, out string? action)
    {
        action = null;
        return false;
    }
}