using System.Collections;
using ZGF.Geometry;

namespace ZGF.Gui;

public abstract class Component : IEnumerable<Component>
{
    private Context? _context;
    public Context? Context
    {
        get => _context;
        set
        {
            var prevContext = _context;
            if (SetField(ref _context, value))
            {
                if (_context != null)
                {
                    OnAttachedToContext(_context);
                }
                else if (prevContext != null)
                {
                    OnDetachedFromContext(prevContext);
                }

                OnApplyContextToChildren(_context);
            }
        }
    }

    protected virtual void OnAttachedToContext(Context context)
    {

    }

    protected virtual void OnDetachedFromContext(Context prevContext)
    {
        prevContext.MouseInputSystem.DisableHover(this);
    }

    private RectF _position;
    public RectF Position
    {
        get => _position;
        protected set => SetField(ref _position, value);
    }

    private StyleValue<float> _leftConstraint;
    public StyleValue<float> LeftConstraint
    {
        get => _leftConstraint;
        set => SetField(ref _leftConstraint, value);
    }

    private StyleValue<float> _bottomConstraint;
    public StyleValue<float> BottomConstraint
    {
        get => _bottomConstraint;
        set => SetField(ref _bottomConstraint, value);
    }

    private StyleValue<float> _widthConstraint;
    public StyleValue<float> WidthConstraint
    {
        get => _widthConstraint;
        set => SetField(ref _widthConstraint, value);
    }
    
    private StyleValue<float> _heightConstraint;
    public StyleValue<float> HeightConstraint
    {
        get => _heightConstraint;
        set => SetField(ref _heightConstraint, value);
    }
    
    public StyleValue<float> RightConstraint => LeftConstraint + WidthConstraint;
    public StyleValue<float> TopConstraint => BottomConstraint + HeightConstraint;
    
    private StyleValue<float> _preferredWidth;
    public StyleValue<float> PreferredWidth
    {
        get => _preferredWidth;
        set => SetField(ref _preferredWidth, value);
    }
    
    private StyleValue<float> _preferredHeight;
    public StyleValue<float> PreferredHeight
    {
        get => _preferredHeight;
        set => SetField(ref _preferredHeight, value);
    }
    
    public Component? Parent { get; private set; }

    private string? _id;
    public string? Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    private int _zIndex;
    public int ZIndex
    {
        get => _depth + _zIndex;
        set => SetField(ref _zIndex, value);
    }

    private int _depth;
    private int _siblingIndex;

    public int SiblingIndex => _siblingIndex;

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
    
    protected Component()
    {

    }

    protected Component(Context ctx)
    {
        Context = ctx;
    }

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

        // Order matters here
        var siblingIndex = _children.Count;
        _children.Add(component);

        component.Parent = this;
        component._depth = _depth + 1;
        component._siblingIndex =  siblingIndex;
        component.Context = Context;
        OnComponentAdded(component);
    }

    public bool Remove(Component component)
    {
        if (_children.Remove(component))
        {
            component.Context = null;
            component.Parent = null;
            component._depth = 0;
            component._siblingIndex = 0;
            OnComponentRemoved(component);
            return true;
        }
        return false;
    }

    public Size MeasureSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight();
        return new Size
        {
            Width = width,
            Height = height
        };
    }

    public virtual float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth;
        
        return WidthConstraint;
    }
    
    public virtual float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
        {
            return PreferredHeight;
        }
        
        return HeightConstraint;
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

    public void SetDirty()
    {
        IsSelfDirty = true;
    }

    protected virtual void OnLayoutSelf()
    {
        var size = MeasureSelf();
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = BottomConstraint,
            Width = size.Width,
            Height = size.Height,
        };
    }

    protected virtual void OnLayoutChildren()
    {
        var position = Position;
        foreach (var child in _children)
        {
            child.LeftConstraint = position.Left;
            child.BottomConstraint = position.Bottom;
            child.WidthConstraint = position.Width;
            child.HeightConstraint = position.Height;
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

    protected virtual void OnApplyContextToChildren(Context? context)
    {
        foreach (var component in _children)
        {
            component.Context = context;
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

    public void BringToFront(Component component)
    {
        // If its already the last child ignore it
        if (component.SiblingIndex == _children.Count - 1)
            return;

        if (component.SiblingIndex >= _children.Count)
            return;

        if (_children[component.SiblingIndex] != component)
            return;

        var lastChild = _children[^1];
        lastChild._siblingIndex = component.SiblingIndex;
        _children[component.SiblingIndex] = lastChild;

        component._siblingIndex = _children.Count - 1;
        _children[^1] = component;
    }

    public bool IsInFrontOf(Component component)
    {
        var x = this;
        var y = component;

        while (x.Parent != null && y.Parent != null)
        {
            if (x.Parent == y.Parent)
            {
                return x._siblingIndex > y._siblingIndex;
            }

            x = x.Parent;
            y = y.Parent;
        }
        return false;
    }

    public void HandleMouseEnterEvent()
    {
        OnMouseEnter();
    }

    public void HandleMouseExitEvent()
    {
        OnMouseExit();
    }

    protected virtual void OnMouseEnter(){}
    protected virtual void OnMouseExit(){}
    protected virtual void OnMouseButtonStateChanged(MouseButtonEvent e) { }
    protected virtual void OnMouseMoved(MouseMoveEvent e) { }

    public void HandleMouseButtonEvent(in MouseButtonEvent e)
    {
        OnMouseButtonStateChanged(e);
    }

    public void HandleMouseWheelEvent()
    {
    }

    public void HandleMouseMoveEvent(in MouseMoveEvent e)
    {
        OnMouseMoved(e);
    }

    public IEnumerator<Component> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}