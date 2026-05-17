namespace ZGF.Observable;

/// <summary>
/// A mutable scalar observable. Writes fire subscribers synchronously. Single-threaded
/// — mutate from the UI thread only. If you need background-thread writers later, add
/// a marshaling layer rather than making this thread-safe.
/// </summary>
public sealed class State<T> : IReadable<T>, IInvalidatable
{
    private T _value;
    private Action<T>? _changed;
    private Action? _invalidated;

    public State() : this(default!) { }

    public State(T initial)
    {
        _value = initial;
    }

    public T Value
    {
        get
        {
            DependencyTracker.Register(this);
            return _value;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            _invalidated?.Invoke();
            _changed?.Invoke(value);
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

    /// <summary>
    /// Subscribes to value changes. Fires immediately with the current value, then on
    /// every subsequent change. Dispose the returned token to unsubscribe.
    /// </summary>
    public IDisposable Subscribe(Action<T> handler)
    {
        handler(_value);
        _changed += handler;
        return new Subscription(() => _changed -= handler);
    }
    
    public void Set(T value) => Value = value;
    
    public static implicit operator State<T> (T value) => new(value);
    public static implicit operator T(State<T> value) => value.Value;

}
