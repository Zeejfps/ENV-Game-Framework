using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public sealed class StackWidget : Widget
{
    public List<IWidget> Children { get; init; } = new();

    protected override IWidget BuildContent(IBuildContext context)
    {
        //Console.WriteLine("Build:StackWidget");
        return new MultiChildWidget(Children);
    }

    public override void Layout(IBuildContext context)
    {
        Console.WriteLine($"Layout Stack: {ScreenRect}");
        foreach (var widget in Children)
            widget.ScreenRect = ScreenRect;
        base.Layout(context);
    }

    public override Rect Measure(IBuildContext context)
    {
        var maxWidth = 0f;
        var maxHeight = 0f;
        foreach (var child in Children)
        {
            var childRect = child.Measure(context);
            if (childRect.Width > maxWidth)
            {
                maxWidth = childRect.Width;
            }
            
            if (childRect.Height > maxHeight)
            {
                maxHeight = childRect.Height;
            }
        }
        return new Rect(0, 0, maxWidth, maxHeight);
    }
}