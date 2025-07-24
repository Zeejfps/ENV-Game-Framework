using System.Collections;
using System.Diagnostics;
using ZGF.Geometry;

namespace ZGF.Gui;

public class View
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
                if (prevContext != null)
                {
                    OnDetachedFromContext(prevContext);
                    if (Controller != null)
                    {
                        Controller.OnDisabled(prevContext);
                    }
                }
                
                if (_context != null)
                {
                    OnAttachedToContext(_context);
                    if (Controller != null)
                    {
                        Controller.OnEnabled(_context);
                    }
                }
                
                OnApplyContextToChildren(_context);
            }
        }
    }

    protected virtual void OnAttachedToContext(Context context)
    {
   
    }

    protected virtual void OnDetachedFromContext(Context context)
    {
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
    
    public View? Parent { get; private set; }

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

    private int Depth
    {
        get => _depth;
        set
        {
            if (_depth == value)
                return;

            _depth = value;
            foreach (var child in _children)
            {
                child.Depth = _depth + 1;
            }
        }
    }
    
    private int _siblingIndex;
    public int SiblingIndex => _siblingIndex;

    private bool IsDirty => IsSelfDirty || IsChildrenDirty;
    private bool IsSelfDirty { get; set; } = true;
    private bool IsChildrenDirty => _children.Any(child => child.IsDirty);

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
    
    public virtual IComponentCollection Children { get; }
    public IStyleClassCollection StyleClasses { get; } 

    private readonly List<View> _children = new();
    private readonly HashSet<string> _styleClasses = new();

    private IController? _controller;
    public IController? Controller
    {
        get => _controller;
        set
        {
            var prevController = _controller;
            _controller = value;

            if (Context != null)
            {
                if (prevController != null)
                {
                    prevController.OnDisabled(Context);
                }

                if (_controller != null)
                {
                    _controller.OnEnabled(Context);
                }
            }
        }
    }

    public View()
    {
        Children = new ComponentCollection(this);
        StyleClasses = new StyleClassCollection(this);
    }

    protected void AddStyleClass(string classId)
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

    protected bool RemoveStyleClass(string classId)
    {
        if (_styleClasses.Remove(classId))
        {
            SetDirty();
            return true;
        }

        return false;
    }
    
    protected void AddChildToSelf(View view)
    {
        if (view.Parent != null)
        {
            view.Parent.RemoveChildFromSelf(view);
        }

        // Order matters here
        var siblingIndex = _children.Count;
        _children.Add(view);

        view.Parent = this;
        view.Depth = Depth + 1;
        view._siblingIndex =  siblingIndex;
        view.StyleSheet = StyleSheet;
        view.Context = Context;
        OnComponentAdded(view);
    }
    
    protected bool RemoveChildFromSelf(View view)
    {
        if (_children.Remove(view))
        {
            view.Context = null;
            view.Parent = null;
            view.Depth = 0;
            view._siblingIndex = 0;
            view.StyleSheet = null;
            OnComponentRemoved(view);
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
        if (c.TryGetClip(out var clipRect))
        {
            if (!clipRect.Intersects(Position))
            {
                return;
            }
        }
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

    protected virtual void OnComponentAdded(View view)
    {
        SetDirty();
    }

    protected virtual void OnComponentRemoved(View view)
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
            OnLayoutChild(position, child);
        }
    }

    protected virtual void OnLayoutChild(in RectF position, View child)
    {
        child.LeftConstraint = position.Left;
        child.BottomConstraint = position.Bottom;
        child.MinWidthConstraint = position.Width;
        child.MaxWidthConstraint = position.Width;
        child.MaxHeightConstraint = position.Height;
        child.LayoutSelf();
    }

    public void ApplyStyle(Style style)
    {
        OnApplyStyle(style);
    }

    protected virtual void OnStyleSheetApplied(StyleSheet styleSheet)
    {
        foreach (var styleClass in _styleClasses)
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

    protected void DrawChild(View child, ICanvas c)
    {
        child.DrawSelf(c);
    }

    protected virtual void OnDrawChildren(ICanvas c)
    {
        foreach (var component in _children)
        {
            component.DrawSelf(c);
        }
    }

    public void BringToFront(View view)
    {
        // If its already the last child ignore it
        if (view.SiblingIndex == _children.Count - 1)
            return;

        if (view.SiblingIndex >= _children.Count)
            return;

        if (_children[view.SiblingIndex] != view)
            return;

        var lastChild = _children[^1];
        lastChild._siblingIndex = view.SiblingIndex;
        _children[view.SiblingIndex] = lastChild;

        view._siblingIndex = _children.Count - 1;
        _children[^1] = view;
    }

    public bool IsAncestorOf(View target)
    {
        var parent = target;
        while (parent != null)
        {
            if (this == parent)
                return true;
            parent = parent.Parent;
        }
        return false;
    }
    
    public bool IsInFrontOf(View view)
    {
        // Use clearer variable names for readability   
        var nodeA = this;
        var nodeB = view;
        var iNodeA = nodeA;
        var iNodeB = nodeB;

        // --- Pre-checks for simple cases ---

        // If nodeB is an ancestor of nodeA, then nodeA comes "after" nodeB.
        if (nodeB.IsAncestorOf(nodeA))
            return true;

        // If nodeA is an ancestor of nodeB, then nodeA comes "before" nodeB.
        // This case was missing and is important for correctness.
        if (nodeA.IsAncestorOf(nodeB))
            return false;

        // --- Main Logic to find common ancestor ---

        // Step 1: Ascend the deeper node until both are at the same depth.
        // The key fix here is checking if the node becomes null after assignment.
        while (nodeA._depth > nodeB._depth)
        {
            nodeA = nodeA.Parent;
            // If we've reached the top of nodeA's tree, they can't be related.
            if (nodeA == null) return false;
        }

        while (nodeB._depth > nodeA._depth)
        {
            nodeB = nodeB.Parent;
            // If we've reached the top of nodeB's tree, they can't be related.
            if (nodeB == null) return false;
        }

        // At this point, nodeA and nodeB are at the same depth.
        // If they are the same, one was an ancestor of the other, which IsAncestorOf should have caught.
        // We can return here for safety, though this line might be redundant if IsAncestorOf is perfect.
        if (nodeA == nodeB) return false;


        // Step 2: Ascend both nodes together until we find their common parent.
        // The loop must terminate if we reach the top of either tree.
        while (nodeA.Parent != nodeB.Parent)
        {
            nodeA = nodeA.Parent;
            nodeB = nodeB.Parent;

            // CRITICAL FIX: If either node is null, it means we reached the root of
            // two separate trees. They don't have a common parent, so they are unrelated.
            if (nodeA == null || nodeB == null)
            {
                Console.WriteLine($"No common parent between: {iNodeA} and {iNodeB}");
                return false;
            }
        }

        // At this point, nodeA and nodeB are guaranteed to be siblings with a shared, non-null parent.
        // We can now safely compare their sibling index to determine their order.
        return nodeA._siblingIndex > nodeB._siblingIndex;
    }

    public override string ToString()
    {
        return base.ToString() + "-" + _depth;
    }

    private sealed class ComponentCollection : IComponentCollection
    {
        private readonly View _view;

        public ComponentCollection(View view)
        {
            _view = view;
        }

        public IEnumerator<View> GetEnumerator()
        {
            return _view._children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _view._children.Count;
        
        public void Add(View view)
        {
            _view.AddChildToSelf(view);
        }

        public bool Remove(View view)
        {
            return _view.RemoveChildFromSelf(view);
        }

        public bool Contains(View view)
        {
            return _view._children.Contains(view);
        }
    }
    
    private sealed class StyleClassCollection : IStyleClassCollection
    {
        private readonly View _view;
        public StyleClassCollection(View view)
        {
            _view = view;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _view._styleClasses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _view._styleClasses.Count;
        
        public void Add(string styleClass)
        {
            _view.AddStyleClass(styleClass);
        }

        public bool Remove(string styleClass)
        {
            return _view.RemoveStyleClass(styleClass);
        }
    }
}