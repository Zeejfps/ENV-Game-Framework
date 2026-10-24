namespace ZGF.Gui.Immediate.Widgets;

/// <summary>How a press or a key changes a <see cref="MultiSelection{TKey}"/>: to that row alone,
/// that row in or out, or every row from the anchor to it.</summary>
public enum SelectionChange
{
    Replace,
    Toggle,
    Extend,
}

/// <summary>
/// The rows of a <see cref="TableFrame{TKey}"/> picked out, owned by the app and held by key, so
/// they follow their rows when rows move: any number of keys, the current one that keyboard moves
/// start from, and the anchor a range runs from. Orders are the rows as shown, first to last.
/// </summary>
public sealed class MultiSelection<TKey> where TKey : notnull
{
    private readonly HashSet<TKey> _selected = new();
    private TKey? _current;
    private bool _hasCurrent;
    private TKey? _anchor;
    private bool _hasAnchor;
    private bool _reveal;

    public int Count => _selected.Count;

    public bool Contains(TKey key) => _selected.Contains(key);

    /// <summary>Whether <paramref name="key"/> is the only selected row.</summary>
    public bool IsOnly(TKey key) => _selected.Count == 1 && _selected.Contains(key);

    /// <summary>Where the current row is in <paramref name="order"/>, or -1.</summary>
    public int IndexOfCurrent(IReadOnlyList<TKey> order)
    {
        if (!_hasCurrent)
            return -1;
        return IndexOf(order, _current!);
    }

    /// <summary>The selected keys in <paramref name="order"/>'s order.</summary>
    public List<TKey> InOrder(IReadOnlyList<TKey> order)
    {
        return order
            .Where(Contains)
            .ToList();
    }

    public void Select(TKey key, SelectionChange change, IReadOnlyList<TKey> order)
    {
        switch (change)
        {
            case SelectionChange.Toggle:
                Toggle(key);
                break;
            case SelectionChange.Extend:
                ExtendTo(key, order);
                break;
            default:
                SelectOnly(key);
                break;
        }
    }

    private void SelectOnly(TKey key)
    {
        _selected.Clear();
        _selected.Add(key);
        SetCurrent(key);
        SetAnchor(key);
    }

    private void Toggle(TKey key)
    {
        if (!_selected.Remove(key))
            _selected.Add(key);
        SetCurrent(key);
        SetAnchor(key);
    }

    /// <summary>Selects the rows from the anchor to <paramref name="key"/> and nothing else; without
    /// an anchor in <paramref name="order"/>, <paramref name="key"/> alone.</summary>
    private void ExtendTo(TKey key, IReadOnlyList<TKey> order)
    {
        var from = _hasAnchor ? IndexOf(order, _anchor!) : -1;
        var to = IndexOf(order, key);
        if (from < 0 || to < 0)
        {
            SelectOnly(key);
            return;
        }
        _selected.Clear();
        for (var i = Math.Min(from, to); i <= Math.Max(from, to); i++)
            _selected.Add(order[i]);
        SetCurrent(key);
    }

    /// <summary>Moves the current row <paramref name="by"/> rows, kept within
    /// <paramref name="order"/>, selecting it alone or, with <paramref name="extend"/>, the range to
    /// it. With no current row, a move selects the first.</summary>
    public void Move(int by, bool extend, IReadOnlyList<TKey> order)
    {
        if (order.Count == 0) return;
        var index = _hasCurrent ? IndexOf(order, _current!) : -1;
        var target = index < 0 ? 0 : index + by;
        MoveTo(target, extend, order);
    }

    /// <summary>Makes row <paramref name="index"/>, kept within <paramref name="order"/>, the current
    /// one, as <see cref="Move"/> does, and asks a scrolling list to show it.</summary>
    public void MoveTo(int index, bool extend, IReadOnlyList<TKey> order)
    {
        if (order.Count == 0) return;
        var key = order[Math.Clamp(index, 0, order.Count - 1)];
        var change = extend ? SelectionChange.Extend : SelectionChange.Replace;
        Select(key, change, order);
        _reveal = true;
    }

    public void SelectAll(IReadOnlyList<TKey> order)
    {
        _selected.Clear();
        foreach (var key in order)
            _selected.Add(key);
        if (!_hasCurrent && order.Count > 0)
            SetCurrent(order[0]);
    }

    /// <summary>Selects row <paramref name="index"/> alone, or the last when past it, or nothing when
    /// <paramref name="order"/> is empty: where the selection goes on after its rows were removed.</summary>
    public void SelectAt(int index, IReadOnlyList<TKey> order)
    {
        if (order.Count == 0)
        {
            Clear();
            return;
        }
        SelectOnly(order[Math.Clamp(index, 0, order.Count - 1)]);
    }

    /// <summary>Forgets the keys <paramref name="order"/> no longer has, the current row and the
    /// anchor among them.</summary>
    public void Reconcile(IReadOnlyList<TKey> order)
    {
        var present = new HashSet<TKey>(order);
        _selected.RemoveWhere(key => !present.Contains(key));
        if (_hasCurrent && !present.Contains(_current!))
            _hasCurrent = false;
        if (_hasAnchor && !present.Contains(_anchor!))
            _hasAnchor = false;
    }

    public void Clear()
    {
        _selected.Clear();
        _hasCurrent = false;
        _hasAnchor = false;
        _reveal = false;
    }

    internal bool TakeReveal()
    {
        var reveal = _reveal;
        _reveal = false;
        return reveal;
    }

    private void SetCurrent(TKey key)
    {
        _current = key;
        _hasCurrent = true;
    }

    private void SetAnchor(TKey key)
    {
        _anchor = key;
        _hasAnchor = true;
    }

    private static int IndexOf(IReadOnlyList<TKey> order, TKey key)
    {
        var comparer = EqualityComparer<TKey>.Default;
        for (var i = 0; i < order.Count; i++)
        {
            if (comparer.Equals(order[i], key))
                return i;
        }
        return -1;
    }
}
