using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Wraps a child in a <see cref="FlexItem"/> so it grows along the parent flex axis.
/// </summary>
public sealed record Grow : Widget
{
    public required IWidget Child { get; init; }

    /// <summary>Flex grow weight. A constant by default; bind it to animate a proportional fill
    /// (e.g. a depleting progress bar) without needing pixel widths.</summary>
    public Prop<float> Factor { get; init; } = 1f;

    protected override View CreateView(Context ctx)
    {
        var v = new FlexItem { Child = Child.BuildView(ctx) };
        Factor.Apply(ctx, v, static (x, f) => x.Grow = f);
        return v;
    }
}