namespace EasyGameFramework.GUI;

public sealed class StackWidget : Widget
{
    public List<IWidget> Children { get; init; } = new();

    protected override IWidget Build(IBuildContext context)
    {
        //Console.WriteLine("Build:StackWidget");
        foreach (var widget in Children)
            widget.ScreenRect = ScreenRect;
        return new MultiChildWidget(Children);
    }
}