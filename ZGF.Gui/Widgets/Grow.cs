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
        // A Grow both fills slack and yields on overflow (shrink weight tracks the grow factor), so a
        // growing item never overflows its container — the long-standing behavior before shrink split
        // out as its own weight. Use the Shrink widget for an item that yields without filling.
        var v = new FlexItem { Child = Child.BuildView(ctx) };
        Factor.Apply(ctx, v, static (x, f) =>
        {
            x.Grow = f;
            x.Shrink = f;
        });
        return v;
    }
}