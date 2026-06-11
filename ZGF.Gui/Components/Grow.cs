using ZGF.Gui.Views;

namespace ZGF.Gui.Components;

/// <summary>
/// Wraps a child in a <see cref="FlexItem"/> so it grows along the parent flex axis.
/// The child must build to a <see cref="MultiChildView"/> (FlexItem's requirement).
/// </summary>
public sealed record Grow : IWidget
{
    public required IWidget Child { get; init; }
    public float Factor { get; init; } = 1f;

    public View BuildView(Context ctx) => new FlexItem
    {
        Grow = Factor,
        Child = (MultiChildView)Child.BuildView(ctx),
    };
}