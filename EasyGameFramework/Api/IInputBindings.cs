using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputBindings
{
    Dictionary<KeyboardKey, string> KeyboardKeyActionBindings { get; }
}