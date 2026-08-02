using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui;

/// <summary>
/// A single-child horizontal scroll viewport. Lays its content out at the wider of the viewport
/// and the content's natural width, and at the viewport's full height, then offsets it by the
/// current scroll distance and clips to the viewport. Content that fits stays put; wider content
/// scrolls. Reports zero intrinsic width so it never forces an ancestor as wide as its content —
/// that is what lets the surrounding chrome shrink past the content and engage scrolling. Its
/// measured height is the content's height at that same laid-out width, so height-for-width
/// content reserves exactly the height it will draw rather than the height it would wrap to. No
/// scrollbar; drive it with a wheel/keyboard controller.
/// </summary>
public sealed class HorizontalScrollView : View
{
    private readonly View _content;
    private float _distanceFromLeft;
    private float _maxDistanceFromLeft;

    public override bool ClipsContent => true;

    public HorizontalScrollView(View content)
    {
        _content = content;
        AddChildToSelf(content);
    }

    protected override float MeasureWidthIntrinsic() => 0f;

    // Measured at the same width layout will grant, not at the viewport: height-for-width content
    // (wrapping text) would otherwise reserve room for lines it never draws once it scrolls.
    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        if (Height.IsSet)
            return Height;

        return _content.MeasureHeight(ContentWidth(availableWidth));
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var contentWidth = ContentWidth(position.Width);
        _maxDistanceFromLeft = contentWidth - position.Width;
        _distanceFromLeft = Math.Clamp(_distanceFromLeft, 0f, _maxDistanceFromLeft);

        child.LeftConstraint = IsRtl
            ? position.Right - contentWidth + _distanceFromLeft
            : position.Left - _distanceFromLeft;
        child.WidthConstraint = contentWidth;
        child.BottomConstraint = position.Bottom;
        child.HeightConstraint = position.Height;
        child.LayoutSelf();
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }

    /// <summary>Scrolls by <paramref name="delta"/>, clamped to the scrollable range. Returns whether
    /// the offset actually moved — false when the content fits or is already pinned against that
    /// edge, which is what lets a wheel controller decline the event and leave it to an ancestor.</summary>
    public bool ScrollHorizontal(float delta)
    {
        var clamped = Math.Clamp(_distanceFromLeft + delta, 0f, _maxDistanceFromLeft);
        if (Math.Abs(clamped - _distanceFromLeft) < 0.0001f) return false;
        _distanceFromLeft = clamped;
        SetDirty();
        return true;
    }

    private float ContentWidth(float viewportWidth) =>
        Math.Max(viewportWidth, _content.MeasureWidth());
}
