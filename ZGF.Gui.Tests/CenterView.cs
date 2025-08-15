using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class CenterView : View
{
    protected override void OnLayoutChild(in RectF position, View child)
    {
        var childSize = child.MeasureSelf();
        var xOffset = (position.Width + childSize.Width) * 0.5f;
        var yOffset = (position.Height + childSize.Height) * 0.5f;
        child.LeftConstraint = xOffset;
        child.BottomConstraint = yOffset;
        child.LayoutSelf();
    }
}