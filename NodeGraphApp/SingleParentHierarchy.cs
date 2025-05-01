using System.Collections;

public interface INode<T> where T : class, INode<T>
{
    T? Parent { get; set; }
    SingleParentHierarchy<T> Children { get; }
}

public sealed class SingleParentHierarchy<T> : IEnumerable<T> where T : class, INode<T>
{
    private readonly T _parent;
    private readonly List<T> _children = new();

    public SingleParentHierarchy(T parent)
    {
        _parent = parent;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T child)
    {
        DetachFromOldParent(child);
        child.Parent = _parent;
        _children.Add(child);
    }

    public void Clear()
    {
        foreach (var child in _children)
            child.Parent = null;
        _children.Clear();
    }

    public bool Contains(T item)
    {
        return _children.Contains(item);
    }
    
    public bool Remove(T child)
    {
        var removed = _children.Remove(child);
        if (removed)
            child.Parent = null;
        return removed;
    }

    public int Count => _children.Count;
    public bool IsReadOnly => false;
    public int IndexOf(T child)
    {
        return _children.IndexOf(child);
    }

    public void Insert(int index, T child)
    {
        DetachFromOldParent(child);
        child.Parent = _parent;
        _children.Insert(index, child);
    }

    public void RemoveAt(int index)
    {
        if (index >= 0 && index < _children.Count)
        {
            var child = _children[index];
            child.Parent = null;
        }
        _children.RemoveAt(index);
    }

    public T this[int index]
    {
        get => _children[index];
        set
        {
            if (index >= 0 && index < _children.Count)
            {
                var child = _children[index];
                child.Parent = null;
            }

            DetachFromOldParent(value);
            _children[index] = value;
            value.Parent = _parent;
        }
    }

    private void DetachFromOldParent(T child)
    {
        if (child.Parent != null && child.Parent != _parent)
            child.Parent.Children.Remove(child);
    }
}