namespace EasyGameFramework.Api;

internal class EventBus : IEventBus
{
    public void Publish<TEvent>() where TEvent : unmanaged
    {
        Listeners<TEvent>.Get(this).Notify(new TEvent());
    }

    public void Publish<TEvent>(in TEvent evt) where TEvent : unmanaged
    {
        Listeners<TEvent>.Get(this).Notify(evt);
    }

    public void AddListener<TEvent>(Action<TEvent> listener) where TEvent : unmanaged
    {
        Listeners<TEvent>.Get(this).AddListener(listener);
    }

    public void RemoveListener<TEvent>(Action<TEvent> listener) where TEvent : unmanaged
    {
        Listeners<TEvent>.Get(this).RemoveListener(listener);
    }

    private class Listeners<TEvent>
    {
        private static readonly Dictionary<IEventBus, Listeners<TEvent>> s_BusToListenersMap = new();
        
        public static Listeners<TEvent> Get(IEventBus eventBus)
        {
            if (!s_BusToListenersMap.TryGetValue(eventBus, out var listeners))
            {
                listeners = new Listeners<TEvent>();
                s_BusToListenersMap[eventBus] = listeners;
            }
            return listeners;
        }

        private readonly List<Action<TEvent>> m_Listeners = new();

        public void Notify(in TEvent evt)
        {
            foreach (var listener in m_Listeners)
                listener.Invoke(evt);
        }
        
        public void AddListener(Action<TEvent> listener)
        {
            m_Listeners.Add(listener);
        }

        public void RemoveListener(Action<TEvent> listener)
        {
            m_Listeners.Remove(listener);
        }
    }
}