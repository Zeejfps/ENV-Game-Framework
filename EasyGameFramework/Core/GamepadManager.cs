using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

public sealed class GamepadManager : IGamepadManager
{
    public event GamepadConnectedDelegate? GamepadConnected;
    public event GamepadDisconnectedDelegate? GamepadDisconnected;
    public event GamepadButtonStateChangedDelegate? GamepadButtonPressed;
    public event GamepadButtonStateChangedDelegate? GamepadButtonReleased;

    public IEnumerable<IGamepad> Gamepads => m_Gamepads.Values;
    
    private readonly Dictionary<int, IGamepad> m_Gamepads = new();

    private ILogger Logger { get; }
    
    public GamepadManager(ILogger logger)
    {
        Logger = logger;
    }
    
    public bool TryGetGamepadInSlot(int slot, out IGamepad? gamepad)
    {
        return m_Gamepads.TryGetValue(slot, out gamepad);
    }

    private void OnGamepadButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        GamepadButtonPressed?.Invoke(evt);
    }

    public void ConnectGamepad(int slot, IGamepad gamepad)
    {
        m_Gamepads.Add(slot, gamepad);
        GamepadConnected?.Invoke(new GamepadConnectedEvent
        {
            Slot = slot,
            Gamepad = gamepad
        });

        gamepad.ButtonPressed += OnGamepadButtonPressed;
    }

    public void DisconnectGamepad(int slot)
    {
        if (!m_Gamepads.TryGetValue(slot, out var gamepad))
        {
            Logger.Warn($"No gamepad connected to slot: {slot}");
            return;
        }
        
        m_Gamepads.Remove(slot);
        GamepadDisconnected?.Invoke(new GamepadDisconnectedEvent
        {
            Gamepad = gamepad
        });
    }
}