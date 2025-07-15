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
    
    private string? _classId;
    public string? ClassId
    {
        get => _classId;
        set => SetField(ref _classId, value);
    }

    private int _zIndex;
    public int ZIndex
    {
        get => _zIndex;
        set => SetField(ref _zIndex, value);
    }

    private bool _isDirty = true;
    public virtual bool IsDirty
    {
        get
        {
            return _children.Any(component => component.IsDirty) || _isDirty;
        }
        private set => _isDirty =  value;
    }

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

    private readonly List<Component> _children = new();

    public IReadOnlyList<Component> Children => _children;

    public void Add(Component component)
    {
        if (component.Parent != null)
        {
            component.Parent.Remove(component);
        }

        component.Parent = this;
        _children.Add(component);
        SetDirty();
    }

    public void Remove(Component component)
    {
        if (_children.Remove(component) && component.Parent == this)
        {
            component.Parent = null;
        }
    }
    
    public void LayoutSelf()
    {
        if (!IsDirty)
            return;

        Console.WriteLine($"Laying out: {GetType()}");
        OnLayoutSelf();
        IsDirty = false;
    }

    public void DrawSelf(ICanvas r)
    {
        OnDrawSelf(r);
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

    protected bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        SetDirty();
        return true;
    }

    public void SetDirty()
    {
        IsDirty = true;
    }

    protected virtual void OnLayoutSelf()
    {
        Position = Constraints;
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
        foreach (var component in _children)
        {
            component.DrawSelf(c);
        }
    }
}