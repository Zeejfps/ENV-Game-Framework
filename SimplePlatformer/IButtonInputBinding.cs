using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public interface IButtonInputBinding
{
    bool Poll(IKeyboard keyboard, IMouse mouse, IGamepad? gamepad);
}