using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ZGF.Gui;

public sealed class Context : IDisposable
{
    public ICanvas Canvas { get; set; } = null!;

    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<Context, object>> _factories = new();
    private readonly List<Type> _hosted = new();
    private readonly List<IDisposable> _owned = new();
    private readonly HashSet<Type> _creating = new();
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
    /// Registers <paramref name="service"/> under a runtime <see cref="Type"/> — for keys known only
    /// at runtime, such as a built view's concrete type, so a controller resolved from this context
    /// can take that view by its exact type.
    /// </summary>
    public void AddService(Type type, object service)
    {
        _services[type] = service;
    }

    /// <summary>
    /// Registers <typeparamref name="T"/> to be constructed lazily on first resolution, with its
    /// constructor parameters injected from this context.
    /// </summary>
    public void AddSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class =>
        AddSingleton(ctx => (T)ctx.CreateInstance(typeof(T)));

    /// <summary>Registers <typeparamref name="TImpl"/> as the lazily-constructed implementation of <typeparamref name="TService"/>.</summary>
    public void AddSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>()
        where TService : class
        where TImpl : class, TService =>
        AddSingleton<TService>(ctx => (TImpl)ctx.CreateInstance(typeof(TImpl)));

    /// <summary>Registers a factory invoked once, on first resolution of <typeparamref name="TService"/>.</summary>
    public void AddSingleton<TService>(Func<Context, TService> factory)
        where TService : class
    {
        _factories[typeof(TService)] = factory;
    }

    /// <summary>
    /// Registers <typeparamref name="T"/> as a hosted service: a singleton the host resolves and
    /// <see cref="IHostedService.Start"/>s once the app is built (see <see cref="StartHostedServices"/>).
    /// Because it starts after Build, its constructor can inject framework services that only exist
    /// then (dispatcher, clipboard, ...). Mirrors ASP.NET Core's <c>AddHostedService</c>.
    /// </summary>
    public void AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IHostedService
    {
        AddSingleton<T>();
        _hosted.Add(typeof(T));
    }

    /// <summary>
    /// Registers <typeparamref name="TImpl"/> as the hosted-service implementation of
    /// <typeparamref name="TService"/>: consumers resolve the interface, the host starts the one instance.
    /// </summary>
    public void AddHostedService<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>()
        where TService : class
        where TImpl : class, TService, IHostedService
    {
        AddSingleton<TService, TImpl>();
        _hosted.Add(typeof(TService));
    }

    /// <summary>
    /// Registers a hosted service built by <paramref name="factory"/> — for services whose
    /// constructor dependencies can't be resolved by plain injection (an interface cast, a
    /// post-construction back-wire). The host resolves and <see cref="IHostedService.Start"/>s it
    /// like any other hosted service.
    /// </summary>
    public void AddHostedService<TService>(Func<Context, TService> factory)
        where TService : class, IHostedService
    {
        AddSingleton(factory);
        _hosted.Add(typeof(TService));
    }

    /// <summary>
    /// Resolves and <see cref="IHostedService.Start"/>s every registered hosted service, in
    /// registration order. Called once by the host after the app is built.
    /// </summary>
    public void StartHostedServices()
    {
        foreach (var type in _hosted)
            ((IHostedService)Resolve(type)!).Start();
        _hosted.Clear();
    }

    /// <summary>
    /// Resolves <typeparamref name="T"/>: a registered instance or singleton wins (searching up the
    /// parent chain); otherwise, if <typeparamref name="T"/> is a constructible class, a new
    /// transient instance is built with constructor parameters injected from this context. The
    /// caller owns transient instances — they are not cached or disposed by the context. Returns
    /// null only when <typeparamref name="T"/> is neither registered nor constructible.
    /// </summary>
    public T? Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class
    {
        var resolved = Resolve(typeof(T));
        if (resolved is T service)
            return service;
        if (IsConstructible(typeof(T)))
            return (T)CreateInstance(typeof(T));
        return null;
    }

    public T Require<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class =>
        Get<T>() ?? throw new InvalidOperationException(
            $"{typeof(T).Name} is not registered in Context and is not a constructible class.");

    private static bool IsConstructible(Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0;

    private object CreateInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
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
            var parameter = parameters[i];
            var svc = Resolve(parameter.ParameterType);
            if (svc is null)
            {
                if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                    continue;
                }
                throw new InvalidOperationException(
                    $"Cannot construct {type.Name}: parameter '{parameter.Name}' " +
                    $"of type {parameter.ParameterType.Name} not registered in Context.");
            }
            args[i] = svc;
        }

        return ctor.Invoke(args);
    }

    private object? Resolve(Type type)
    {
        if (_services.TryGetValue(type, out var service))
            return service;

        if (_factories.TryGetValue(type, out var factory))
        {
            if (!_creating.Add(type))
                throw new InvalidOperationException(
                    $"Circular dependency detected while creating {type.Name}.");
            try
            {
                var created = factory(this);
                _services[type] = created;
                if (created is IDisposable disposable)
                    _owned.Add(disposable);
                return created;
            }
            finally
            {
                _creating.Remove(type);
            }
        }

        return _parent?.Resolve(type);
    }

    /// <summary>Disposes singletons this context created, in reverse creation order. Instances
    /// added via <see cref="AddService{T}"/> are owned by the caller and left alone.</summary>
    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--)
            _owned[i].Dispose();
        _owned.Clear();
    }
}
