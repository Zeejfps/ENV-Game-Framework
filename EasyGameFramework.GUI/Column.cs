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

public enum CrossAxisSize
{
    Min,
    Max
}

public enum CrossAxisAlignment
{
    Start,
    Center,
    End,
}

public sealed class Column : Widget
{
    public float Spacing { get; set; }
    public MainAxisSize MainAxisSize { get; set; }
    public CrossAxisSize CrossAxisSize { get; set; }
    public MainAxisAlignment MainAxisAlignment { get; set; }
    public CrossAxisAlignment CrossAxisAlignment { get; set; }
    public List<Widget> Children { get; set; } = new();
    
    protected override IWidget BuildContent(IBuildContext context)
    {
        var children = Children;
        return new MultiChildWidget(children);
    }

    public override void Layout(IBuildContext context)
    {
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
                totalHeight += layout.Height;
            }
        }
        
        var childWidth = ScreenRect.Width;
        var childrenHeight = (totalHeight - Spacing * (childrenCount - 1)) / childrenCount;
        var y = ScreenRect.Y;
        
        if (MainAxisAlignment == MainAxisAlignment.Center)
        {
            y = (ScreenRect.Height - totalHeight) * 0.5f;
        }
        
        foreach (var child in children)
        {
            var childRect = child.Measure(context);

            if (CrossAxisAlignment == CrossAxisAlignment.Center)
            {
                childRect.X = (ScreenRect.Width - childRect.Width) * 0.5f;
            }
            else if (CrossAxisAlignment == CrossAxisAlignment.Start)
            {
                childRect.X = ScreenRect.X;
            }
            else
            {
                childRect.X = ScreenRect.Width - childRect.Width;
            }
            
            childRect.Y = y;
            
            if (CrossAxisSize == CrossAxisSize.Max)
                childRect.Width = childWidth;
            
            if (MainAxisSize == MainAxisSize.Max)
                childRect.Height = childrenHeight;
            
            child.ScreenRect = childRect;
            y += childRect.Height + Spacing;
        }
        base.Layout(context);
    }
}