using OpenGLSandbox;

namespace ModelViewer;

public sealed class Column : Widget
{
    public float Spacing { get; set; }
    public List<Widget> Children { get; set; } = new();
    
    protected override IWidget Build(IBuildContext context)
    {
        var children = Children;
        var childrenCount = children.Count;
        if (childrenCount < 0)
            return this;
        
        var childrenHeight = (ScreenRect.Height - Spacing * (childrenCount - 1)) / childrenCount;
        var y = ScreenRect.Y;
        foreach (var child in children)
        {
            var childRect = child.ScreenRect;
            childRect.X = ScreenRect.X;
            childRect.Y = y;
            childRect.Width = ScreenRect.Width;
            childRect.Height = childrenHeight;
            child.ScreenRect = childRect;
            y += childrenHeight + Spacing;
        }

        return new MultiChildWidget(children);
    }
}