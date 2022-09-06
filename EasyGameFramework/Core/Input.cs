using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Input : IInput
{
    public event GamepadConnectedDelegate? GamepadConnected;
    public event GamepadDisconnectedDelegate? GamepadDisconnected;
    public event GamepadButtonStateChangedDelegate? GamepadButtonPressed;
    public event GamepadButtonStateChangedDelegate? GamepadButtonReleased;
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
    public IEnumerable<IGamepad> Gamepads => m_Gamepads.Values;

    private ILogger Logger { get; }
    private IEventBus EventBus { get; }

    private readonly Dictionary<int, IGamepad> m_Gamepads = new();

    public Input(ILogger logger, IEventBus eventBus, IMouse mouse, IKeyboard keyboard)
    {
        Logger = logger;
        EventBus = eventBus;
        Mouse = mouse;
        Keyboard = keyboard;
        
        // TODO: Maybe only subscribe when we have bindings?
        Mouse.ButtonPressed += OnMouseButtonPressed;
        Keyboard.KeyPressed += OnKeyboardKeyPressed;
    }

    private void OnMouseButtonPressed(in MouseButtonPressedEvent evt)
    {
        EventBus.Publish(evt);
    }

    private void OnKeyboardKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        EventBus.Publish(evt);
    }

    public void Update()
    {
        Mouse.Reset();
    }

    public bool TryGetGamepadInSlot(int slot, out IGamepad? gamepad)
    {
        return m_Gamepads.TryGetValue(slot, out gamepad);
    }

    private void OnGamepadButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        EventBus.Publish(evt);
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
        GamepadDisconnected?.Invoke(new GamePadDisconnectedEvent
        {
            Gamepad = gamepad
        });
    }
}