namespace EasyGameFramework.Api;

public delegate void EventListener<TEvent>(in TEvent evt) where TEvent : struct;

public interface IEventBus
{
    void Publish<TEvent>() where TEvent : struct;
    void Publish<TEvent>(in TEvent evt) where TEvent : struct;
    
    void AddListener<TEvent>(EventListener<TEvent> listener) where TEvent : struct;
    void RemoveListener<TEvent>(EventListener<TEvent> listener) where TEvent : struct;
}