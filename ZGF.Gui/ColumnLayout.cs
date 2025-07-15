using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class ColumnLayout : Layout
{
    protected override RectF OnDoLayout(RectF position, IReadOnlyList<Component> components)
    {
        var componentCount = components.Count;
        var componentHeight = position.Height / componentCount;
        var offset = position.Top - componentHeight;
        foreach (var component in components)
        {
            component.Position = new RectF
            {
                Left = position.Left,
                Bottom = position.Bottom + offset,
                Width = position.Width,
                Height = componentHeight,
            };
            component.LayoutSelf();

            offset -= componentHeight;
        }
        return position;
    }
}