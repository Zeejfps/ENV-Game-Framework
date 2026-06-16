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
    public string? Id { get; init; }

    /// <summary>Visibility: a constant, an observable (<c>vm.IsOpen</c>), a projection
    /// (<c>vm.Count.Map(c =&gt; c == 0)</c>), or a compute (<c>Prop.Bind(() =&gt; …)</c>). Unset leaves
    /// the view visible.</summary>
    public Prop<bool> Visible { get; init; }

    public View BuildView(Context ctx)
    {
        var v = CreateView(ctx);
        Width.Apply(v, static (x, w) => x.Width = w);
        Height.Apply(v, static (x, h) => x.Height = h);
        MinWidth.Apply(v, static (x, w) => x.MinWidthConstraint = w);
        MinHeight.Apply(v, static (x, h) => x.MinHeightConstraint = h);
        Visible.Apply(v, static (x, vis) => x.IsVisible = vis);
        if (Id != null) v.Id = Id;
        return v;
    }

    /// <summary>Compose: resolve dependencies and return other widgets.</summary>
    protected virtual IWidget Build(Context ctx) =>
        throw new InvalidOperationException($"{GetType().Name} must override Build or CreateView.");

    /// <summary>Construct: build and wire real views. Defaults to recursing through <see cref="Build"/>.</summary>
    protected virtual View CreateView(Context ctx) => Build(ctx).BuildView(ctx);
}