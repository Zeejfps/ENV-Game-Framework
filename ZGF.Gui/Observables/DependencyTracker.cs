namespace ZGF.Observable;

internal interface IDependencyCollector
{
    void AddDependency(IInvalidatable source);
}

/// <summary>
/// Thread-static stack of active dependency collectors. <see cref="State{T}"/> and
/// <see cref="ObservableList{T}"/> register themselves on read; <see cref="Derived{T}"/>
/// pushes itself before re-evaluating and collects the resulting dependency set.
/// </summary>
internal static class DependencyTracker
{
    [ThreadStatic] private static Stack<IDependencyCollector>? _stack;

    public static TrackingScope BeginTracking(IDependencyCollector collector)
    {
        _stack ??= new Stack<IDependencyCollector>();
        _stack.Push(collector);
        return new TrackingScope(_stack);
    }

    public static void Register(IInvalidatable source)
    {
        if (_stack is { Count: > 0 })
        {
            _stack.Peek().AddDependency(source);
        }
    }

    public readonly struct TrackingScope : IDisposable
    {
        private readonly Stack<IDependencyCollector> _stack;
        internal TrackingScope(Stack<IDependencyCollector> stack) { _stack = stack; }
        public void Dispose() => _stack.Pop();
    }
}
