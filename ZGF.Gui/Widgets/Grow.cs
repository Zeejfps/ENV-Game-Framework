using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Wraps a child in a <see cref="FlexItem"/> so it grows along the parent flex axis.
/// </summary>
public sealed record Grow : Widget
{
    public required IWidget Child { get; init; }
    public float Factor { get; init; } = 1f;

    protected override View CreateView(Context ctx) => new FlexItem
    {
        Grow = Factor,
        Child = Child.BuildView(ctx),
    };
}