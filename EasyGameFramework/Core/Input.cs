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
    private readonly Stack<IInputLayer> m_InputLayerStack = new();

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

    public void PushLayer(IInputLayer inputLayer)
    {
        if (m_InputLayerStack.Count > 0)
            m_InputLayerStack.Peek().Unbind(this);
        
        inputLayer.Bind(this);
        m_InputLayerStack.Push(inputLayer);
    }

    public void PopLayer()
    {
        var inputLayer = m_InputLayerStack.Pop();
        inputLayer.Unbind(this);
        
        if (m_InputLayerStack.Count > 0)
            m_InputLayerStack.Peek().Bind(this);
    }
}