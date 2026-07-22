using System.Collections;

namespace ZGF.Observable;

/// <summary>
/// A mutable list that fires fine-grained <see cref="ListChange{T}"/> events synchronously.
/// Single-threaded — mutate from the UI thread only.
/// </summary>
public sealed class ObservableList<T> : IReadOnlyList<T>, IInvalidatable
{
    private readonly List<T> _items = new();
    private Action<ListChange<T>>? _changed;
    private Action? _invalidated;

    public int Count
    {
        get
        {
            DependencyTracker.Register(this);
            return _items.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            DependencyTracker.Register(this);
            return _items[index];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        DependencyTracker.Register(this);
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int IndexOf(T item)
    {
        DependencyTracker.Register(this);
        return _items.IndexOf(item);
    }

    public void Add(T item)
    {
        var index = _items.Count;
        _items.Add(item);
        Fire(ListChange<T>.Added(index, item));
    }

    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        Fire(ListChange<T>.Added(index, item));
    }

    public bool Remove(T item)
    {
        var index = _items.IndexOf(item);
        if (index < 0) return false;
        _items.RemoveAt(index);
        Fire(ListChange<T>.Removed(index, item));
        return true;
    }

    public void RemoveAt(int index)
    {
        var item = _items[index];
        _items.RemoveAt(index);
        Fire(ListChange<T>.Removed(index, item));
    }

    public void Move(int from, int to)
    {
        if (from == to) return;
        var item = _items[from];
        _items.RemoveAt(from);
        _items.Insert(to, item);
        Fire(ListChange<T>.Moved(from, to, item));
    }

    public void Replace(int index, T item)
    {
        var old = _items[index];
        if (EqualityComparer<T>.Default.Equals(old, item)) return;
        _items[index] = item;
        Fire(ListChange<T>.Replaced(index, old, item));
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        Fire(ListChange<T>.Cleared());
    }

    public event Action<ListChange<T>> Changed
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
    /// Subscribes to list changes. Fires immediately with a <see cref="ListChangeKind.Reset"/>
    /// event so the consumer can seed itself from the current contents, then on every
    /// subsequent mutation.
    /// </summary>
    public IDisposable Subscribe(Action<ListChange<T>> handler)
    {
        handler(ListChange<T>.Reset());
        _changed += handler;
        return new Subscription(() => _changed -= handler);
    }

    private void Fire(ListChange<T> change)
    {
        _invalidated?.Invoke();
        _changed?.Invoke(change);
    }
}
