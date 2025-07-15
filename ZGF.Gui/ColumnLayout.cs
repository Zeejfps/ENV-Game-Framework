using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class ColumnLayout : MultiChildComponent
{
    protected override void OnLayoutSelf()
    {
        var position = Position;
        var components = Children;
        var componentCount = components.Count;
        Console.WriteLine(Position);
        if (componentCount == 0)
        {
            return;
        }

        var componentHeight = position.Height / componentCount;
        Console.WriteLine($"Component height: {componentHeight}");
        var bottom = position.Top - componentHeight;
        Console.WriteLine($"Top offset: {position.Top}");
        foreach (var component in components)
        {
            component.Position = new RectF
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

    protected override void OnDrawSelf(ICanvas c)
    {
        base.OnDrawSelf(c);
        c.DrawRect(Position, new RectStyle());
    }
}