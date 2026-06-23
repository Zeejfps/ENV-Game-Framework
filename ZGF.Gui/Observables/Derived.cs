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
    private readonly Action _onDependencyInvalidated;
    // The sources we're currently subscribed to. _next is the set collected during the latest
    // compute; the two are diffed so only genuinely added/dropped sources change subscriptions.
    private HashSet<IInvalidatable> _dependencies = new();
    private HashSet<IInvalidatable> _next = new();
    private T _value;
    private bool _hasComputed;
    private Action<T>? _changed;
    private Action? _invalidated;

    public Derived(Func<T> compute)
    {
        _compute = compute;
        _onDependencyInvalidated = Recompute;
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
        _next.Add(source);
    }

    /// <summary>
    /// Unsubscribes from all tracked upstream dependencies and clears all downstream
    /// subscribers. After Dispose, the derived value is frozen and no further notifications
    /// fire — subscribers fall silent without needing to explicitly unsubscribe. Idempotent.
    /// </summary>
    public void Dispose()
    {
        foreach (var dep in _dependencies) dep.Invalidated -= _onDependencyInvalidated;
        _dependencies.Clear();
        _next.Clear();
        _changed = null;
        _invalidated = null;
    }

    private void Recompute()
    {
        _next.Clear();
        T newValue;
        using (DependencyTracker.BeginTracking(this))
        {
            newValue = _compute();
        }

        // Reconcile subscriptions against the new dependency set instead of tearing them all
        // down and rebuilding: the set is almost always identical recompute-to-recompute, so the
        // common case is zero subscription changes and zero allocation.
        foreach (var dep in _dependencies)
            if (!_next.Contains(dep)) dep.Invalidated -= _onDependencyInvalidated;
        foreach (var dep in _next)
            if (!_dependencies.Contains(dep)) dep.Invalidated += _onDependencyInvalidated;
        (_dependencies, _next) = (_next, _dependencies);

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
