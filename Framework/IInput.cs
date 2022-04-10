using ENV.Engine.InputDevices;

namespace ENV.Engine;

public interface IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
}