using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public interface IButtonInputBinding
{
    bool Poll(Controller controller);
}