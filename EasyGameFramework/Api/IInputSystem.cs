using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInputSystem
{
    IMouse Mouse { get; }
    IKeyboard Keyboard { get; }
    IGamepadManager GamepadManager { get; }
}