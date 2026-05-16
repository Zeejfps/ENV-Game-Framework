namespace ZGF.Observable;

/// <summary>
/// Thread-safe queue-based dispatcher. <see cref="Post"/> is safe to call from any
/// thread; <see cref="Drain"/> must be called from the UI thread (typically once per
/// frame). Actions posted while a drain is in progress run on the next drain, not the
/// current one — this keeps reentrant Post calls bounded.
/// </summary>
public sealed class QueuedUiDispatcher : IUiDispatcher
{
    private readonly object _gate = new();
    private List<Action> _queue = new();

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_gate) _queue.Add(action);
    }

    public void Drain()
    {
        List<Action> batch;
        lock (_gate)
        {
            if (_queue.Count == 0) return;
            batch = _queue;
            _queue = new List<Action>();
        }
        foreach (var action in batch) action();
    }
}
