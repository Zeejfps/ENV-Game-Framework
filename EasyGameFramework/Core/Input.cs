using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Input : IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
    private ILogger Logger { get; }

    private Dictionary<string, HashSet<Action>> ActionToHandlerMap { get; } = new();
    private IInputBindings? m_ActiveBindings;
    
    private KeyboardKeyBindings KeyboardKeyBindings { get; }

    public Input(ILogger logger, IEventBus eventBus)
    {
        Logger = logger;
        Mouse = new Mouse();
        Keyboard = new Keyboard(eventBus);
        KeyboardKeyBindings = new KeyboardKeyBindings();
        
        eventBus.AddListener<KeyboardKeyPressedEvent>(OnKeyboardKeyPressed);
    }

    private void OnKeyboardKeyPressed(KeyboardKeyPressedEvent evt)
    {
        var key = evt.Key;
        if (KeyboardKeyBindings.TryGetAction(key, out var action))
        {
            if (ActionToHandlerMap.TryGetValue(action, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler.Invoke();
                }
            }
        }
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

    public void ApplyBindings(IInputBindings inputBindings)
    {
        if (m_ActiveBindings != null)
            Unbind(m_ActiveBindings);
        
        Bind(inputBindings);
        m_ActiveBindings = inputBindings;
    }
    
    private void Bind(IInputBindings bindings)
    {
        foreach (var (key, action) in bindings.KeyboardKeyToActionBindings)
            KeyboardKeyBindings.BindKeyToAction(key, action);
    }

    private void Unbind(IInputBindings bindings)
    {
        foreach (var (key, _) in bindings.KeyboardKeyToActionBindings)
            KeyboardKeyBindings.UnbindKey(key);
    }
}