using System.Collections;

namespace ZGF.Gui;

public class MultiChildView : View
{
    public virtual IComponentCollection Children { get; }

    public MultiChildView()
    {
        Children = new ComponentCollection(this);
    }

    private sealed class ComponentCollection(MultiChildView view) : IComponentCollection
    {
        public IEnumerator<View> GetEnumerator()
        {
            return view._children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => view._children.Count;
        
        public void Add(View view1)
        {
            view.AddChildToSelf(view1);
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