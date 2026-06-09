using System.Collections;

namespace ZGF.Gui;

public class MultiChildView : View
{
    public virtual IChildrenCollection Children { get; }

    public MultiChildView()
    {
        Children = new ChildrenCollection(this);
    }

    private sealed class ChildrenCollection(MultiChildView view) : IChildrenCollection
    {
        public ChildEnumerator GetEnumerator()
        {
            return new ChildEnumerator(view._children);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return view._children.GetEnumerator();
        }

        public int Count => view._children.Count;

        public View this[int index] => view._children[index];

        public void Add(View view1)
        {
            view.AddChildToSelf(view1);
        }

        public void Insert(int index, View view1)
        {
            view.InsertChildToSelf(index, view1);
        }

        public void Move(View view1, int newIndex)
        {
            view.MoveChildToSelf(view1, newIndex);
        }

        public bool Remove(View view1)
        {
            return view.RemoveChildFromSelf(view1);
        }

        public bool Contains(View view1)
        {
            return view._children.Contains(view1);
        }

        public void Clear()
        {
            foreach (var child in view._children.ToArray())
            {
                Remove(child);
            }
        }
    }
}