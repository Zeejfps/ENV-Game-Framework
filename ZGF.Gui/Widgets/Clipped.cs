using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>Clips its child to the allotted bounds (e.g. long labels in fixed rows). Builds a <see cref="ClippingView"/>.</summary>
public sealed record Clipped : Widget
{
    public required IWidget Child { get; init; }

    protected override View CreateView(Context ctx) =>
        new ClippingView { Children = { Child.BuildView(ctx) } };
}
