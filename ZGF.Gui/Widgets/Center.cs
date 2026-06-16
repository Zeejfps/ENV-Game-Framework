using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>Centers its child in the available space. Builds a <see cref="CenterView"/>.</summary>
public sealed record Center : Widget
{
    public required IWidget Child { get; init; }

    protected override View CreateView(Context ctx) => new CenterView
    {
        Children = { Child.BuildView(ctx) },
    };
}
