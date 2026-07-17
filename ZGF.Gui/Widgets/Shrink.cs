using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Wraps a child so it gives up main-axis space when the flex container overflows, but never grows
/// past its natural size when there's slack. The yield-only complement of <see cref="Grow"/>: use it
/// for content that should make way — e.g. an ellipsizing label sitting beside a pinned sibling that
/// must stay adjacent rather than being pushed to the far edge.
/// </summary>
public sealed record Shrink : Widget
{
    public required IWidget Child { get; init; }

    /// <summary>Flex shrink weight. Higher yields more of the overflow relative to other shrinkers.</summary>
    public Prop<float> Factor { get; init; } = 1f;

    protected override View CreateView(Context ctx)
    {
        var v = new FlexItem { Child = Child.BuildView(ctx) };
        Factor.Apply(ctx, v, static (x, f) => x.Shrink = f);
        return v;
    }
}
