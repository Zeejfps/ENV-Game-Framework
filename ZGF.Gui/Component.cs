using ZGF.Geometry;

namespace ZGF.Gui;

public abstract class Component
{
    private RectF _position;
    public RectF Position
    {
        get => _position;
        protected set => SetField(ref _position, value);
    }

    private RectF _constraints;
    public RectF Constraints
    {
        get => _constraints;
        set => SetField(ref _constraints, value);
    }
    
    public Component? Parent { get; private set; }

    private string? _id;
    public string? Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    private int _zIndex;
    public virtual int ZIndex
    {
        get => _zIndex;
        private set => SetField(ref _zIndex, value);
    }

    private bool IsDirty => IsSelfDirty || IsChildrenDirty;
    private bool IsSelfDirty { get; set; } = true;
    private bool IsChildrenDirty => Children.Any(child => child.IsDirty);

    private StyleSheet? _styleSheet;
    protected StyleSheet? StyleSheet
    {
        get => _styleSheet;
        set
        {
            if (_styleSheet == value)
                return;
            
            var prevStyleSheet = _styleSheet;
            _styleSheet = value;
            
            if (prevStyleSheet != null)
                OnStyleSheetCleared(prevStyleSheet);
            
            if (_styleSheet != null)
                OnStyleSheetApplied(_styleSheet);
            
            SetDirty();
        }
    }
    
    public IReadOnlyList<Component> Children => _children;
    public IEnumerable<string> StyleClasses => _styleClasses;

    private readonly List<Component> _children = new();
    private readonly HashSet<string> _styleClasses = new();
    
    public void AddStyleClass(string classId)
    {
        if (_styleClasses.Add(classId))
        {
            SetDirty();
        }
    }

    public void RemoveStyleClass(string classId)
    {
        if (_styleClasses.Remove(classId))
        {
            SetDirty();
        }
    }
    
    public void Add(Component component)
    {
        if (component.Parent != null)
        {
            component.Parent.Remove(component);
        }

        component.Parent = this;
        component.ZIndex = ZIndex + 1;
        _children.Add(component);
        OnComponentAdded(component);
    }

    public void Remove(Component component)
    {
        if (_children.Remove(component) && component.Parent == this)
        {
            component.Parent = null;
            component.ZIndex = 0;
            OnComponentRemoved(component);
        }
    }
    
    public void LayoutSelf()
    {
        if (IsSelfDirty)
        {
            OnLayoutSelf();
            OnLayoutChildren();
            IsSelfDirty = false;
        }
        else if (IsChildrenDirty)
        {
            OnLayoutChildren();
        }
    }

    public void DrawSelf(ICanvas c)
    {
        OnDrawSelf(c);
        OnDrawChildren(c);
    }

    public void ApplyStyleSheet(StyleSheet styleSheet)
    {
        StyleSheet = styleSheet;
    }

    public void ClearStyleSheet()
    {
        StyleSheet = null;
    }
        
    public void AddMouseListener(IMouseListener mouseListener)
    {
        EventSystem.Instance.AddMouseListener(this, mouseListener);
    }

    protected virtual void OnComponentAdded(Component component)
    {
        SetDirty();
    }

    protected virtual void OnComponentRemoved(Component component)
    {
        SetDirty();
    }

    protected bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        SetDirty();
        return true;
    }

    protected void SetDirty()
    {
        IsSelfDirty = true;
    }

    protected virtual void OnLayoutSelf()
    {
        Position = Constraints;
    }

    protected virtual void OnLayoutChildren()
    {
        foreach (var child in _children)
        {
            child.Constraints = Position;
            child.LayoutSelf();
        }
    }

    protected virtual void OnStyleSheetApplied(StyleSheet styleSheet)
    {
        ApplyStyleSheetToChildren(styleSheet);
    }

    protected virtual void OnStyleSheetCleared(StyleSheet styleSheet)
    {
        ClearStyleSheetFromChildren(styleSheet);
    }

    protected virtual void ApplyStyleSheetToChildren(StyleSheet styleSheet)
    {
        foreach (var child in _children)
        {
            child.ApplyStyleSheet(styleSheet);
        }
    }

    protected virtual void ClearStyleSheetFromChildren(StyleSheet styleSheet)
    {
        foreach (var child in _children)
        {
            //child.ApplyStyleSheetToSelf(styleSheet);
        }
    }

    protected virtual void OnDrawSelf(ICanvas c)
    {
        
    }

    protected virtual void OnDrawChildren(ICanvas c)
    {
        foreach (var component in _children)
        {
            component.DrawSelf(c);
        }
    }
}