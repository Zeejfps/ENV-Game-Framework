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

    private readonly Dictionary<string, List<Action>> m_ActionToHandlerMap = new();

    public Input(ILogger logger, IEventBus eventBus)
    {
        Logger = logger;
        m_Mouse = new Mouse();
        m_Keyboard = new Keyboard(eventBus);
        
        eventBus.AddListener<ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(ActionPerformedEvent evt)
    {
        if (m_ActionToHandlerMap.TryGetValue(evt.ActionName, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler.Invoke();
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
            handlers = new List<Action>();
            m_ActionToHandlerMap[actionName] = handlers;
        }
        handlers.Add(handler);
    }

    public void UnbindAction(string actionName, Action handler)
    {
    }

    public void BindAxis(string axisName, Action<float> handler)
    {
    }

    public void UnbindAxis(string axisName, Action<float> handler)
    {
    }
}