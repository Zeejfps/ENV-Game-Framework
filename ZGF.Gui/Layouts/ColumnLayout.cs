namespace ZGF.Gui.Layouts;

public sealed class ColumnLayout : Component
{
    protected override void OnLayoutChildren()
    {
        var position = Position;
        var components = Children;
        var componentCount = components.Count;
        if (componentCount == 0)
        {
            return;
        }

        var componentHeight = position.Height / componentCount;
        var bottom = position.Top - componentHeight;
        foreach (var component in components)
        {
            component.LeftConstraint = position.Left;
            component.MinWidthConstraint = position.Width;
            component.MaxWidthConstraint = position.Width;
            component.BottomConstraint = bottom;
            component.MaxHeightConstraint = componentHeight;
            component.LayoutSelf();
            bottom -= componentHeight;
        }
    }
}