using ZGF.Gui.Bindings;
using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Column : FlexBase
{
    protected override Axis Axis => Axis.Vertical;
}

/// <summary>
/// The data-driven counterpart of <see cref="Column"/>: its rows mirror a reactive list,
/// re-seeding whenever <see cref="Items"/> changes and rendering each element through
/// <see cref="Template"/>.
/// </summary>
public sealed record Column<T> : FlexBase
{
    public required Prop<IReadOnlyList<T>> Items { get; init; }
    public required Func<T, IWidget> Template { get; init; }

    protected override Axis Axis => Axis.Vertical;

    protected override View CreateView(Context ctx)
    {
        var v = (FlexView)base.CreateView(ctx);
        v.Children.BindChildren(Items.AsCompute(ctx), item => Template(item).BuildView(ctx));
        return v;
    }
}
