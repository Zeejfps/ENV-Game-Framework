using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }

    void Update();
}