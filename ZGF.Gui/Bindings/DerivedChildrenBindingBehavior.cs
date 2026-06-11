using ZGF.Observable;

namespace ZGF.Gui.Bindings;

/// <summary>
/// Binds a parent's children to a derived list defined by a compute function.
/// The compute function's observable reads are auto-tracked; when any dependency
/// invalidates, the function re-runs and the parent's children are reseeded.
/// </summary>
internal sealed class DerivedChildrenBindingBehavior<TItem, TChild> : IViewBehavior
    where TChild : View
{
    private readonly View.ChildrenCollection _children;
    private readonly Func<IEnumerable<TItem>> _compute;
    private readonly Func<TItem, TChild> _create;

    private Derived<TItem[]>? _derived;
    private IDisposable? _subscription;
    private readonly List<TChild> _tracked = new();

    public DerivedChildrenBindingBehavior(
        View.ChildrenCollection children,
        Func<IEnumerable<TItem>> compute,
        Func<TItem, TChild> create)
    {
        _children = children;
        _compute = compute;
        _create = create;
    }

    public void Attach(View view)
    {
        _derived = new Derived<TItem[]>(() => _compute().ToArray());
        _subscription = _derived.Subscribe(Reseed);
    }

    public void Detach(View view)
    {
        _subscription?.Dispose();
        _subscription = null;
        _derived?.Dispose();
        _derived = null;
        Clear();
    }

    private void Reseed(TItem[] items)
    {
        Clear();
        foreach (var item in items)
        {
            var child = _create(item);
            _children.Add(child);
            _tracked.Add(child);
        }
    }

    private void Clear()
    {
        foreach (var child in _tracked)
        {
            _children.Remove(child);
        }
        _tracked.Clear();
    }
}
