using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public delegate void GamepadConnectedDelegate(GamepadConnectedEvent evt);
public delegate void GamepadDisconnectedDelegate(GamePadDisconnectedEvent evt);

public interface IInput
{
    event GamepadConnectedDelegate? GamepadConnected;
    event GamepadDisconnectedDelegate? GamepadDisconnected;
    event GamepadButtonStateChangedDelegate? GamepadButtonPressed;
    event GamepadButtonStateChangedDelegate? GamepadButtonReleased;
    
    IMouse Mouse { get; }
    IKeyboard Keyboard { get; }
    IEnumerable<IGamepad> Gamepads { get; }
    
    void Update();
    

    bool TryGetGamepadInSlot(int slot, out IGamepad? gamepad);
    void ConnectGamepad(int slot, IGamepad gamepad);
    void DisconnectGamepad(int slot);
}