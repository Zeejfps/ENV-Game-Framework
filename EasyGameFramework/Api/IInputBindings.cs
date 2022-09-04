using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputBindings
{
    bool TryGetAction(KeyboardKey key, out string? action);
    bool TryGetAction(MouseButton button, out string? action);
}