using ZGF.Geometry;

namespace ZGF.Gui;

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
            component.Constraints = new RectF
            {
                Left = position.Left,
                Bottom = bottom,
                Width = position.Width,
                Height = componentHeight,
            };
            component.LayoutSelf();

            bottom -= componentHeight;
        }
    }
}