namespace ZGF.Gui;

/// <summary>
/// Collects IDisposable subscriptions added during a view's attach pass and disposes them
/// all in detach. Replaces the boilerplate `_x?.Dispose(); _x = null;` sequence that view
/// classes accumulate when they subscribe to several observables and bus messages.
/// </summary>
public sealed class SubscriptionGroup : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();

    public void Add(IDisposable? subscription)
    {
        if (subscription != null)
            _subscriptions.Add(subscription);
    }

    public void Add(Action cleanup)
    {
        _subscriptions.Add(new ActionDisposable(cleanup));
    }

    private sealed class ActionDisposable : IDisposable
    {
        private Action? _cleanup;

        public ActionDisposable(Action cleanup) => _cleanup = cleanup;

        public void Dispose()
        {
            var c = _cleanup;
            _cleanup = null;
            c?.Invoke();
        }
    }

    public void Dispose()
    {
        for (var i = _subscriptions.Count - 1; i >= 0; i--)
            _subscriptions[i].Dispose();
        _subscriptions.Clear();
    }
}
