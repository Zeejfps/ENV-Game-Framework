using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputBindings
{
    IReadOnlyDictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; }
    IReadOnlyDictionary<MouseButton, string> DefaultMouseButtonBindings { get; }

    IDictionary<KeyboardKey, string> OverrideKeyboardKeyBindings { get; }
    IDictionary<MouseButton, string> OverrideMouseButtonBindings { get; }

    bool TryResolveBinding(IGenericGamepad gamepad, GamepadButtonOld button, out string? action);
}