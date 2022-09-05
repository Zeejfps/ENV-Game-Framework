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
    IEnumerable<IGamepad> Gamepads { get; }

    
    IInputBindings? Bindings { get; set; }
    
    
    void Update();
    
    
    void BindAction(string actionName, Action handler);
    void UnbindAction(string actionName, Action handler);

    
    void BindAxis(string axisName, Action<float> handler);
    void UnbindAxis(string axisName, Action<float> handler);


    bool TryGetGamepadInSlot(int slot, out IGamepad? gamepad);
    void ConnectGamepad(int slot, IGamepad gamepad);
    void DisconnectGamepad(int slot);
}