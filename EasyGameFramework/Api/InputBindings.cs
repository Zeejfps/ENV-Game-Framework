using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    public abstract IReadOnlyDictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; }
    public abstract IReadOnlyDictionary<MouseButton, string> DefaultMouseButtonBindings { get; }

    public IDictionary<KeyboardKey, string> OverrideKeyboardKeyBindings { get; } = new Dictionary<KeyboardKey, string>();
    public IDictionary<MouseButton, string> OverrideMouseButtonBindings { get; } = new Dictionary<MouseButton, string>();
    
    public abstract bool TryResolveBinding(IGenericGamepad gamepad, GamepadButtonOld button, out string? action);
}