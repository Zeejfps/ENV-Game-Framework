using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputBindings
{
    IEnumerable<KeyValuePair<KeyboardKey, string>> KeyboardBindings { get; }
    IEnumerable<KeyValuePair<MouseButton, string>> MouseBindings { get; }

    bool TryGetAction(KeyboardKey key, out string? action);
    bool TryGetAction(MouseButton button, out string? action);

    void LoadDefaults();

    void BindKeyboardKey(KeyboardKey key, string action);
    void BindMouseButton(MouseButton button, string action);
}