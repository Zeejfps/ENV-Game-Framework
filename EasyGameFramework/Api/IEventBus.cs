namespace EasyGameFramework.Api;

public interface IEventBus
{
    void Publish<TEvent>() where TEvent : unmanaged;
    void Publish<TEvent>(in TEvent evt) where TEvent : unmanaged;
    
    void AddListener<TEvent>(Action<TEvent> listener) where TEvent : unmanaged;
    void RemoveListener<TEvent>(Action<TEvent> listener) where TEvent : unmanaged;
}