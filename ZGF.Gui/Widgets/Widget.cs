using ZGF.Gui.Bindings;

namespace ZGF.Gui.Widgets;

/// <summary>
/// The single authoring base for widgets. Override exactly one of the two seams:
/// <see cref="Build"/> to compose (return other widgets), or <see cref="CreateView"/> to
/// construct (build and wire real views). Shared per-view props (size, id, visibility
/// binding) are forwarded onto the built <see cref="View"/> either way.
/// </summary>
public abstract record Widget : IWidget
{
    public Prop<float> Width { get; init; }
    public Prop<float> Height { get; init; }
    public Prop<float> MinWidth { get; init; }
    public Prop<float> MinHeight { get; init; }

    /// <summary>Upper bounds on the measured size. A view that would measure larger is capped to these;
    /// content that no longer fits must scroll or clip (e.g. a <c>ScrollArea</c> given a
    /// <see cref="MaxHeight"/> so it has a bounded viewport). Unset leaves the size unbounded.</summary>
    public Prop<float> MaxWidth { get; init; }
    public Prop<float> MaxHeight { get; init; }
    public string? Id { get; init; }

    /// <summary>Accessibility metadata to overlay onto the built view. A widget may set an intrinsic
    /// role/label in <see cref="CreateView"/>; what's set here wins per-field (see
    /// <see cref="AccessibilityInfo.Overlay"/>), so an author can override just the label.</summary>
    public AccessibilityInfo Accessibility { get; init; }

    /// <summary>Visibility: a constant, an observable (<c>vm.IsOpen</c>), a projection
    /// (<c>vm.Count.Bind(c =&gt; c == 0)</c>), or a compute (<c>Prop.Bind(() =&gt; …)</c>). Unset leaves
    /// the view visible.</summary>
    public Prop<bool> Visible { get; init; }

    /// <summary>Render-only opacity (0..1) for this view and its descendants. Unset leaves it fully
    /// opaque (no cost). Bind it to an animation for a fade.</summary>
    public Prop<float> Opacity { get; init; }

    /// <summary>Render-only draw offsets (logical points) for this view and its descendants — visual
    /// only, never layout. Unset = no offset. Bind them to an animation for a slide.</summary>
    public Prop<float> TranslationX { get; init; }
    public Prop<float> TranslationY { get; init; }

    /// <summary>Render-only scale factors about the view's center for this view and its descendants —
    /// visual only, never layout. Unset = unscaled (1). Bind them to an animation for a pop/zoom.</summary>
    public Prop<float> ScaleX { get; init; }
    public Prop<float> ScaleY { get; init; }

    /// <summary>Stacking order relative to siblings — accumulates down the tree, higher draws on top.
    /// Unset leaves it at 0. Use to lift an overlay layer above normal content.</summary>
    public Prop<int> ZIndex { get; init; }

    public View BuildView(Context ctx)
    {
        var v = CreateView(ctx);
        Width.Apply(ctx, v,static (x, w) => x.Width = w);
        Height.Apply(ctx, v,static (x, h) => x.Height = h);
        MinWidth.Apply(ctx, v,static (x, w) => x.MinWidthConstraint = w);
        MinHeight.Apply(ctx, v,static (x, h) => x.MinHeightConstraint = h);
        MaxWidth.Apply(ctx, v,static (x, w) => x.MaxWidthConstraint = w);
        MaxHeight.Apply(ctx, v,static (x, h) => x.MaxHeightConstraint = h);
        Visible.Apply(ctx, v,static (x, vis) => x.IsVisible = vis);
        Opacity.Apply(ctx, v,static (x, o) => x.Opacity = o);
        TranslationX.Apply(ctx, v,static (x, t) => x.TranslationX = t);
        TranslationY.Apply(ctx, v,static (x, t) => x.TranslationY = t);
        ScaleX.Apply(ctx, v,static (x, s) => x.ScaleX = s);
        ScaleY.Apply(ctx, v,static (x, s) => x.ScaleY = s);
        ZIndex.Apply(ctx, v,static (x, z) => x.ZIndex = z);
        if (Id != null) v.Id = Id;
        if (!Accessibility.IsEmpty) v.Accessibility = v.Accessibility.Overlay(Accessibility);
        return v;
    }

    /// <summary>Compose: resolve dependencies and return other widgets.</summary>
    protected virtual IWidget Build(Context ctx) =>
        throw new InvalidOperationException($"{GetType().Name} must override Build or CreateView.");

    /// <summary>Construct: build and wire real views. Defaults to recursing through <see cref="Build"/>.</summary>
    protected virtual View CreateView(Context ctx) => Build(ctx).BuildView(ctx);
}

/// <summary>
/// A <see cref="Widget"/> that owns a long-lived mutable <typeparamref name="TState"/> — interaction
/// state, a view model, anything that must outlive the authoring record. <see cref="CreateState"/>
/// builds it once, <see cref="Build"/> composes the view tree with that state in hand, and the state
/// is exposed via <see cref="State"/> so a parent combinator (e.g. <c>WithController</c>) can reach
/// the same instance the visuals bind to.
/// <para>
/// This is the retained-mode analogue of Flutter's <c>StatefulWidget</c>/<c>State</c> split, with one
/// deliberate difference: <see cref="Build"/> runs <b>once</b>, not per frame — state changes
/// propagate through reactive bindings, not by re-running build. So the state is created once for the
/// view's whole life; it is not torn down and recreated across an unmount/remount cycle. (Reopening a
/// dialog builds a fresh widget, hence a fresh state; that is a new build, not a remount.)
/// </para>
/// </summary>
public abstract record Widget<TState> : Widget, IWidget<TState> where TState : class
{
    private TState? _state;

    /// <summary>The state for this build. Throws until <see cref="CreateView"/> has run, since
    /// <see cref="CreateState"/> needs the build context and so cannot run at construction.</summary>
    public TState State
    {
        get
        {
            return _state ?? throw new InvalidOperationException(
                $"{GetType().Name}.State is not available until the widget has been built.");
        }
        set
        {
            _state = value;
        }
    }

    /// <summary>Builds the state object once, before the view tree. <paramref name="ctx"/> resolves
    /// any services or authoring props (e.g. <c>SomeProp.ToReadable(ctx)</c>) the state needs.</summary>
    protected virtual TState CreateState(Context ctx)
    {
        return _state ?? ctx.Require<TState>()
    }

    /// <summary>Composes the view tree against <paramref name="state"/> — bind visuals to its
    /// observables, etc. Replaces <see cref="Widget.Build(Context)"/>.</summary>
    protected abstract IWidget Build(Context ctx, TState state);

    protected sealed override View CreateView(Context ctx)
    {
        var state = _state = CreateState(ctx);
        var view = Build(ctx, state).BuildView(ctx);
        if (state is IDisposable disposable)
            view.Use(() => disposable);
        return view;
    }
}
