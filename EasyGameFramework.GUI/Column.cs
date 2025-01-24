using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public enum MainAxisSize
{
    Min,
    Max
}

public enum MainAxisAlignment
{
    Start,
    Center,
    End,
}

public sealed class Column : Widget
{
    public float Spacing { get; set; }
    public MainAxisSize MainAxisSize { get; set; }
    public MainAxisAlignment MainAxisAlignment { get; set; }
    public List<Widget> Children { get; set; } = new();
    
    protected override IWidget BuildContent(IBuildContext context)
    {
        var children = Children;
        return new MultiChildWidget(children);
    }

    public override void Layout(IBuildContext context)
    {
        Console.WriteLine($"Layout Column: {ScreenRect}");
        var children = Children;
        var childrenCount = children.Count;
        if (childrenCount < 0)
            return;

        var totalHeight = ScreenRect.Height;
        if (MainAxisSize == MainAxisSize.Min)
        {
            totalHeight = 0f;
            foreach (var child in children)
            {
                var layout = child.Measure(context);
                Console.WriteLine($"{child} Height: {layout.Height}");
                totalHeight += layout.Height;
            }
        }
        
        Console.WriteLine($"Total height: {totalHeight}");
        var childrenHeight = (totalHeight - Spacing * (childrenCount - 1)) / childrenCount;
        var y = ScreenRect.Y;

        if (MainAxisAlignment == MainAxisAlignment.Center)
        {
            y = (ScreenRect.Height - totalHeight) * 0.5f;
        }
        
        foreach (var child in children)
        {
            var childRect = child.ScreenRect;
            childRect.X = ScreenRect.X;
            childRect.Y = y;
            
            childRect.Width = ScreenRect.Width;
            childRect.Height = childrenHeight;
            Console.WriteLine($"Setting {child} Height: {childrenHeight}");
            
            child.ScreenRect = childRect;
            y += childrenHeight + Spacing;
        }
        base.Layout(context);
    }
}