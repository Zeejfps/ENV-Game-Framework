using ZGF.Geometry;

namespace ZGF.Gui;

public class StackLayout : Layout
{
    protected override RectF OnDoLayout(RectF position, IReadOnlyList<Component> components)
    {
        Console.WriteLine("Stack layout");
        foreach (var component in components)
        {
            component.Position = position;
            component.LayoutSelf();
        }
        return position;
    }
}