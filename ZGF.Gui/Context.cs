namespace ZGF.Gui;

public sealed class Context
{
    public ICanvas Canvas { get; set; } = null!;

    private readonly Dictionary<Type, object> _services = new();
    private readonly Context? _parent;

    public Context() { }

    public Context(Context parent)
    {
        _parent = parent;
        Canvas = parent.Canvas;
    }

    public void AddService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public T? Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        return _parent?.Get<T>();
    }

    public T Require<T>() where T : class =>
        Get<T>() ?? throw new InvalidOperationException(
            $"{typeof(T).Name} not registered in Context.");
}
