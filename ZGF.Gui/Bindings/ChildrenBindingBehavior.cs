using ZGF.Observable;

namespace ZGF.Gui.Bindings;

internal sealed class ChildrenBindingBehavior<TItem, TChild> : IViewBehavior
    where TChild : View
{
    private readonly View.ChildrenCollection _children;
    private readonly ObservableList<TItem> _source;
    private readonly Func<TItem, TChild> _create;
    private readonly Action<TChild, TItem>? _onCreated;
    private readonly Action<TChild>? _onRemoved;

    private IDisposable? _subscription;
    private readonly List<TChild> _tracked = new();

    public ChildrenBindingBehavior(
        View.ChildrenCollection children,
        ObservableList<TItem> source,
        Func<TItem, TChild> create,
        Action<TChild, TItem>? onCreated,
        Action<TChild>? onRemoved)
    {
        _children = children;
        _source = source;
        _create = create;
        _onCreated = onCreated;
        _onRemoved = onRemoved;
    }

    public void Attach(View view)
    {
        _subscription = _source.Subscribe(OnChange);
    }

    public void Detach(View view)
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void OnChange(ListChange<TItem> change)
    {
        switch (change.Kind)
        {
            case ListChangeKind.Reset:
                Reseed();
                break;

            case ListChangeKind.Added:
            {
                var child = _create(change.Item!);
                _children.Insert(change.Index, child);
                _tracked.Insert(change.Index, child);
                _onCreated?.Invoke(child, change.Item!);
                break;
            }

            case ListChangeKind.Removed:
            {
                var child = _tracked[change.Index];
                _children.Remove(child);
                _tracked.RemoveAt(change.Index);
                _onRemoved?.Invoke(child);
                break;
            }

            case ListChangeKind.Replaced:
            {
                var oldChild = _tracked[change.Index];
                _children.Remove(oldChild);
                _onRemoved?.Invoke(oldChild);

                var newChild = _create(change.Item!);
                _children.Insert(change.Index, newChild);
                _tracked[change.Index] = newChild;
                _onCreated?.Invoke(newChild, change.Item!);
                break;
            }

            case ListChangeKind.Moved:
            {
                var child = _tracked[change.Index];
                _tracked.RemoveAt(change.Index);
                _tracked.Insert(change.NewIndex, child);
                _children.Move(child, change.NewIndex);
                break;
            }

            case ListChangeKind.Cleared:
                ClearTracked();
                break;
        }
    }

    private void Reseed()
    {
        ClearTracked();
        for (var i = 0; i < _source.Count; i++)
        {
            var item = _source[i];
            var child = _create(item);
            _children.Add(child);
            _tracked.Add(child);
            _onCreated?.Invoke(child, item);
        }
    }

    private void ClearTracked()
    {
        foreach (var child in _tracked)
        {
            _children.Remove(child);
            _onRemoved?.Invoke(child);
        }
        _tracked.Clear();
    }
}
