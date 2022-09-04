using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputLayer
{
    Dictionary<KeyboardKey, string> KeyboardBindings { get; }
    // void Bind(IInput input);
    // void Unbind(IInput input);
}