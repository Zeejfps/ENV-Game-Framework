using ZGF.Observable;

namespace ZGF.Gui.Bindings;

/// <summary>
/// Binds a parent's children to a derived list defined by a compute function.
/// The compute function's observable reads are auto-tracked; when any dependency
/// invalidates, the function re-runs and the parent's children are reseeded.
/// </summary>
internal sealed class ComputedChildrenBindingBehavior<TItem, TChild> : IViewBehavior
    where TChild : View
{
    private readonly MultiChildView _parent;
    private readonly Func<IEnumerable<TItem>> _compute;
    private readonly Func<TItem, TChild> _create;

    private Derived<TItem[]>? _computed;
    private IDisposable? _subscription;
    private readonly List<TChild> _tracked = new();

    public ComputedChildrenBindingBehavior(
        MultiChildView parent,
        Func<IEnumerable<TItem>> compute,
        Func<TItem, TChild> create)
    {
        _parent = parent;
        _compute = compute;
        _create = create;
    }

    public void AttachToContext(View view, Context context)
    {
        _computed = new Derived<TItem[]>(() => _compute().ToArray());
        _subscription = _computed.Subscribe(Reseed);
    }

    public void DetachFromContext(View view, Context context)
    {
        _subscription?.Dispose();
        _subscription = null;
        _computed = null;
        Clear();
    }

    private void Reseed(TItem[] items)
    {
        Clear();
        foreach (var item in items)
        {
            var child = _create(item);
            _parent.Children.Add(child);
            _tracked.Add(child);
        }
    }

    private void Clear()
    {
        foreach (var child in _tracked)
        {
            _parent.Children.Remove(child);
        }
        _tracked.Clear();
    }
}
