using ZGF.Observable;

// TItem is unconstrained (callers pick it via Column<T>), so the keying dictionary draws a
// notnull warning; null items are guarded explicitly below and simply never get pooled.
#pragma warning disable CS8714

namespace ZGF.Gui.Bindings;

/// <summary>
/// Binds a parent's children to a derived list defined by a compute function.
/// The compute function's observable reads are auto-tracked; when any dependency
/// invalidates, the function re-runs and the parent's children are reconciled against the
/// new list, reusing the child views whose items are unchanged so only the delta is built.
/// </summary>
internal sealed class DerivedChildrenBindingBehavior<TItem, TChild> : IViewBehavior
    where TChild : View
{
    private readonly View.ChildrenCollection _children;
    private readonly Func<IEnumerable<TItem>> _compute;
    private readonly Func<TItem, TChild> _create;

    private Derived<TItem[]>? _derived;
    private IDisposable? _subscription;

    // Tracked children in their current order, paired with the item that produced each — the
    // item is the reconciliation key on the next reseed. The scratch list / pool / reused set
    // are reused across reseeds so a steady-state list churn allocates only the new rows.
    private List<(TItem Item, TChild Child)> _tracked = new();
    private List<(TItem Item, TChild Child)> _scratch = new();
    private readonly Dictionary<TItem, TChild> _pool = new();
    private readonly HashSet<TChild> _reused = new();

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
        // The reconcile path assumes this binding owns the children collection (its only mutator),
        // which holds for Column<T>. If anything else has added children, fall back to a full
        // rebuild so index-based reordering can't disturb views we don't manage.
        if (_children.Count != _tracked.Count)
        {
            FullReseed(items);
            return;
        }

        _pool.Clear();
        _reused.Clear();
        _scratch.Clear();

        foreach (var (item, child) in _tracked)
            if (item is not null) _pool.TryAdd(item, child);

        foreach (var item in items)
        {
            TChild child;
            if (item is not null && _pool.Remove(item, out var existing))
            {
                child = existing;
                _reused.Add(child);
            }
            else
            {
                child = _create(item);
            }
            _scratch.Add((item, child));
        }

        foreach (var (_, child) in _tracked)
            if (!_reused.Contains(child))
                _children.Remove(child);

        // Place each child at its target index. Insert moves an existing child or mounts a new
        // one; children already in position are skipped, so an unchanged tail costs nothing.
        for (var i = 0; i < _scratch.Count; i++)
        {
            var child = _scratch[i].Child;
            if (i < _children.Count && _children[i] == child) continue;
            _children.Insert(i, child);
        }

        (_tracked, _scratch) = (_scratch, _tracked);
    }

    private void FullReseed(TItem[] items)
    {
        Clear();
        foreach (var item in items)
        {
            var child = _create(item);
            _children.Add(child);
            _tracked.Add((item, child));
        }
    }

    private void Clear()
    {
        foreach (var (_, child) in _tracked)
            _children.Remove(child);
        _tracked.Clear();
    }
}
