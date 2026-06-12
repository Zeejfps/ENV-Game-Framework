using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Overlays children in z-order — first is bottom, last is top; each child gets the full
/// allotted bounds. Builds a <see cref="ContainerView"/>.
/// </summary>
public sealed record Stack : Widget
{
    public IWidget[] Children { get; init; } = [];

    protected override View CreateView(Context ctx)
    {
        var v = new ContainerView();
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}
