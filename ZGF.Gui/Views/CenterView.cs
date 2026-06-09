using ZGF.Geometry;

namespace ZGF.Gui.Views;

/// <summary>
/// Centers each child within this view's bounds. Each child is measured loosely (so a child
/// smaller than the viewport keeps its natural size) and capped to the viewport less
/// <see cref="Margin"/>, so a child can never be laid out larger than the space available —
/// a centered modal never grows past the window it sits in; content that exceeds the cap must
/// scroll internally. A child's <c>Min*Constraint</c> still floors the measured size.
/// </summary>
public sealed class CenterView : LayoutView
{
    /// <summary>
    /// Gap kept between a clamped child and the viewport edge, so a child forced
    /// down to the cap doesn't sit flush against the window border.
    /// </summary>
    public float Margin
    {
        get;
        set => SetField(ref field, value);
    } = 24f;

    protected override Size MeasureContent(Constraints c)
    {
        // Report the largest child's natural size (capped to the incoming box). When the parent
        // hands down a tight constraint, the base Measure's Constrain expands this to fill.
        var loose = Constraints.Loose(c.MaxWidth, c.MaxHeight);
        var w = 0f;
        var h = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            var s = child.Measure(loose);
            if (s.Width > w) w = s.Width;
            if (s.Height > h) h = s.Height;
        }
        return new Size(w, h);
    }

    protected override void ArrangeContent(RectF bounds)
    {
        var maxWidth = Math.Max(0f, bounds.Width - Margin * 2f);
        var maxHeight = Math.Max(0f, bounds.Height - Margin * 2f);

        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;

            var minW = child.MinWidthConstraint.IsSet ? Math.Min((float)child.MinWidthConstraint, maxWidth) : 0f;
            var minH = child.MinHeightConstraint.IsSet ? Math.Min((float)child.MinHeightConstraint, maxHeight) : 0f;
            var size = child.Measure(new Constraints(minW, maxWidth, minH, maxHeight));

            var x = bounds.Left + (bounds.Width - size.Width) / 2f;
            var y = bounds.Bottom + (bounds.Height - size.Height) / 2f;
            child.Arrange(new RectF(x, y, size.Width, size.Height));
        }
    }
}
