using Framework.InputDevices;

namespace Framework;

public interface IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
}