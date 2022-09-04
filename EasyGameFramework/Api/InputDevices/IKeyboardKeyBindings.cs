namespace EasyGameFramework.Api.InputDevices;

public interface IKeyboardKeyBindings
{
    bool TryGetAction(KeyboardKey key, out string? action);
    void BindKeyToAction(KeyboardKey key, string action);
    void UnbindKey(KeyboardKey key);
}