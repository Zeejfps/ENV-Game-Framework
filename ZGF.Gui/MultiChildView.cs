using System.Collections;

namespace ZGF.Gui;

public class MultiChildView : View
{
    public virtual ChildrenCollection Children { get; }

    public MultiChildView()
    {
        Children = new ChildrenCollection(this);
    }

    /// <summary>
    /// A view's children. A concrete collection with a by-value <see cref="Enumerator"/> — the
    /// same shape as <see cref="List{T}"/> — so <c>foreach (var child in Children)</c> allocates
    /// nothing. It implements only the non-generic <see cref="IEnumerable"/>, which is all that
    /// collection-initializer syntax (<c>Children = { a, b }</c>) needs; there is deliberately no
    /// <c>IEnumerable&lt;View&gt;</c> LINQ surface that would box an enumerator.
    /// </summary>
    public sealed class ChildrenCollection(MultiChildView view) : IEnumerable
    {
        public int Count => view._children.Count;

        public View this[int index] => view._children[index];

        public Enumerator GetEnumerator() => new(view._children);

        IEnumerator IEnumerable.GetEnumerator() => view._children.GetEnumerator();

        public void Add(View child) => view.AddChildToSelf(child);

        public void Insert(int index, View child) => view.InsertChildToSelf(index, child);

        public void Move(View child, int newIndex) => view.MoveChildToSelf(child, newIndex);

        public bool Remove(View child) => view.RemoveChildFromSelf(child);

        public bool Contains(View child) => view._children.Contains(child);

        public void Clear()
        {
            foreach (var child in view._children.ToArray())
                Remove(child);
        }

        public struct Enumerator(List<View> children)
        {
            private List<View>.Enumerator _inner = children.GetEnumerator();

            public View Current => _inner.Current;

            public bool MoveNext() => _inner.MoveNext();
        }
    }
}
