using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputLayer
{
    Dictionary<KeyboardKey, string> KeyboardKeyActionBindings { get; }
}