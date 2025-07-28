namespace ZGF.Gui;

public sealed class Context
{
    public required InputSystem InputSystem { get; init; }
    public required ICanvas Canvas { get; init; }

    private readonly Dictionary<Type, object> _services = new();

    public void AddService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }
    
    public T? Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        return null;
    }
}