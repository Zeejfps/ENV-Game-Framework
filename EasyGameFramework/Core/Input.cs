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
    public IInputBindings? Bindings { get; set; }

    private ILogger Logger { get; }
    private IEventBus EventBus { get; }
    private Dictionary<string, HashSet<Action>> ActionToHandlerMap { get; } = new();

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
        if (Bindings == null)
            return;

        EventBus.Publish(evt);

        var button = evt.Button;
        if (Bindings.OverrideMouseButtonBindings.TryGetValue(button, out var action))
            OnActionPerformed(action!);
        else if (Bindings.DefaultMouseButtonBindings.TryGetValue(button, out action))
            OnActionPerformed(action!);
    }

    private void OnKeyboardKeyPressed(in KeyboardKeyPressedEvent evt)
    {
        if (Bindings == null)
            return;

        EventBus.Publish(evt);
        
        Logger.Trace($"Key pressed: {evt.Key}, {Bindings}");
        var key = evt.Key;
        if (Bindings.OverrideKeyboardKeyBindings.TryGetValue(key, out var action))
            OnActionPerformed(action!);
        else if (Bindings.DefaultKeyboardKeyBindings.TryGetValue(key, out action))
            OnActionPerformed(action!);
    }

    private void OnActionPerformed(string action)
    {
        Logger.Trace($"Action performed: {action}");
        if (!ActionToHandlerMap.TryGetValue(action, out var handlers))
            return;
        
        foreach (var handler in handlers)
            handler.Invoke();
    }

    public void Update()
    {
        Keyboard.Reset();
        Mouse.Reset();
    }

    public void BindAction(string actionName, Action handler)
    {
        if (!ActionToHandlerMap.TryGetValue(actionName, out var handlers))
        {
            handlers = new HashSet<Action>();
            ActionToHandlerMap[actionName] = handlers;
        }
        handlers.Add(handler);
    }

    public void UnbindAction(string actionName, Action handler)
    {
        if (ActionToHandlerMap.TryGetValue(actionName, out var handlers))
            handlers.Remove(handler);
    }

    public void BindAxis(string axisName, Action<float> handler)
    {
    }

    public void UnbindAxis(string axisName, Action<float> handler)
    {
    }

    public bool TryGetGamepadInSlot(int slot, out IGenericGamepad? gamepad)
    {
        return m_Gamepads.TryGetValue(slot, out gamepad);
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

    private void OnGamepadButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        if (Bindings == null)
            return;
        
        var gamepad = evt.Gamepad;
        var button = evt.Button;

        if (Bindings.TryResolveBinding(gamepad, button, out var action))
            OnActionPerformed(action!);
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