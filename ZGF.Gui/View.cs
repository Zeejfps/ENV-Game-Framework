using System.Collections;
using System.ComponentModel;
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
                    // Snapshot: a behavior's DetachFromContext may add/remove behaviors via
                    // UseController/UseViewModel etc. The snapshot avoids both
                    // CollectionModified and the index-shift footgun.
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

    public StyleValue<float> PreferredWidth
    {
        get;
        set => SetField(ref field, value);
    }

    public StyleValue<float> PreferredHeight
    {
        get;
        set => SetField(ref field, value);
    }

    public View? Parent { get; private set; }

    public string? Id
    {
        get;
        set
        {
            if (SetField(ref field, value))
                ResolveAndApply();
        }
    }

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

    private int _siblingIndex;
    public int SiblingIndex => _siblingIndex;

    private bool IsDirty => IsSelfDirty || IsChildrenDirty;
    private bool IsSelfDirty { get; set; } = true;
    private bool IsChildrenDirty => _children.Any(child => child.IsDirty);

    protected StyleSheet? StyleSheet
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            // Single cascade per swap on self, then propagate to children (each child's
            // setter does its own ResolveAndApply). Previously the cleared/applied paths
            // each ran a ResolveAndApply with a transient null-sheet state in between,
            // tripling cascade cost across the tree on every theme swap.
            ResolveAndApply();
            PropagateStyleSheetToChildren();
            SetDirty();
        }
    }

    public IStyleClassCollection StyleClasses { get; }
    public IStyleModifierCollection StyleModifiers { get; }
    public IBehaviorCollection Behaviors { get; }

    /// <summary>
    /// Cascade output. Built from defaults → matching sheet rules (stable-sorted ascending by
    /// specificity) → <see cref="_localStyle"/>. Renderers read from this; do not assign to it.
    /// </summary>
    protected readonly ResolvedStyle _resolvedStyle = new();

    /// <summary>
    /// Imperative local-write style. Property setters (e.g. <c>BackgroundColor = X</c>) route
    /// here; bindings also write here. Wins over the sheet by construction.
    /// </summary>
    protected readonly Style _localStyle = new();

    protected readonly List<View> _children = new();
    protected readonly HashSet<string> _styleClasses = new();
    protected readonly HashSet<string> _styleModifiers = new();
    protected readonly List<IViewBehavior> _behaviors = new();

    public View()
    {
        StyleClasses = new StyleClassCollection(this);
        StyleModifiers = new StyleModifierCollection(this);
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
            _children[i]._siblingIndex = i;
        }

        view.Parent = this;
        view.Depth = Depth + 1;
        view.StyleSheet = StyleSheet;
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
            _children[i]._siblingIndex = i;
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

    /// <summary>
    /// Rebuild <see cref="_resolvedStyle"/> from scratch in cascade order:
    /// defaults → matching sheet rules (stable-sorted ascending by specificity)
    /// → <see cref="_localStyle"/>. Invoked whenever the inputs change (style sheet
    /// swap, classes, modifiers, local writes). After resolution, notifies subclasses
    /// via <see cref="OnStyleResolved"/>.
    /// </summary>
    protected void ResolveAndApply()
    {
        _resolvedStyle.ResetToDefaults();

        var sheet = StyleSheet;
        if (sheet != null)
        {
            foreach (var rule in sheet.RulesMatching(this))
                _resolvedStyle.Apply(rule.Style);
        }

        _resolvedStyle.Apply(_localStyle);

        OnStyleResolved(_resolvedStyle);
        SetDirty();
    }

    /// <summary>
    /// Hook for view subtypes to react to a cascade refresh. The default implementation
    /// pulls layout fields off the resolved style so framework views still respect
    /// <c>PreferredWidth</c> / <c>PreferredHeight</c> tokens. Subclasses that want
    /// behavioural reactions (e.g. invalidating text wrap on font-size change) override this.
    /// </summary>
    protected virtual void OnStyleResolved(ResolvedStyle style)
    {
        // IsSet-guarded so the cascade doesn't clobber constructor-time PreferredWidth /
        // PreferredHeight assignments on views that never participate in a sheet rule for
        // these fields. Removing a class/modifier that previously set Preferred* won't
        // reset the dimension — latent, no current rule sets these so it doesn't bite.
        if (style.PreferredWidth.IsSet)
            PreferredWidth = style.PreferredWidth;
        if (style.PreferredHeight.IsSet)
            PreferredHeight = style.PreferredHeight;
    }

    /// <summary>
    /// Marks <see cref="_localStyle"/> as having changed and re-runs the cascade. Property
    /// setters on view subtypes call this after writing to <see cref="_localStyle"/>.
    /// </summary>
    protected void MarkLocalStyleDirty() => ResolveAndApply();

    /// <summary>
    /// Propagate this view's current StyleSheet (which may be null) to every direct child.
    /// Each child's setter handles its own cascade re-run, so descendants don't pass through
    /// a transient null-sheet state when an ancestor swaps from one sheet to another.
    /// </summary>
    protected virtual void PropagateStyleSheetToChildren()
    {
        var sheet = StyleSheet;
        foreach (var child in _children)
        {
            if (sheet != null) child.ApplyStyleSheet(sheet);
            else child.ClearStyleSheet();
        }
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
    }

    protected virtual void OnLayoutSelf()
    {
        float width;
        if (PreferredWidth.IsSet)
        {
            width = PreferredWidth;
        }
        else if (WidthConstraint.IsSet)
        {
            width = WidthConstraint;
        }
        else
        {
            width = MeasureWidth();
        }

        float height;
        if (PreferredHeight.IsSet)
        {
            height = PreferredHeight;
        }
        else if (HeightConstraint.IsSet)
        {
            height = HeightConstraint;
        }
        else
        {
            // Use the same width-fallback chain as the width branch so wrapping/
            // height-for-width children get the right available width.
            float availableWidth;
            if (WidthConstraint.IsSet)
                availableWidth = WidthConstraint;
            else if (PreferredWidth.IsSet)
                availableWidth = PreferredWidth;
            else
                availableWidth = MeasureWidth();

            height = MeasureHeight(availableWidth);
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

    public virtual float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
        {
            return PreferredWidth;
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
    public virtual float MeasureHeight(float availableWidth)
    {
        if (PreferredHeight.IsSet)
        {
            return PreferredHeight;
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
        view._siblingIndex =  siblingIndex;
        view.StyleSheet = StyleSheet;
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
            view._siblingIndex = 0;
            view.StyleSheet = null;
            OnChildRemoved(view);
            return true;
        }
        return false;
    }

    public void ApplyStyleSheet(StyleSheet styleSheet)
    {
        StyleSheet = styleSheet;
    }

    public void ClearStyleSheet()
    {
        StyleSheet = null;
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

    private void AddStyleClass(string classId)
    {
        if (_styleClasses.Add(classId))
        {
            ResolveAndApply();
        }
    }

    private bool RemoveStyleClass(string classId)
    {
        if (_styleClasses.Remove(classId))
        {
            ResolveAndApply();
            return true;
        }

        return false;
    }

    private void AddStyleModifier(string modifier)
    {
        if (_styleModifiers.Add(modifier))
        {
            ResolveAndApply();
        }
    }

    private bool RemoveStyleModifier(string modifier)
    {
        if (_styleModifiers.Remove(modifier))
        {
            ResolveAndApply();
            return true;
        }

        return false;
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

    private sealed class StyleClassCollection(View view) : IStyleClassCollection
    {
        public IEnumerator<string> GetEnumerator()
        {
            return view._styleClasses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => view._styleClasses.Count;

        public bool Contains(string styleClass) => view._styleClasses.Contains(styleClass);

        public void Add(string styleClass)
        {
            view.AddStyleClass(styleClass);
        }

        public bool Remove(string styleClass)
        {
            return view.RemoveStyleClass(styleClass);
        }
    }

    private sealed class StyleModifierCollection(View view) : IStyleModifierCollection
    {
        public IEnumerator<string> GetEnumerator()
        {
            return view._styleModifiers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => view._styleModifiers.Count;

        public bool Contains(string modifier) => view._styleModifiers.Contains(modifier);

        public void Add(string modifier)
        {
            view.AddStyleModifier(modifier);
        }

        public bool Remove(string modifier)
        {
            return view.RemoveStyleModifier(modifier);
        }
    }
}
