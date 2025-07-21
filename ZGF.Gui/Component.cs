using System.Collections;
using System.Diagnostics;
using ZGF.Geometry;

namespace ZGF.Gui;

public class Component : IEnumerable<Component>
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

    private bool _isInteractable;
    public bool IsInteractable
    {
        get => _isInteractable;

        set
        {
            if (SetField(ref _isInteractable, value) && Context != null)
            {
                if (_isInteractable)
                {
                    Context.InputSystem.AddInteractable(this);
                }
                else
                {
                    Context.InputSystem.RemoveInteractable(this);
                }
            }
        }
    }

    public bool IsFocused
    {
        get
        {
            if (Context == null)
                return false;
            return Context.InputSystem.IsFocused(this);
        }
    }

    public bool TryFocus()
    {
        if (Context == null)
            return false;

        return Context.InputSystem.TryFocus(this);
    }

    public void StealFocus()
    {
        if (Context == null)
            return;

        Context.InputSystem.StealFocus(this);
    }

    public void Blur()
    {
        if (Context == null)
            return;

        Context.InputSystem.Blur(this);
    }

    protected virtual void OnAttachedToContext(Context context)
    {
        if (_isInteractable)
        {
            context.InputSystem.AddInteractable(this);
        }
    }

    protected virtual void OnDetachedFromContext(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
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

    private StyleValue<float> _minWidthConstraint;
    public StyleValue<float> MinWidthConstraint
    {
        get => _minWidthConstraint;
        set => SetField(ref _minWidthConstraint, value);
    }
    
    private StyleValue<float> _maxWidthConstraint;
    public StyleValue<float> MaxWidthConstraint
    {
        get => _maxWidthConstraint;
        set => SetField(ref _maxWidthConstraint, value);
    }
    
    private StyleValue<float> _maxHeightConstraint;
    public StyleValue<float> MaxHeightConstraint
    {
        get => _maxHeightConstraint;
        set => SetField(ref _maxHeightConstraint, value);
    }
    
    public StyleValue<float> RightConstraint => LeftConstraint + MaxWidthConstraint;
    public StyleValue<float> TopConstraint => BottomConstraint + MaxHeightConstraint;
    
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
        get => _zIndex;
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
    
    public Component()
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
            var styleSheet = StyleSheet;
            if (styleSheet == null)
                return;

            if (styleSheet.TryGetByClass(classId, out var classStyle))
            {
                OnApplyStyle(classStyle);
                SetDirty();
            }
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
        component.StyleSheet = StyleSheet;
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
            component.StyleSheet = null;
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
        
        var maxWidth = 0f;
        foreach (var child in _children)
        {
            var childWith = child.MeasureWidth();
            if (childWith > maxWidth)
            {
                maxWidth = childWith;
            }
        }
        
        return maxWidth;
    }
    
    public virtual float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
        {
            return PreferredHeight;
        }
        
        var height = 0f;
        foreach (var child in _children)
        {
            var childHeight = child.MeasureHeight();
            if (childHeight > height)
            {
                height = childHeight;
            }
        }
        return height;
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

    public void DrawSelf()
    {
        Debug.Assert(Context != null);
        var c = Context.Canvas;
        DrawSelf(c);
    }

    private void DrawSelf(ICanvas c)
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
        var width = MeasureWidth();
        if (MinWidthConstraint.IsSet && width < MinWidthConstraint)
        {
            width = MinWidthConstraint;
        }
        else if (MaxWidthConstraint.IsSet && width > MaxWidthConstraint)
        {
            width = MaxWidthConstraint;
        }
        
        var height = MeasureHeight();
        if (MaxHeightConstraint.IsSet)
        {
            height = MaxHeightConstraint.Value;
        }
        
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = BottomConstraint,
            Width = width,
            Height = height,
        };
    }

    protected virtual void OnLayoutChildren()
    {
        var position = Position;
        foreach (var child in _children)
        {
            child.LeftConstraint = position.Left;
            child.BottomConstraint = position.Bottom;
            child.MinWidthConstraint = position.Width;
            child.MaxWidthConstraint = position.Width;
            child.MaxHeightConstraint = position.Height;
            child.LayoutSelf();
        }
    }

    public void ApplyStyle(Style style)
    {
        OnApplyStyle(style);
    }

    protected virtual void OnStyleSheetApplied(StyleSheet styleSheet)
    {
        foreach (var styleClass in StyleClasses)
        {
            if (styleSheet.TryGetByClass(styleClass, out var classStyle))
            {
                OnApplyStyle(classStyle);
            }
        }
        
        if (styleSheet.TryGetById(Id, out var idStyle))
        {
            OnApplyStyle(idStyle);
        }
        
        ApplyStyleSheetToChildren(styleSheet);
    }

    protected virtual void OnApplyStyle(Style style)
    {
        if (style.PreferredWidth.IsSet)
        {
            PreferredWidth = style.PreferredWidth;
        }

        if (style.PreferredHeight.IsSet)
        {
            PreferredHeight = style.PreferredHeight;
        }
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

    public bool IsAncestorOf(Component target)
    {
        var current = target;
        while (current != null)
        {
            if (current == this)
                return true;
            current = current.Parent;
        }
        return false;
    }
    
    public bool IsInFrontOf(Component component)
    {
        var x = this;
        var y = component;

        if (y.IsAncestorOf(x))
            return true;

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

    protected virtual bool OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        return false;
    }

    protected virtual bool OnMouseMoved(MouseMoveEvent e) { return true; }

    public bool HandleMouseButtonEvent(in MouseButtonEvent e)
    {
        return OnMouseButtonStateChanged(e);
    }

    public void HandleMouseWheelEvent()
    {
    }

    public bool HandleMouseMoveEvent(in MouseMoveEvent e)
    {
        return OnMouseMoved(e);
    }

    protected T? Get<T>() where T : class
    {
        if (Context == null)
            return default;
        return Context.Get<T>();
    }

    public IEnumerator<Component> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void HandleFocusGained()
    {
        OnFocusGained();
    }

    public void HandleFocusLost()
    {
        OnFocusLost();
    }

    protected virtual void OnFocusGained(){}
    protected virtual void OnFocusLost(){}

    public bool HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        return OnKeyboardKeyStateChanged(e);
    }

    protected virtual bool OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
    {
        return false;
    }
}