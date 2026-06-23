using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Sets the UI writing direction (LTR/RTL) on its child's subtree root, from where every descendant
/// inherits it via <see cref="View.IsRtl"/>. Wrap the app content in one of these, driven from the
/// locale, and the whole tree mirrors — <c>Row</c>/<c>Column</c> reflect their children,
/// <c>BorderLayout</c> swaps its edges, and custom painters read <see cref="View.IsRtl"/>.
/// </summary>
public sealed record UiDirection : Widget
{
    public required Prop<bool> Rtl { get; init; }
    public required IWidget Child { get; init; }

    protected override View CreateView(Context ctx)
    {
        var view = Child.BuildView(ctx);
        Rtl.Apply(ctx, view, static (v, rtl) => v.IsRtl = rtl);
        return view;
    }
}
