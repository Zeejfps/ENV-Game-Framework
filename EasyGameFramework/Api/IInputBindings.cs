using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputBindings
{
    Dictionary<KeyboardKey, string> KeyboardKeyToActionBindings { get; }
    Dictionary<MouseButton, string> MouseButtonToActionBindings { get; }
}