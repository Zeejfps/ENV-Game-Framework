using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>Flexible empty space — <c>new Spacer()</c> pushes siblings apart.</summary>
public sealed record Spacer : Widget
{
    protected override View CreateView(Context ctx) => new FlexItem { Grow = 1f, Child = new RectView() };
}