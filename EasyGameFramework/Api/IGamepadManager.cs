using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IGamepadManager
{
    event GamepadConnectedDelegate? GamepadConnected;
    event GamepadDisconnectedDelegate? GamepadDisconnected;
    event GamepadButtonStateChangedDelegate? GamepadButtonPressed;
    event GamepadButtonStateChangedDelegate? GamepadButtonReleased;
    
    IEnumerable<IGamepad> Gamepads { get; }

    bool TryGetGamepadInSlot(int slot, out IGamepad? gamepad);
    void ConnectGamepad(int slot, IGamepad gamepad);
    void DisconnectGamepad(int slot);
}