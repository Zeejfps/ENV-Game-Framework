using System.Collections;

namespace ZGF.Gui;

// Implements only the non-generic IEnumerable — enough for collection-initializer syntax
// (`Children = { a, b }`, which the compiler gates on IEnumerable but fills via Add) while
// deliberately withholding IEnumerable<View>. Without the generic interface there is no LINQ
// surface to silently box an enumerator; iterate with foreach, which binds to the struct
// ChildEnumerator below.
public interface IChildrenCollection : IEnumerable
{
    int Count { get; }
    View this[int index] { get; }
    void Add(View view);
    void Insert(int index, View view);
    void Move(View view, int newIndex);
    bool Remove(View view);
    bool Contains(View view);
    void Clear();

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
