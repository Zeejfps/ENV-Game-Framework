using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Input : IInput
{
    private readonly Keyboard m_Keyboard;

    private readonly Mouse m_Mouse;

    public IMouse Mouse => m_Mouse;
    public IKeyboard Keyboard => m_Keyboard;
    private ILogger Logger { get; }

    private readonly Dictionary<string, HashSet<Action>> m_ActionToHandlerMap = new();
    private IInputBindings? m_ActiveBindings;
    
    private KeyboardKeyBindings KeyboardKeyBindings { get; }

    public Input(ILogger logger, IEventBus eventBus)
    {
        Logger = logger;
        m_Mouse = new Mouse();
        m_Keyboard = new Keyboard(eventBus);
        KeyboardKeyBindings = new KeyboardKeyBindings();
        
        eventBus.AddListener<KeyboardKeyPressedEvent>(OnKeyboardKeyPressed);
    }

    private void OnKeyboardKeyPressed(KeyboardKeyPressedEvent evt)
    {
        var key = evt.Key;
        if (KeyboardKeyBindings.TryGetAction(key, out var action))
        {
            if (m_ActionToHandlerMap.TryGetValue(action, out var handlers))
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
        m_Keyboard.Reset();
        m_Mouse.Reset();
    }

    public void BindAction(string actionName, Action handler)
    {
        if (!m_ActionToHandlerMap.TryGetValue(actionName, out var handlers))
        {
            handlers = new HashSet<Action>();
            m_ActionToHandlerMap[actionName] = handlers;
        }
        handlers.Add(handler);
    }

    public void UnbindAction(string actionName, Action handler)
    {
        if (m_ActionToHandlerMap.TryGetValue(actionName, out var handlers))
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