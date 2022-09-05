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
    public IEnumerable<IGamepad> Gamepads => m_Gamepads.Values;
    public IInputBindings? Bindings { get; set; }

    private ILogger Logger { get; }
    private IEventBus EventBus { get; }
    private Dictionary<string, HashSet<Action>> ActionToHandlerMap { get; } = new();

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
        
        var key = evt.Key;
        if (Bindings.OverrideKeyboardKeyBindings.TryGetValue(key, out var action))
            OnActionPerformed(action!);
        else if (Bindings.DefaultKeyboardKeyBindings.TryGetValue(key, out action))
            OnActionPerformed(action!);
    }

    private void OnActionPerformed(string action)
    {
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

    public bool TryGetGamepadInSlot(int slot, out IGamepad? gamepad)
    {
        return m_Gamepads.TryGetValue(slot, out gamepad);
    }

    public void ConnectGamepad(int slot, IGamepad gamepad)
    {
        m_Gamepads.Add(slot, gamepad);
        GamepadConnected?.Invoke(new GamepadConnectedEvent
        {
            Gamepad = gamepad
        });
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