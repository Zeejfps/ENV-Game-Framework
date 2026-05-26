namespace ZGF.Observable;

public static class ObservableListExtensions
{
    /// <summary>
    /// Creates a derived <see cref="ObservableList{U}"/> that mirrors <paramref name="source"/>,
    /// projecting each item through <paramref name="selector"/>. Use this when a parent view
    /// model owns a list of child view models keyed off a registry / domain list — the parent
    /// gets a typed list that tracks the source 1:1, with disposal hooks for the children.
    /// <para>
    /// <paramref name="onRemove"/> is invoked when a mapped item leaves the list (Remove /
    /// Replace / Reset / Cleared) and once per remaining item when <paramref name="subscription"/>
    /// is disposed. Pass <c>v =&gt; v.Dispose()</c> when <typeparamref name="U"/> is
    /// <see cref="IDisposable"/>.
    /// </para>
    /// </summary>
    public static ObservableList<U> Map<T, U>(
        this ObservableList<T> source,
        Func<T, U> selector,
        out IDisposable subscription,
        Action<U>? onRemove = null)
    {
        var target = new ObservableList<U>();
        var sub = source.Subscribe(change =>
        {
            switch (change.Kind)
            {
                case ListChangeKind.Reset:
                    DrainAndNotify(target, onRemove);
                    foreach (var t in source) target.Add(selector(t));
                    break;
                case ListChangeKind.Added:
                    target.Insert(change.Index, selector(change.Item!));
                    break;
                case ListChangeKind.Removed:
                {
                    var removed = target[change.Index];
                    target.RemoveAt(change.Index);
                    onRemove?.Invoke(removed);
                    break;
                }
                case ListChangeKind.Moved:
                    target.Move(change.Index, change.NewIndex);
                    break;
                case ListChangeKind.Replaced:
                {
                    var old = target[change.Index];
                    target.Replace(change.Index, selector(change.Item!));
                    onRemove?.Invoke(old);
                    break;
                }
                case ListChangeKind.Cleared:
                    DrainAndNotify(target, onRemove);
                    break;
            }
        });
        subscription = new MapSubscription<U>(sub, target, onRemove);
        return target;
    }

    private static void DrainAndNotify<U>(ObservableList<U> target, Action<U>? onRemove)
    {
        if (onRemove != null)
        {
            foreach (var u in target) onRemove(u);
        }
        target.Clear();
    }

    private sealed class MapSubscription<U> : IDisposable
    {
        private readonly IDisposable _source;
        private readonly ObservableList<U> _target;
        private readonly Action<U>? _onRemove;
        private bool _disposed;

        public MapSubscription(IDisposable source, ObservableList<U> target, Action<U>? onRemove)
        {
            _source = source;
            _target = target;
            _onRemove = onRemove;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _source.Dispose();
            DrainAndNotify(_target, _onRemove);
        }
    }
}
