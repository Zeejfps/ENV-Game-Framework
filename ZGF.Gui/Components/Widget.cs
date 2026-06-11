using ZGF.Gui.Bindings;

namespace ZGF.Gui.Components;

/// <summary>
/// Shared per-view props every primitive forwards onto the built <see cref="View"/>.
/// </summary>
public abstract record Widget : IWidget
{
    public StyleValue<float> Width { get; init; }
    public StyleValue<float> Height { get; init; }
    public StyleValue<float> MinWidth { get; init; }
    public StyleValue<float> MinHeight { get; init; }
    public string? Id { get; init; }

    /// <summary>Auto-tracked visibility binding (e.g. <c>() =&gt; vm.Items.Count == 0</c>).</summary>
    public Func<bool>? BindVisible { get; init; }

    public View BuildView(Context ctx)
    {
        var v = CreateView(ctx);
        if (Width.IsSet) v.Width = Width;
        if (Height.IsSet) v.Height = Height;
        if (MinWidth.IsSet) v.MinWidthConstraint = MinWidth;
        if (MinHeight.IsSet) v.MinHeightConstraint = MinHeight;
        if (Id != null) v.Id = Id;
        if (BindVisible != null) v.BindIsVisible(BindVisible);
        return v;
    }

    protected abstract View CreateView(Context ctx);
}