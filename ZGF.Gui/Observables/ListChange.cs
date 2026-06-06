namespace ZGF.Observable;

public enum ListChangeKind
{
    /// <summary>The list's contents should be re-read from scratch. Fired once on Subscribe.</summary>
    Reset,
    Added,
    Removed,
    Replaced,
    Moved,
    Cleared,
}

/// <summary>
/// A single mutation event from an <see cref="ObservableList{T}"/>. Subscribers should
/// apply only the indicated change rather than diffing the whole list.
/// </summary>
public readonly struct ListChange<T>
{
    public ListChangeKind Kind { get; }
    public int Index { get; }
    public int NewIndex { get; }
    public T? Item { get; }
    public T? OldItem { get; }

    private ListChange(ListChangeKind kind, int index, int newIndex, T? item, T? oldItem)
    {
        Kind = kind;
        Index = index;
        NewIndex = newIndex;
        Item = item;
        OldItem = oldItem;
    }

    public static ListChange<T> Reset() => new(ListChangeKind.Reset, -1, -1, default, default);
    public static ListChange<T> Added(int index, T item) => new(ListChangeKind.Added, index, -1, item, default);
    public static ListChange<T> Removed(int index, T item) => new(ListChangeKind.Removed, index, -1, default, item);
    public static ListChange<T> Replaced(int index, T oldItem, T newItem) => new(ListChangeKind.Replaced, index, -1, newItem, oldItem);
    public static ListChange<T> Moved(int from, int to, T item) => new(ListChangeKind.Moved, from, to, item, default);
    public static ListChange<T> Cleared() => new(ListChangeKind.Cleared, -1, -1, default, default);
}
