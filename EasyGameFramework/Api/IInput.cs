using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public delegate void GamepadConnectedDelegate(GamepadConnectedEvent evt);
public delegate void GamepadDisconnectedDelegate(GamePadDisconnectedEvent evt);

public interface IInput
{
    event GamepadConnectedDelegate GamepadConnected;
    event GamepadDisconnectedDelegate GamepadDisconnected;
    
    IMouse Mouse { get; }
    IKeyboard Keyboard { get; }
    IEnumerable<IGenericGamepad> Gamepads { get; }
    
    void Update();
    

    bool TryGetGamepadInSlot(int slot, out IGenericGamepad? gamepad);
    void ConnectGamepad(int slot, IGenericGamepad gamepad);
    void DisconnectGamepad(int slot);
}