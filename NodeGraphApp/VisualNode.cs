using System.Collections;
using System.Numerics;
using NodeGraphApp;
using TextAlignment = NodeGraphApp.TextAlignment;

public class VisualNode
{
    public Action<ScreenRect>? BoundsChanged;

    private ScreenRect _bounds;
    public ScreenRect Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds == value)
                return;
            _bounds = value;
            OnBoundsChanged();
        }
    }

    protected virtual void OnBoundsChanged()
    {
        BoundsChanged?.Invoke(_bounds);
    }

    public float Width
    {
        get => Bounds.Width;
        set => Bounds = Bounds with { Width = value };
    }
    
    public float Height
    {
        get => Bounds.Height;
        set => Bounds = Bounds with { Height = value };
    }

    public string? Text { get; set; }
    public Color Color { get; set; }
    public Color BorderColor { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
    public BorderRadiusStyle BorderRadius { get; set; }
    public VisualNode? Parent { get; private set; }
    public IList<VisualNode> Children { get; }
    public TextAlignment TextVerticalAlignment { get; set; }
    public Vector2 CenterPosition => new(Bounds.Left + Bounds.Width*0.5f, Bounds.Bottom + Bounds.Height*0.5f);

    public VisualNode()
    {
        Children = new ChildrenList(this);
    }

    private sealed class ChildrenList : IList<VisualNode>
    {
        private readonly VisualNode _parent;
        private readonly List<VisualNode> _children = new();

        public ChildrenList(VisualNode parent)
        {
            _parent = parent;
        }

        public IEnumerator<VisualNode> GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(VisualNode item)
        {
            if (item.Parent != null && item.Parent != _parent)
                item.Parent.Children.Remove(item);
            
            item.Parent = _parent;
            _children.Add(item);
        }

        public void Clear()
        {
            foreach (var child in _children)
                child.Parent = null;
            _children.Clear();
        }

        public bool Contains(VisualNode item)
        {
            return _children.Contains(item);
        }

        public void CopyTo(VisualNode[] array, int arrayIndex)
        {
            _children.CopyTo(array, arrayIndex);
        }

        public bool Remove(VisualNode child)
        {
            var removed = _children.Remove(child);
            if (removed)
                child.Parent = null;
            return removed;
        }

        public int Count => _children.Count;
        public bool IsReadOnly => false;
        public int IndexOf(VisualNode child)
        {
            return _children.IndexOf(child);
        }

        public void Insert(int index, VisualNode child)
        {
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

        public VisualNode this[int index]
        {
            get => _children[index];
            set
            {
                if (index >= 0 && index < _children.Count)
                {
                    var child = _children[index];
                    child.Parent = null;
                }
                _children[index] = value;
                value.Parent = _parent;
            }
        }
    }
}