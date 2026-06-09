using System.Collections;

namespace ZGF.Gui;

public interface IChildrenCollection : IEnumerable<View>
{
    int Count { get; }
    View this[int index] { get; }
    void Add(View view);
    void Insert(int index, View view);
    void Move(View view, int newIndex);
    bool Remove(View view);
    bool Contains(View view);
    void Clear();

    // Struct enumerator so `foreach (var child in someCollection)` allocates nothing even
    // through the interface — the C# foreach pattern binds to this in preference to the
    // boxed IEnumerator<View> from IEnumerable<View>.
    new ChildEnumerator GetEnumerator();
}

public struct ChildEnumerator
{
    private List<View>.Enumerator _inner;

    internal ChildEnumerator(List<View> children)
    {
        _inner = children.GetEnumerator();
    }

    public View Current => _inner.Current;

    public bool MoveNext() => _inner.MoveNext();
}
