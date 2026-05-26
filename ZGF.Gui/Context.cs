using System.Reflection;

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

    /// <summary>
    /// Alias for <see cref="AddService{T}"/>. Registers <paramref name="service"/> against the
    /// type token <typeparamref name="T"/>; later <see cref="Get{T}"/> calls return it.
    /// </summary>
    public void Set<T>(T service) where T : class => AddService(service);

    public T? Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        return _parent?.Get<T>();
    }

    public T Require<T>() where T : class =>
        Get<T>() ?? throw new InvalidOperationException(
            $"{typeof(T).Name} not registered in Context.");

    /// <summary>
    /// Constructs an instance of <typeparamref name="T"/> by resolving its constructor
    /// parameters from this context. Picks the greediest public constructor. Throws if any
    /// parameter type is not registered.
    /// </summary>
    public T Create<T>() where T : class => (T)Create(typeof(T));

    public object Create(Type type)
    {
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (ctors.Length == 0)
            throw new InvalidOperationException($"{type.Name} has no public constructor.");

        var ctor = ctors[0];
        for (var i = 1; i < ctors.Length; i++)
            if (ctors[i].GetParameters().Length > ctor.GetParameters().Length)
                ctor = ctors[i];

        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var pType = parameters[i].ParameterType;
            var svc = Resolve(pType);
            args[i] = svc ?? throw new InvalidOperationException(
                $"Cannot construct {type.Name}: parameter '{parameters[i].Name}' " +
                $"of type {pType.Name} not registered in Context.");
        }

        return ctor.Invoke(args);
    }

    private object? Resolve(Type type)
    {
        if (_services.TryGetValue(type, out var service))
            return service;
        return _parent?.Resolve(type);
    }
}
