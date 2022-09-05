using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Input : IInput
{
    public event GamepadConnectedDelegate? GamepadConnected;
    public event GamepadDisconnectedDelegate? GamepadDisconnected;
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
    public IEnumerable<IGenericGamepad> Gamepads => m_Gamepads.Values;

    private ILogger Logger { get; }
    private IEventBus EventBus { get; }

    private readonly Dictionary<int, IGenericGamepad> m_Gamepads = new();

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

    private void OnKeyboardKeyPressed(in KeyboardKeyPressedEvent evt)
    {
        EventBus.Publish(evt);
    }

    public void Update()
    {
        Keyboard.Reset();
        Mouse.Reset();
    }

    public bool TryGetGamepadInSlot(int slot, out IGenericGamepad? gamepad)
    {
        return m_Gamepads.TryGetValue(slot, out gamepad);
    }

    private void OnGamepadButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        EventBus.Publish(evt);
    }

    public void ConnectGamepad(int slot, IGenericGamepad gamepad)
    {
        m_Gamepads.Add(slot, gamepad);
        GamepadConnected?.Invoke(new GamepadConnectedEvent
        {
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