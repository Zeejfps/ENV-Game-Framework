using System.Collections;
using System.Diagnostics;
using ZGF.Geometry;

namespace ZGF.Gui;

public abstract class View
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
                    foreach (var behavior in _behaviors.ToArray())
                    {
                        behavior.DetachFromContext(this, prevContext);
                    }
                    OnDetachedFromContext(prevContext);
                }

                OnApplyContextToChildren(_context);

                if (_context != null)
                {
                    OnAttachedToContext(_context);
                    // Snapshot: a behavior's AttachToContext may add new behaviors via
                    // UseController/UseViewModel etc. AddBehaviorToSelf already attaches
                    // those immediately, so the snapshot avoids both CollectionModified
                    // and double-attach.
                    foreach (var behavior in _behaviors.ToArray())
                    {
                        behavior.AttachToContext(this, _context);
                    }
                }
            }
        }
    }
    
    public RectF Position
    {
        get;
        protected set => SetField(ref field, value);
    }

    public StyleValue<float> LeftConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> BottomConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> MinWidthConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> MinHeightConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// Upper bound on the laid-out width. Unlike <see cref="WidthConstraint"/> this caps the
    /// resolved size <em>after</em> a fixed <see cref="Width"/> or measured size is chosen, so it
    /// reins in a view that would otherwise size past its container — e.g. a fixed-width dialog
    /// frame on a window narrower than that fixed width.
    /// </summary>
    public StyleValue<float> MaxWidthConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>Upper bound on the laid-out height; the vertical counterpart to
    /// <see cref="MaxWidthConstraint"/>. A view taller than this is capped, and content that no
    /// longer fits must scroll.</summary>
    public StyleValue<float> MaxHeightConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> WidthConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> HeightConstraint
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> RightConstraint => LeftConstraint + WidthConstraint;
    public StyleValue<float> TopConstraint => BottomConstraint + HeightConstraint;

    public StyleValue<float> Width
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> Height
    {
        get;
        set => SetField(ref field, value);
    }

    public View? Parent { get; private set; }

    public string? Id { get; set; }

    public int ZIndex
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// When false, this view is skipped from layout (no size, no gap contribution in
    /// flex/column/row containers) and not drawn — equivalent to CSS <c>display: none</c>.
    /// Setting triggers a layout pass.
    /// </summary>
    public bool IsVisible
    {
        get;
        set => SetField(ref field, value);
    } = true;

    /// <summary>
    /// When true, this view clips its descendants — both visually (containers that
    /// override this should also <c>PushClip</c> in <c>OnDrawChildren</c>) and for
    /// hit-testing. The input system rejects descendant hits whose cursor point
    /// falls outside this view's <see cref="Position"/>. Used by scrollable
    /// containers so rows positioned outside the viewport don't receive mouse
    /// events through the area where they're not actually visible.
    /// </summary>
    public virtual bool ClipsContent => false;

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

    public int SiblingIndex { get; private set; }

    private bool IsSelfDirty { get; set; } = true;

    // Set when any descendant is self-dirty. Propagated up on SetDirty and cleared on layout,
    // so dirty detection is O(1) instead of re-walking the subtree on every LayoutSelf.
    private bool _childrenDirty;

    private bool _measuredWidthValid;
    private float _measuredWidth;
    private bool _measuredHeightValid;
    private float _measuredHeight;
    private float _measuredHeightAvailableWidth;

    /// <summary>
    /// Set on a window's root view; invoked when any view in this tree becomes dirty, so the
    /// window can schedule a redraw instead of repainting every frame.
    /// </summary>
    public Action? OnRedrawNeeded { get; set; }

    public IBehaviorCollection Behaviors { get; }

    protected readonly List<View> _children = new();
    protected readonly List<IViewBehavior> _behaviors = new();

    public View()
    {
        Behaviors = new BehaviorCollection(this);
    }
    
    protected virtual void OnAttachedToContext(Context context)
    {

    }

    protected virtual void OnDetachedFromContext(Context context)
    {
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
        lastChild.SiblingIndex = view.SiblingIndex;
        _children[view.SiblingIndex] = lastChild;

        view.SiblingIndex = _children.Count - 1;
        _children[^1] = view;
        SetDirty();
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
        return nodeA.SiblingIndex > nodeB.SiblingIndex;
    }
    
    protected void InsertChildToSelf(int index, View view)
    {
        if (index < 0 || index > _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (view.Parent == this)
        {
            MoveChildToSelf(view, index);
            return;
        }

        if (view.Parent != null)
        {
            view.Parent.RemoveChildFromSelf(view);
        }

        _children.Insert(index, view);

        for (var i = index; i < _children.Count; i++)
        {
            _children[i].SiblingIndex = i;
        }

        view.Parent = this;
        view.Depth = Depth + 1;
        view.Context = Context;
        OnChildAdded(view);
    }

    protected void MoveChildToSelf(View view, int newIndex)
    {
        var currentIndex = _children.IndexOf(view);
        if (currentIndex < 0)
            throw new ArgumentException("View is not a child of this view.", nameof(view));

        if (newIndex < 0 || newIndex >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(newIndex));

        if (currentIndex == newIndex)
            return;

        _children.RemoveAt(currentIndex);
        _children.Insert(newIndex, view);

        var lo = Math.Min(currentIndex, newIndex);
        var hi = Math.Max(currentIndex, newIndex);
        for (var i = lo; i <= hi; i++)
        {
            _children[i].SiblingIndex = i;
        }

        SetDirty();
    }

    public override string ToString()
    {
        return base.ToString() + "-" + _depth;
    }

    public T? GetParentOfType<T>() where T : View
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is T t)
            {
                return t;
            }
            parent = parent.Parent;
        }

        return null;
    }
    
    protected virtual void OnApplyContextToChildren(Context? context)
    {
        foreach (var component in _children)
        {
            component.Context = context;
        }
    }

    protected virtual void OnDrawSelf(ICanvas c)
    {
        
    }

    protected void DrawChild(View child, ICanvas c)
    {
        child.DrawSelf(c);
    }

    protected virtual void OnChildRemoved(View view)
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
        InvalidateMeasure();
        // A change here invalidates every ancestor's cached measure and marks the path dirty.
        // Stop at the first already-dirty ancestor: it (and everything above) was flagged when
        // it became dirty, so the chain to the root is already consistent.
        var top = this;
        for (var ancestor = Parent; ancestor != null && !ancestor._childrenDirty; ancestor = ancestor.Parent)
        {
            ancestor._childrenDirty = true;
            ancestor.InvalidateMeasure();
            top = ancestor;
        }
        // An early-out above means a dirty ancestor's own walk already reached the root and
        // notified, so only the walk that arrives at the root tells the window.
        if (top.Parent == null)
            top.OnRedrawNeeded?.Invoke();
    }

    private void InvalidateMeasure()
    {
        _measuredWidthValid = false;
        _measuredHeightValid = false;
    }

    protected virtual void OnLayoutSelf()
    {
        var width = ResolveWidth();
        var height = ResolveHeight(width);

        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = BottomConstraint,
            Width = width,
            Height = height,
        };
    }

    /// <summary>
    /// The width this view lays out at: a fixed <see cref="Width"/>, else a parent-allotted
    /// <see cref="WidthConstraint"/>, else the intrinsic <see cref="MeasureWidth"/> — then
    /// clamped to <see cref="MinWidthConstraint"/>/<see cref="MaxWidthConstraint"/>.
    /// </summary>
    protected float ResolveWidth()
    {
        float width;
        if (Width.IsSet)
            width = Width;
        else if (WidthConstraint.IsSet)
            width = WidthConstraint;
        else
            width = MeasureWidth();

        return ClampWidth(width);
    }

    /// <summary>
    /// The height this view lays out at, measured at <paramref name="availableWidth"/> so
    /// wrapping content wraps to the width it will actually occupy. Mirrors <see cref="ResolveWidth"/>.
    /// </summary>
    protected float ResolveHeight(float availableWidth)
    {
        float height;
        if (Height.IsSet)
            height = Height;
        else if (HeightConstraint.IsSet)
            height = HeightConstraint;
        else
            height = MeasureHeight(availableWidth);

        return ClampHeight(height);
    }

    public float ClampWidth(float width)
    {
        if (MinWidthConstraint.IsSet && width < MinWidthConstraint)
            width = MinWidthConstraint;
        if (MaxWidthConstraint.IsSet && width > MaxWidthConstraint)
            width = MaxWidthConstraint;
        return width;
    }

    public float ClampHeight(float height)
    {
        if (MinHeightConstraint.IsSet && height < MinHeightConstraint)
            height = MinHeightConstraint;
        if (MaxHeightConstraint.IsSet && height > MaxHeightConstraint)
            height = MaxHeightConstraint;
        return height;
    }

    protected virtual void OnLayoutChildren()
    {
        var position = Position;
        foreach (var child in _children)
        {
            OnLayoutChild(position, child);
        }
    }

    public Size MeasureSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight(width);

        return new Size
        {
            Width = width,
            Height = height
        };
    }

    public float MeasureWidth()
    {
        if (_measuredWidthValid)
            return _measuredWidth;

        _measuredWidth = MeasureWidthIntrinsic();
        _measuredWidthValid = true;
        return _measuredWidth;
    }

    protected virtual float MeasureWidthIntrinsic()
    {
        if (Width.IsSet)
        {
            return Width;
        }

        return MeasureChildrenWidth();
    }

    protected float MeasureChildrenWidth()
    {
        var maxWidth = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            var childWith = child.MeasureWidth();
            if (childWith > maxWidth)
            {
                maxWidth = childWith;
            }
        }

        return maxWidth;
    }
    
    /// <summary>
    /// Convenience overload that uses the view's intrinsic width as the available width.
    /// Equivalent to <c>MeasureHeight(MeasureWidth())</c>. Prefer the parameterized overload
    /// from layout containers — they typically know the width they'll lay out at.
    /// </summary>
    public float MeasureHeight() => MeasureHeight(MeasureWidth());

    /// <summary>
    /// Measure the height this view would occupy when given <paramref name="availableWidth"/>
    /// of horizontal space. A non-positive value (≤ 0) means "unconstrained — use the view's
    /// intrinsic width." This is the entry point for height-for-width content (wrapping text).
    /// </summary>
    public float MeasureHeight(float availableWidth)
    {
        if (_measuredHeightValid && _measuredHeightAvailableWidth == availableWidth)
            return _measuredHeight;

        _measuredHeight = MeasureHeightIntrinsic(availableWidth);
        _measuredHeightAvailableWidth = availableWidth;
        _measuredHeightValid = true;
        return _measuredHeight;
    }

    protected virtual float MeasureHeightIntrinsic(float availableWidth)
    {
        if (Height.IsSet)
        {
            return Height;
        }

        return MeasureChildrenHeight(availableWidth);
    }

    protected float MeasureChildrenHeight(float availableWidth)
    {
        var height = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            var childHeight = child.MeasureHeight(availableWidth);
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
            // Clear before laying out children: OnLayoutChildren can re-dirty this view as a side
            // effect (e.g. a ScrollPane recomputes its scale and pushes it to a sibling scroll bar
            // subtree, which propagates _childrenDirty back up through us). Clearing afterwards
            // would wipe that signal and strand the re-dirtied view; clearing first lets it survive
            // to the next frame.
            IsSelfDirty = false;
            _childrenDirty = false;
            OnLayoutSelf();
            OnLayoutChildren();
        }
        else if (_childrenDirty)
        {
            _childrenDirty = false;
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
        if (!IsVisible) return;
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

    protected virtual void OnChildAdded(View view)
    {
        SetDirty();
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
        view.SiblingIndex =  siblingIndex;
        view.Context = Context;
        OnChildAdded(view);
    }

    protected bool RemoveChildFromSelf(View view)
    {
        if (_children.Remove(view))
        {
            view.Context = null;
            view.Parent = null;
            view.Depth = 0;
            view.SiblingIndex = 0;
            OnChildRemoved(view);
            return true;
        }
        return false;
    }

    private void AddBehaviorToSelf(IViewBehavior behavior)
    {
        _behaviors.Add(behavior);
        if (_context != null)
        {
            behavior.AttachToContext(this, _context);
        }
    }

    private bool RemoveBehaviorFromSelf(IViewBehavior behavior)
    {
        if (!_behaviors.Remove(behavior))
            return false;

        if (_context != null)
        {
            behavior.DetachFromContext(this, _context);
        }
        return true;
    }

    protected virtual void OnLayoutChild(in RectF position, View child)
    {
        child.LeftConstraint = position.Left;
        child.BottomConstraint = position.Bottom;
        child.WidthConstraint = position.Width;
        child.HeightConstraint = position.Height;
        child.LayoutSelf();
    }

    protected virtual void OnDrawChildren(ICanvas c)
    {
        foreach (var component in _children)
        {
            component.DrawSelf(c);
        }
    }


    protected int GetDrawZIndex()
    {
        var parentZIndex = Parent?.GetDrawZIndex() ?? 0;
        return parentZIndex + ZIndex;
    }
        
    private sealed class BehaviorCollection(View view) : IBehaviorCollection
    {
        public IEnumerator<IViewBehavior> GetEnumerator()
        {
            return view._behaviors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => view._behaviors.Count;

        public void Add(IViewBehavior behavior)
        {
            view.AddBehaviorToSelf(behavior);
        }

        public bool Remove(IViewBehavior behavior)
        {
            return view.RemoveBehaviorFromSelf(behavior);
        }
    }
}