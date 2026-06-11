using ZGF.Gui.Views;

namespace ZGF.Gui.Components;

/// <summary>Flexible empty space — <c>new Spacer()</c> pushes siblings apart.</summary>
public sealed record Spacer : IWidget
{
    public View BuildView(Context ctx) => new FlexItem { Grow = 1f, Child = new RectView() };
}