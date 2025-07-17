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
            component.BottomConstraint = bottom;
            component.HeightConstraint = componentHeight;
            component.LayoutSelf();
            bottom -= componentHeight;
        }
    }
}