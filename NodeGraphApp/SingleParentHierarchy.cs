using System.Collections;

namespace NodeGraphApp;

public interface INode<T> where T : class, INode<T>
{
    SingleParentHierarchy<T> Hierarchy { get; }
}

public sealed class SingleParentHierarchy<T> : IEnumerable<T> where T : class, INode<T>
{
    public T? Parent { get; private set; }
    public IEnumerable<T> Children => _children;

    private readonly T _self;
    private readonly List<T> _children = new();

    public SingleParentHierarchy(T self)
    {
        _self = self;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void AddChild(T child)
    {
        DetachFromOldParent(child);
        child.Hierarchy.Parent = _self;
        _children.Add(child);
    }

    public void RemoveAllChildren()
    {
        foreach (var child in _children)
            child.Hierarchy.Parent = null;
        _children.Clear();
    }

    public bool Contains(T item)
    {
        return _children.Contains(item);
    }
    
    public bool RemoveChild(T child)
    {
        var removed = _children.Remove(child);
        if (removed)
            child.Hierarchy.Parent = null;
        return removed;
    }

    public int ChildrenCount => _children.Count;
    public bool IsReadOnly => false;
    public int IndexOf(T child)
    {
        return _children.IndexOf(child);
    }

    public void InsertChild(int index, T child)
    {
        DetachFromOldParent(child);
        child.Hierarchy.Parent = _self;
        _children.Insert(index, child);
    }

    public void RemoveAt(int index)
    {
        if (index >= 0 && index < _children.Count)
        {
            var child = _children[index];
            child.Hierarchy.Parent = null;
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
                child.Hierarchy.Parent = null;
            }

            DetachFromOldParent(value);
            _children[index] = value;
            value.Hierarchy.Parent = _self;
        }
    }

    private void DetachFromOldParent(T child)
    {
        if (child.Hierarchy.Parent != null && child.Hierarchy.Parent != _self)
            child.Hierarchy.Parent.Hierarchy.RemoveChild(child);
    }
}