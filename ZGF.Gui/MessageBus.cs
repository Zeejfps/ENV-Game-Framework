namespace ZGF.Gui;

public sealed class MessageBus
{
    public static MessageBus Instance { get; } = new();

    public event Action<Type> ListenerAdded; 
    
    private readonly Dictionary<Type, HashSet<object>> _listenersByMessageType = new();

    public IEnumerable<IListener<TMessage>> GetListeners<TMessage>()
    {
        var messageType = typeof(TMessage);
        
        if (!_listenersByMessageType.TryGetValue(messageType, out var listeners))
            yield break;
        
        foreach (var listener in listeners)
        {
            if (listener is Listener<TMessage> castedListener)
                yield return castedListener;
        }
    }
    
    public void RegisterCallback<TMessage>(Component component, Action<TMessage> callback)
    {
        var messageType = typeof(TMessage);
        
        if (!_listenersByMessageType.TryGetValue(messageType, out var listeners))
        {
            listeners = new HashSet<object>();
            _listenersByMessageType.Add(messageType, listeners);
        }
        
        listeners.Add(new Listener<TMessage>(component, callback));
    }

    public void Update()
    {
        
    }
}

public interface IListener<in TMessage>
{
    Component Component { get; }
    
    void HandleMessage(TMessage message);
}

public sealed class Listener<TMessage>(Component component, Action<TMessage> callback) : IListener<TMessage>
{
    public Component Component { get; } = component;

    public void HandleMessage(TMessage message)
    {
        callback.Invoke(message);
    }
}

public sealed class MouseEnterEvent
{
    
}