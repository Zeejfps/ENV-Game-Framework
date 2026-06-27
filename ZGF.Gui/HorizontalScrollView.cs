using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui;

/// <summary>
/// A single-child horizontal scroll viewport. Lays its content out at the wider of the viewport
/// and the content's natural width, and at the viewport's full height, then offsets it by the
/// current scroll distance and clips to the viewport. Content that fits stays put; wider content
/// scrolls. Reports zero intrinsic width so it never forces an ancestor as wide as its content —
/// that is what lets the surrounding chrome shrink past the content and engage scrolling. No
/// scrollbar; drive it with a wheel/keyboard controller.
/// </summary>
public sealed class HorizontalScrollView : View
{
    private float _distanceFromLeft;
    private float _maxDistanceFromLeft;

    public override bool ClipsContent => true;

    public HorizontalScrollView(View content)
    {
        AddChildToSelf(content);
    }

    protected override float MeasureWidthIntrinsic() => 0f;

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var naturalWidth = child.MeasureWidth();
        var contentWidth = Math.Max(position.Width, naturalWidth);
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

    public void ScrollHorizontal(float delta)
    {
        var clamped = Math.Clamp(_distanceFromLeft + delta, 0f, _maxDistanceFromLeft);
        if (Math.Abs(clamped - _distanceFromLeft) < 0.0001f) return;
        _distanceFromLeft = clamped;
        SetDirty();
    }
}
