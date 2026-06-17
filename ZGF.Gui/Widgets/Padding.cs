using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Insets its children by a <see cref="PaddingStyle"/> without painting anything.
/// Use this for pure spacing; reach for <see cref="Box"/> only when you also need to
/// paint a background or border. Builds a <see cref="PaddingView"/>.
/// </summary>
public sealed record Padding : Widget
{
    public Prop<PaddingStyle> Amount { get; init; }
    public IWidget[] Children { get; init; } = [];

    protected override View CreateView(Context ctx)
    {
        var v = new PaddingView();
        Amount.Apply(ctx, v, static (x, p) => x.Padding = p);
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}
