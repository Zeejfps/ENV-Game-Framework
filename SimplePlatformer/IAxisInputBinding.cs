using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public interface IAxisInputBinding
{
    float Poll(IKeyboard keyboard, IMouse mouse, IGamepad? gamepad);
}