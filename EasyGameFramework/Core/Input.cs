using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Input : IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
    public IInputBindings? Bindings { get; set; }

    private ILogger Logger { get; }
    private Dictionary<string, HashSet<Action>> ActionToHandlerMap { get; } = new();

    public Input(ILogger logger, IEventBus eventBus, IMouse mouse, IKeyboard keyboard)
    {
        Logger = logger;
        Mouse = mouse;
        Keyboard = keyboard;
        
        // TODO: Maybe only subscribe when we have bindings?
        eventBus.AddListener<KeyboardKeyPressedEvent>(OnKeyboardKeyPressed);
        eventBus.AddListener<MouseButtonPressedEvent>(OnMouseButtonPressed);
    }

    private void OnMouseButtonPressed(MouseButtonPressedEvent evt)
    {
        if (Bindings == null)
            return;
        
        var button = evt.Button;
        var mouseButtonBindings = Bindings.MouseButtonToActionBindings;
        if (mouseButtonBindings.TryGetValue(button, out var action))
            OnActionPerformed(action!);
    }

    private void OnKeyboardKeyPressed(KeyboardKeyPressedEvent evt)
    {
        if (Bindings == null)
            return;

        var key = evt.Key;
        var keyboardKeyBindings = Bindings.KeyboardKeyToActionBindings;
        if (keyboardKeyBindings.TryGetValue(key, out var action))
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
}