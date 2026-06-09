using ZGF.Geometry;

namespace ZGF.Gui;

/// <summary>
/// Base for containers ported to the W1 box-constraints protocol: subclasses implement
/// <see cref="MeasureContent"/> and <see cref="ArrangeContent"/>.
///
/// The sealed legacy overrides below are a transition bridge: they let a not-yet-ported
/// legacy parent (e.g. a GitBench custom container that still positions children via the
/// constraint fields + <c>LayoutSelf</c>) drive this container through the new protocol.
/// Removed in W1.6 once the legacy path is deleted.
/// </summary>
public abstract class LayoutView : MultiChildView
{
    protected abstract override Size MeasureContent(Constraints c);
    protected abstract override void ArrangeContent(RectF bounds);

    public sealed override float MeasureWidth() =>
        Measure(Constraints.Unbounded).Width;

    public sealed override float MeasureHeight(float availableWidth)
    {
        var maxW = availableWidth > 0f ? availableWidth : float.PositiveInfinity;
        return Measure(new Constraints(0, maxW, 0, float.PositiveInfinity)).Height;
    }

    // Legacy LayoutSelf resolves Position from the constraint fields via base.OnLayoutSelf,
    // then calls OnLayoutChildren — which we route into the new arrange.
    protected sealed override void OnLayoutChildren() => ArrangeContent(Position);
}
