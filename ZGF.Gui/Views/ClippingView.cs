namespace ZGF.Gui.Views;

/// <summary>
/// Container that clips its descendants to its own bounds. <see cref="RectView"/>
/// doesn't clip — only scroll panes do — so any child that measures wider than the
/// parent draws past the edge. Wrap a subtree in this to keep it inside visually.
/// </summary>
public sealed class ClippingView : ContainerView
{
    public override bool ClipsContent => true;

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }
}
