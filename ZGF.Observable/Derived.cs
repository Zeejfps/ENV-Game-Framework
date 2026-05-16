namespace ZGF.Observable;

/// <summary>
/// A derived observable. The compute function is re-evaluated synchronously whenever any
/// source it read invalidates. Dependencies are tracked automatically — reads of
/// <see cref="State{T}"/> / <see cref="ObservableList{T}"/> inside the compute function
/// register as dependencies, no manual subscription required.
/// </summary>
public sealed class Derived<T> : IReadable<T>, IInvalidatable, IDependencyCollector, IDisposable
{
    private readonly Func<T> _compute;
    private readonly HashSet<IInvalidatable> _dependencies = new();
    private readonly List<Action> _depUnsubscribes = new();
    private T _value;
    private bool _hasComputed;
    private Action<T>? _changed;
    private Action? _invalidated;

    public Derived(Func<T> compute)
    {
        _compute = compute;
        _value = default!;
        Recompute();
    }

    public T Value
    {
        get
        {
            DependencyTracker.Register(this);
            return _value;
        }
    }

    public event Action<T> Changed
    {
        add => _changed += value;
        remove => _changed -= value;
    }

    public event Action Invalidated
    {
        add => _invalidated += value;
        remove => _invalidated -= value;
    }

    public IDisposable Subscribe(Action<T> handler)
    {
        handler(_value);
        _changed += handler;
        return new Subscription(() => _changed -= handler);
    }

    void IDependencyCollector.AddDependency(IInvalidatable source)
    {
        if (source == this) return;
        if (_dependencies.Add(source))
        {
            Action handler = Recompute;
            source.Invalidated += handler;
            _depUnsubscribes.Add(() => source.Invalidated -= handler);
        }
    }

    /// <summary>
    /// Unsubscribes from all tracked dependencies. After Dispose, the derived value is
    /// frozen at its last computed value and will not recompute. Idempotent.
    /// </summary>
    public void Dispose()
    {
        foreach (var unsub in _depUnsubscribes) unsub();
        _depUnsubscribes.Clear();
        _dependencies.Clear();
    }

    private void Recompute()
    {
        foreach (var unsub in _depUnsubscribes) unsub();
        _depUnsubscribes.Clear();
        _dependencies.Clear();

        T newValue;
        using (DependencyTracker.BeginTracking(this))
        {
            newValue = _compute();
        }

        var changed = !_hasComputed || !EqualityComparer<T>.Default.Equals(_value, newValue);
        _value = newValue;
        _hasComputed = true;

        if (changed)
        {
            _invalidated?.Invoke();
            _changed?.Invoke(newValue);
        }
    }
}
