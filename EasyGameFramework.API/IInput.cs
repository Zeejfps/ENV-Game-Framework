using EasyGameFramework.API.InputDevices;

namespace EasyGameFramework.API;

public interface IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
}